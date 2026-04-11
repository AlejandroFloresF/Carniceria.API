using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Carniceria.Application.Features.Auth;
using Carniceria.Domain.Interfaces;
using Carniceria.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carniceria.API.Controllers;

// ── Request records ────────────────────────────────────────────────────────────

public record LoginRequest(
    [Required][MaxLength(100)] string Username,
    [Required][MaxLength(255)] string Password
);

public record ForgotPasswordRequest([Required][EmailAddress][MaxLength(200)] string Email);

public record ResetPasswordRequest(
    [Required][MaxLength(200)] string Token,
    [Required][MinLength(8)][MaxLength(255)] string NewPassword
);

public record SendOtpRequest([Required][MaxLength(50)] string Purpose);

public record ChangePasswordRequest(
    [Required][MaxLength(255)] string CurrentPassword,
    [Required][MinLength(8)][MaxLength(255)] string NewPassword,
    [Required][MaxLength(10)] string OtpCode
);

public record UpdateUsernameRequest(
    [Required][MaxLength(100)] string NewUsername,
    [Required][MaxLength(10)] string OtpCode
);

public record RequestEmailChangeRequest(
    [Required][EmailAddress][MaxLength(200)] string NewEmail
);

public record ConfirmEmailChangeRequest([Required][MaxLength(10)] string OtpCode);

public record UpdateProfilePhotoRequest([MaxLength(8_000_000)] string? Base64DataUrl);

// WebAuthn
public record PasskeyRegisterVerifyRequest(
    [Required] string ClientDataJson,
    [Required] string AttestationObject
);
public record PasskeyAuthOptionsRequest([Required][MaxLength(100)] string Username);
public record PasskeyAuthVerifyRequest(
    [Required] string ClientDataJson,
    [Required] string AuthenticatorData,
    [Required] string Signature,
    [Required] string CredentialId
);

