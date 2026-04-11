using System.Collections.Concurrent;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Carniceria.Infrastructure.Services;

/// <summary>
/// Minimal WebAuthn (FIDO2 Passkeys) implementation using only built-in .NET crypto.
/// Supports ES256 (P-256 ECDSA) — the most common authenticator algorithm.
/// Challenges are stored in-memory (fine for single-instance POS).
/// </summary>
public class WebAuthnService
{
    private readonly IConfiguration _config;
    private readonly ConcurrentDictionary<string, (string Challenge, DateTime Expiry)> _pendingChallenges = new();

    public WebAuthnService(IConfiguration config) => _config = config;

    private string RpId   => _config["WebAuthn:RpId"]   ?? "localhost";
    private string RpName => _config["WebAuthn:RpName"] ?? _config["App:ShopName"] ?? "POS";

    // ── Registration ───────────────────────────────────────────────

    public (string Challenge, object Options) CreateRegistrationOptions(Guid userId, string username)
    {
        var challenge = GenerateChallenge();
        _pendingChallenges[userId.ToString()] = (challenge, DateTime.UtcNow.AddMinutes(5));

        var options = new
        {
            challenge,
            rp = new { id = RpId, name = RpName },
            user = new
            {
                id = Convert.ToBase64String(userId.ToByteArray()),
                name = username,
                displayName = username,
            },
            pubKeyCredParams = new[]
            {
                new { type = "public-key", alg = -7 },  // ES256
                new { type = "public-key", alg = -257 }, // RS256
            },
            timeout = 60000,
            attestation = "none",
            authenticatorSelection = new
            {
                residentKey = "preferred",
                userVerification = "preferred",
            },
        };

        return (challenge, options);
    }

    public (byte[] CredentialId, byte[] PublicKey, uint SignCount) VerifyRegistration(
        Guid userId, string clientDataJsonB64, string attestationObjectB64)
    {
        if (!_pendingChallenges.TryRemove(userId.ToString(), out var stored) || stored.Expiry < DateTime.UtcNow)
            throw new InvalidOperationException("Challenge expired or not found.");

        var clientDataJson   = Base64UrlDecode(clientDataJsonB64);
        var attestationObject = Base64UrlDecode(attestationObjectB64);

        // Validate clientDataJSON
        var clientData = JsonDocument.Parse(clientDataJson).RootElement;
        if (clientData.GetProperty("type").GetString() != "webauthn.create")
            throw new InvalidOperationException("Invalid type.");

        var receivedChallenge = clientData.GetProperty("challenge").GetString()!;
        // Browsers base64url-encode the challenge bytes
        var expectedChallenge = Convert.ToBase64String(Encoding.UTF8.GetBytes(stored.Challenge))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        if (receivedChallenge != expectedChallenge && receivedChallenge != stored.Challenge)
            throw new InvalidOperationException("Challenge mismatch.");

        // Parse attestationObject (CBOR)
        var (authData, _) = ParseAttestationObject(attestationObject);

        // Parse authData
        var (credentialId, publicKey, signCount) = ParseAuthData(authData, isRegistration: true);

        return (credentialId!, publicKey!, signCount);
    }

    // ── Authentication ─────────────────────────────────────────────

    public (string Challenge, object Options) CreateAuthenticationOptions(Guid userId, byte[] credentialId)
    {
        var challenge = GenerateChallenge();
        _pendingChallenges["auth-" + userId] = (challenge, DateTime.UtcNow.AddMinutes(5));

        var options = new
        {
            challenge,
            rpId = RpId,
            timeout = 60000,
            userVerification = "preferred",
            allowCredentials = new[]
            {
                new { type = "public-key", id = Convert.ToBase64String(credentialId) },
            },
        };

        return (challenge, options);
    }

    public uint VerifyAuthentication(
        Guid userId, byte[] storedPublicKey, uint storedSignCount,
        string clientDataJsonB64, string authenticatorDataB64, string signatureB64)
    {
        if (!_pendingChallenges.TryRemove("auth-" + userId, out var stored) || stored.Expiry < DateTime.UtcNow)
            throw new InvalidOperationException("Challenge expired or not found.");

        var clientDataJson    = Base64UrlDecode(clientDataJsonB64);
        var authenticatorData = Base64UrlDecode(authenticatorDataB64);
        var signature         = Base64UrlDecode(signatureB64);

        // Validate clientDataJSON
        var clientData = JsonDocument.Parse(clientDataJson).RootElement;
        if (clientData.GetProperty("type").GetString() != "webauthn.get")
            throw new InvalidOperationException("Invalid type.");

        var receivedChallenge = clientData.GetProperty("challenge").GetString()!;
        var expectedChallenge = Convert.ToBase64String(Encoding.UTF8.GetBytes(stored.Challenge))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        if (receivedChallenge != expectedChallenge && receivedChallenge != stored.Challenge)
            throw new InvalidOperationException("Challenge mismatch.");

        // Build verification data: authenticatorData || SHA256(clientDataJSON)
        var clientDataHash = SHA256.HashData(clientDataJson);
        var verificationData = authenticatorData.Concat(clientDataHash).ToArray();

        // Verify signature using stored public key (COSE EC2 P-256)
        if (!VerifyEs256Signature(storedPublicKey, verificationData, signature))
            throw new InvalidOperationException("Signature verification failed.");

        // Parse sign count from authData (bytes 33-36, big-endian)
        var newSignCount = (uint)(authenticatorData[33] << 24 | authenticatorData[34] << 16
                                | authenticatorData[35] << 8  | authenticatorData[36]);

        if (newSignCount > 0 && newSignCount <= storedSignCount)
            throw new InvalidOperationException("Sign count replay detected.");

        return newSignCount;
    }

