using Carniceria.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Carniceria.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SmtpEmailService(IConfiguration config) => _config = config;

    public async Task SendPasswordResetAsync(string toEmail, string displayName, string resetLink, CancellationToken ct = default)
    {
        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px 24px">
              <h2 style="color:#4f46e5;margin-bottom:8px">Recuperar contraseña</h2>
              <p style="color:#374151">Hola <strong>{displayName}</strong>,</p>
              <p style="color:#374151">Recibimos una solicitud para restablecer tu contraseña. Haz clic en el botón:</p>
              <a href="{resetLink}"
                 style="display:inline-block;margin:20px 0;padding:12px 28px;background:#4f46e5;color:#fff;
                        border-radius:8px;text-decoration:none;font-weight:600">
                Restablecer contraseña
              </a>
              <p style="color:#6b7280;font-size:13px">
                Este enlace expira en <strong>1 hora</strong>.<br>
                Si no solicitaste esto, ignora este correo.
              </p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0">
              <p style="color:#9ca3af;font-size:12px">{_config["App:ShopName"] ?? "POS"}</p>
            </div>
            """;

        await SendAsync(toEmail, displayName, "Restablecer contraseña", body, ct);
    }

    public async Task SendOtpAsync(string toEmail, string displayName, string otp, string purpose, CancellationToken ct = default)
    {
        var purposeLabel = purpose switch
        {
            "change-password" => "cambiar tu contraseña",
            "change-email"    => "cambiar tu correo electrónico",
            "change-username" => "cambiar tu nombre de usuario",
            _                 => "realizar este cambio",
        };

        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px 24px">
              <h2 style="color:#4f46e5;margin-bottom:8px">Código de verificación</h2>
              <p style="color:#374151">Hola <strong>{displayName}</strong>,</p>
              <p style="color:#374151">Ingresa este código para {purposeLabel}:</p>
              <div style="margin:24px 0;text-align:center">
                <span style="display:inline-block;padding:16px 40px;background:#f3f4f6;border-radius:12px;
                             font-size:36px;font-weight:700;letter-spacing:8px;color:#111827">
                  {otp}
                </span>
              </div>
              <p style="color:#6b7280;font-size:13px">
                Expira en <strong>10 minutos</strong>.<br>
                Si no solicitaste este código, ignora este correo.
              </p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0">
              <p style="color:#9ca3af;font-size:12px">{_config["App:ShopName"] ?? "POS"}</p>
            </div>
            """;

        await SendAsync(toEmail, displayName, $"Código de verificación — {otp}", body, ct);
    }

    private async Task SendAsync(string toEmail, string displayName, string subject, string htmlBody, CancellationToken ct)
    {
        var fromEmail = _config["Email:From"] ?? throw new InvalidOperationException("Email:From not configured.");
        var fromName  = _config["Email:FromName"] ?? _config["App:ShopName"] ?? "POS";
        var host      = _config["Email:Host"] ?? throw new InvalidOperationException("Email:Host not configured.");
        var port      = int.Parse(_config["Email:Port"] ?? "587");
        var username  = _config["Email:Username"] ?? throw new InvalidOperationException("Email:Username not configured.");
        var password  = _config["Email:Password"] ?? throw new InvalidOperationException("Email:Password not configured.");
        var useSsl    = bool.Parse(_config["Email:UseSsl"] ?? "false");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(displayName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

        using var client = new SmtpClient();
        var secureOption = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(host, port, secureOption, ct);
        await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
