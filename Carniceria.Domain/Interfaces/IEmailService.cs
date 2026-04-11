namespace Carniceria.Domain.Interfaces;

public interface IEmailService
{
    /// <summary>Sends a password-reset link to the user's email.</summary>
    Task SendPasswordResetAsync(string toEmail, string displayName, string resetLink, CancellationToken ct = default);

    /// <summary>Sends a 6-digit OTP for a sensitive profile change.</summary>
    Task SendOtpAsync(string toEmail, string displayName, string otp, string purpose, CancellationToken ct = default);
}
