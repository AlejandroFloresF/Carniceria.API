using Carniceria.Domain.Common;
using System.Security.Cryptography;

namespace Carniceria.Domain.Entities;

public enum UserRole { Admin, Cashier }

public class AppUser : BaseEntity
{
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    // Profile
    public string? Email { get; private set; }
    public bool EmailVerified { get; private set; }
    public string? ProfilePhotoBase64 { get; private set; }  // data URL stored directly

    // Email change flow
    public string? PendingEmail { get; private set; }

    // Password reset (forgot password link)
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetExpiry { get; private set; }

    // OTP for sensitive in-app changes (change password, username, confirm email)
    public string? OtpCode { get; private set; }
    public DateTime? OtpExpiry { get; private set; }
    public string? OtpPurpose { get; private set; }

    // WebAuthn / Passkey (one credential per user for simplicity)
    public byte[]? PasskeyCredentialId { get; private set; }
    public byte[]? PasskeyPublicKey { get; private set; }     // COSE EC2 P-256 key bytes
    public uint PasskeySignCount { get; private set; }

    private AppUser() { }

    public static AppUser Create(string username, string passwordHash, UserRole role, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Username is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        return new AppUser
        {
            Username = username.Trim().ToLower(),
            PasswordHash = passwordHash,
            Role = role,
            Email = email?.Trim().ToLower(),
            EmailVerified = email is not null,
        };
    }

    // ── Profile mutations ──────────────────────────────────────────

    public void SetUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Username is required.");
        Username = username.Trim().ToLower();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) throw new DomainException("Password hash is required.");
        PasswordHash = hash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEmail(string email)
    {
        Email = email.Trim().ToLower();
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetProfilePhoto(string? base64DataUrl)
    {
        ProfilePhotoBase64 = base64DataUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Password reset (forgot password) ──────────────────────────

    /// <summary>Generates a secure token for the password-reset email link. Valid 1 hour.</summary>
    public string GeneratePasswordResetToken()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        PasswordResetToken = token;
        PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
        UpdatedAt = DateTime.UtcNow;
        return token;
    }

    public bool ValidatePasswordResetToken(string token)
    {
        return PasswordResetToken is not null
            && PasswordResetToken == token
            && PasswordResetExpiry > DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── OTP for in-app sensitive changes ──────────────────────────

    /// <summary>
    /// Generates a 6-digit OTP for a specific purpose (e.g. "change-password", "change-email", "change-username").
    /// Valid 10 minutes. Returns the code to be sent by email.
    /// </summary>
    public string GenerateOtp(string purpose)
    {
        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        OtpCode = code;
        OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        OtpPurpose = purpose;
        UpdatedAt = DateTime.UtcNow;
        return code;
    }

    public bool ValidateOtp(string code, string purpose)
    {
        return OtpCode is not null
            && OtpCode == code
            && OtpPurpose == purpose
            && OtpExpiry > DateTime.UtcNow;
    }

    public void ClearOtp()
    {
        OtpCode = null;
        OtpExpiry = null;
        OtpPurpose = null;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Email change flow ──────────────────────────────────────────

    /// <summary>Stage a new email address. Confirmed via OTP sent to the new address.</summary>
    public void SetPendingEmail(string newEmail)
    {
        PendingEmail = newEmail.Trim().ToLower();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Commit the pending email once the OTP is validated.</summary>
    public void ConfirmEmailChange()
    {
        if (PendingEmail is null) throw new DomainException("No pending email change.");
        Email = PendingEmail;
        PendingEmail = null;
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── WebAuthn / Passkey ─────────────────────────────────────────

    public void SetPasskey(byte[] credentialId, byte[] publicKey, uint signCount)
    {
        PasskeyCredentialId = credentialId;
        PasskeyPublicKey = publicKey;
        PasskeySignCount = signCount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePasskeySignCount(uint signCount)
    {
        PasskeySignCount = signCount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemovePasskey()
    {
        PasskeyCredentialId = null;
        PasskeyPublicKey = null;
        PasskeySignCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }
}