// ── Controller ─────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IUserRepository _users;
    private readonly IEmailService _email;
    private readonly WebAuthnService _webAuthn;
    private readonly IConfiguration _config;

    public AuthController(
        ISender mediator, IUserRepository users,
        IEmailService email, WebAuthnService webAuthn, IConfiguration config)
    {
        _mediator = mediator;
        _users    = users;
        _email    = email;
        _webAuthn = webAuthn;
        _config   = config;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());

    // ── Login ──────────────────────────────────────────────────────

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _mediator.Send(new LoginCommand(req.Username, req.Password));
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    // ── Forgot / reset password (unauthenticated) ──────────────────

    [HttpPost("forgot-password")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);
        // Always return 200 to avoid email enumeration
        if (user is null) return Ok(new { message = "Si el correo existe, recibirás un enlace." });

        var token    = user.GeneratePasswordResetToken();
        var baseUrl  = _config["App:BaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{baseUrl}?reset-token={token}";

        await _users.SaveChangesAsync();

        try { await _email.SendPasswordResetAsync(user.Email!, user.Username, resetLink); }
        catch { /* log but don't expose SMTP errors */ }

        return Ok(new { message = "Si el correo existe, recibirás un enlace." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _users.GetByPasswordResetTokenAsync(req.Token);
        if (user is null || !user.ValidatePasswordResetToken(req.Token))
            return BadRequest(new { error = "Token inválido o expirado." });

        user.SetPasswordHash(BCrypt.Net.BCrypt.HashPassword(req.NewPassword));
        user.ClearPasswordResetToken();
        await _users.SaveChangesAsync();

        return Ok(new { message = "Contraseña actualizada." });
    }

    // ── Profile (authenticated, admin only) ────────────────────────

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();
        return Ok(new
        {
            id             = user.Id,
            username       = user.Username,
            role           = user.Role.ToString(),
            email          = user.Email,
            emailVerified  = user.EmailVerified,
            profilePhoto   = user.ProfilePhotoBase64,
            hasPasskey     = user.PasskeyCredentialId is not null,
        });
    }

    [HttpPost("send-otp")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();
        if (user.Email is null) return BadRequest(new { error = "Necesitas registrar un correo primero." });

        var otp = user.GenerateOtp(req.Purpose);
        await _users.SaveChangesAsync();

        try { await _email.SendOtpAsync(user.Email, user.Username, otp, req.Purpose); }
        catch { return StatusCode(500, new { error = "Error al enviar el correo. Verifica la configuración SMTP." }); }

        return Ok(new { message = "Código enviado a tu correo." });
    }

    [HttpPut("change-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { error = "Contraseña actual incorrecta." });

        if (!user.ValidateOtp(req.OtpCode, "change-password"))
            return BadRequest(new { error = "Código incorrecto o expirado." });

        user.SetPasswordHash(BCrypt.Net.BCrypt.HashPassword(req.NewPassword));
        user.ClearOtp();
        await _users.SaveChangesAsync();

        return Ok(new { message = "Contraseña actualizada." });
    }

    [HttpPut("username")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeUsername([FromBody] UpdateUsernameRequest req)
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (!user.ValidateOtp(req.OtpCode, "change-username"))
            return BadRequest(new { error = "Código incorrecto o expirado." });

        if (await _users.UsernameExistsAsync(req.NewUsername, CurrentUserId))
            return Conflict(new { error = "Ese nombre de usuario ya existe." });

        user.SetUsername(req.NewUsername);
        user.ClearOtp();
        await _users.SaveChangesAsync();

        return Ok(new { message = "Usuario actualizado.", username = user.Username });
    }

    [HttpPost("request-email-change")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> RequestEmailChange([FromBody] RequestEmailChangeRequest req)
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (await _users.EmailExistsAsync(req.NewEmail, CurrentUserId))
            return Conflict(new { error = "Ese correo ya está en uso." });

        user.SetPendingEmail(req.NewEmail);
        var otp = user.GenerateOtp("change-email");
        await _users.SaveChangesAsync();

        try { await _email.SendOtpAsync(req.NewEmail, user.Username, otp, "change-email"); }
        catch { return StatusCode(500, new { error = "Error al enviar el correo de verificación." }); }

        return Ok(new { message = "Código enviado al nuevo correo." });
    }

    [HttpPost("confirm-email-change")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest req)
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        if (!user.ValidateOtp(req.OtpCode, "change-email"))
            return BadRequest(new { error = "Código incorrecto o expirado." });

        user.ConfirmEmailChange();
        user.ClearOtp();
        await _users.SaveChangesAsync();

        return Ok(new { message = "Correo actualizado.", email = user.Email });
    }

    [HttpPut("profile-photo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProfilePhoto([FromBody] UpdateProfilePhotoRequest req)
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        // Basic validation: must be a data URL or null
        if (req.Base64DataUrl is not null && !req.Base64DataUrl.StartsWith("data:image/"))
            return BadRequest(new { error = "Formato inválido. Se espera una imagen en base64." });

        user.SetProfilePhoto(req.Base64DataUrl);
        await _users.SaveChangesAsync();

        return Ok(new { message = "Foto actualizada." });
    }

    // ── WebAuthn / Passkey ─────────────────────────────────────────

    [HttpPost("passkey/register-options")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PasskeyRegisterOptions()
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        var (_, options) = _webAuthn.CreateRegistrationOptions(user.Id, user.Username);
        return Ok(options);
    }

    [HttpPost("passkey/register-verify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PasskeyRegisterVerify([FromBody] PasskeyRegisterVerifyRequest req)
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();

        try
        {
            var (credId, pubKey, signCount) = _webAuthn.VerifyRegistration(
                user.Id, req.ClientDataJson, req.AttestationObject);
            user.SetPasskey(credId, pubKey, signCount);
            await _users.SaveChangesAsync();
            return Ok(new { message = "Passkey registrada correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("passkey/auth-options")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> PasskeyAuthOptions([FromBody] PasskeyAuthOptionsRequest req)
    {
        var user = await _users.GetByUsernameAsync(req.Username.Trim().ToLower());
        if (user is null || user.PasskeyCredentialId is null)
            return NotFound(new { error = "No hay passkey registrada para este usuario." });

        var (_, options) = _webAuthn.CreateAuthenticationOptions(user.Id, user.PasskeyCredentialId);
        return Ok(options);
    }

    [HttpPost("passkey/auth-verify")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> PasskeyAuthVerify([FromBody] PasskeyAuthVerifyRequest req)
    {
        var credentialIdBytes = Convert.FromBase64String(
            req.CredentialId.Replace('-', '+').Replace('_', '/').PadRight(
                req.CredentialId.Length + (4 - req.CredentialId.Length % 4) % 4, '='));

        var user = await _users.GetByPasskeyCredentialIdAsync(credentialIdBytes);
        if (user is null || user.PasskeyPublicKey is null)
            return NotFound(new { error = "Passkey no encontrada." });

        try
        {
            var newSignCount = _webAuthn.VerifyAuthentication(
                user.Id, user.PasskeyPublicKey, user.PasskeySignCount,
                req.ClientDataJson, req.AuthenticatorData, req.Signature);

            user.UpdatePasskeySignCount(newSignCount);
            await _users.SaveChangesAsync();

            // Issue the same JWT as normal login
            var jwtResult = await _mediator.Send(new LoginByIdCommand(user.Id));
            return jwtResult.IsSuccess ? Ok(jwtResult.Value) : BadRequest(new { error = jwtResult.Error });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("passkey")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemovePasskey()
    {
        var user = await _users.GetByIdAsync(CurrentUserId);
        if (user is null) return NotFound();
        user.RemovePasskey();
        await _users.SaveChangesAsync();
        return Ok(new { message = "Passkey eliminada." });
    }
}