    // ── Private helpers ────────────────────────────────────────────

    private static string GenerateChallenge() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        var rem = s.Length % 4;
        if (rem == 2) s += "==";
        else if (rem == 3) s += "=";
        return Convert.FromBase64String(s);
    }

    private static (byte[] AuthData, string Format) ParseAttestationObject(byte[] attestationObject)
    {
        var reader = new CborReader(attestationObject);
        reader.ReadStartMap();
        byte[]? authData = null;
        string format = "none";
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "fmt":
                    format = reader.ReadTextString();
                    break;
                case "authData":
                    authData = reader.ReadByteString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        return (authData ?? throw new InvalidOperationException("No authData in attestation."), format);
    }

    private static (byte[]? CredentialId, byte[]? PublicKey, uint SignCount) ParseAuthData(byte[] authData, bool isRegistration)
    {
        // Layout: rpIdHash(32) | flags(1) | signCount(4) | [attested credential data if AT flag set]
        if (authData.Length < 37) throw new InvalidOperationException("AuthData too short.");

        uint signCount = (uint)(authData[33] << 24 | authData[34] << 16 | authData[35] << 8 | authData[36]);

        if (!isRegistration) return (null, null, signCount);

        byte flags = authData[32];
        bool hasAttestedCredential = (flags & 0x40) != 0;
        if (!hasAttestedCredential) throw new InvalidOperationException("No attested credential data.");

        // Skip aaguid (16 bytes)
        int offset = 37 + 16;

        // Credential ID length (2 bytes, big-endian)
        int credIdLen = (authData[offset] << 8) | authData[offset + 1];
        offset += 2;

        var credentialId = authData[offset..(offset + credIdLen)];
        offset += credIdLen;

        // Remaining bytes = COSE-encoded public key
        var publicKeyBytes = authData[offset..];

        // Validate it's an EC2 P-256 key by doing a quick CBOR peek
        ValidateCoseKey(publicKeyBytes);

        return (credentialId, publicKeyBytes, signCount);
    }

    private static void ValidateCoseKey(byte[] coseKey)
    {
        var reader = new CborReader(coseKey);
        reader.ReadStartMap();
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var label = reader.ReadInt32();
            if (label == 1) // kty
            {
                var kty = reader.ReadInt32();
                if (kty != 2) throw new InvalidOperationException("Only EC2 keys (kty=2) are supported.");
                return;
            }
            reader.SkipValue();
        }
    }

    private static bool VerifyEs256Signature(byte[] cosePublicKey, byte[] data, byte[] signature)
    {
        // Parse COSE EC2 key to extract x and y coordinates
        var reader = new CborReader(cosePublicKey);
        reader.ReadStartMap();
        byte[]? x = null, y = null;
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var label = reader.ReadInt32();
            switch (label)
            {
                case -2: x = reader.ReadByteString(); break;
                case -3: y = reader.ReadByteString(); break;
                default: reader.SkipValue(); break;
            }
        }

        if (x is null || y is null) throw new InvalidOperationException("Missing x or y in COSE key.");

        var ecParams = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = x, Y = y },
        };

        using var ecdsa = ECDsa.Create(ecParams);
        // Convert DER-encoded signature to IEEE P1363 format expected by .NET
        var ieeeSig = ConvertDerToIeeeP1363(signature);
        return ecdsa.VerifyData(data, ieeeSig, HashAlgorithmName.SHA256);
    }

    /// <summary>Converts DER-encoded ECDSA signature to the IEEE P1363 fixed-length format.</summary>
    private static byte[] ConvertDerToIeeeP1363(byte[] derSig)
    {
        // DER: 30 len 02 rLen r 02 sLen s
        int pos = 2; // skip 0x30 and total length
        if (derSig[pos] != 0x02) throw new InvalidOperationException("Invalid DER signature.");
        int rLen = derSig[pos + 1];
        pos += 2;
        byte[] r = derSig[pos..(pos + rLen)];
        pos += rLen;
        if (derSig[pos] != 0x02) throw new InvalidOperationException("Invalid DER signature.");
        int sLen = derSig[pos + 1];
        pos += 2;
        byte[] s = derSig[pos..(pos + sLen)];

        // Remove leading zero padding and pad to 32 bytes
        static byte[] Normalize(byte[] v)
        {
            var trimmed = v.SkipWhile(b => b == 0).ToArray();
            if (trimmed.Length > 32) trimmed = trimmed[^32..];
            var result = new byte[32];
            trimmed.CopyTo(result, 32 - trimmed.Length);
            return result;
        }

        return [.. Normalize(r), .. Normalize(s)];
    }
}
