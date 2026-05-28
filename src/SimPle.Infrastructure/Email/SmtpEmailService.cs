using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;

namespace SimPle.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string toEmail, string toName, string verificationUrl, CancellationToken ct = default) =>
        SendAsync(
            toEmail, toName,
            "Verify your SimPle email address",
            BuildVerificationHtml(toName, verificationUrl),
            ct);

    public Task SendWelcomeEmailAsync(string toEmail, string toName, CancellationToken ct = default) =>
        SendAsync(
            toEmail, toName,
            "Welcome to SimPle — you're in",
            BuildWelcomeHtml(toName, _options.AppUrl),
            ct);

    public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl, CancellationToken ct = default) =>
        SendAsync(
            toEmail, toName,
            "Reset your SimPle password",
            BuildPasswordResetHtml(toName, resetUrl),
            ct);

    public Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken ct = default) =>
        SendAsync(
            toEmail, toName,
            "Your SimPle password was changed",
            BuildPasswordChangedHtml(toName, _options.AppUrl),
            ct);

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            // 30-second socket-level timeout covers the initial TLS handshake, which can
            // stall on the first connection in a new process (Windows CRL/OCSP download).
            client.Timeout = 30_000;
            // Independent 30-second deadline so a hanging SMTP server does not block
            // the caller even when CancellationToken.None is passed (e.g. from registration).
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls, cts.Token);
            await client.AuthenticateAsync(_options.Username, _options.Password, cts.Token);
            await client.SendAsync(message, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email '{Subject}' to {Email}", subject, toEmail);
            throw;
        }
    }

    // ── HTML templates ────────────────────────────────────────────────────────

    private static string BuildVerificationHtml(string name, string url) => Wrap(
        $@"<h2 style=""margin:0 0 8px"">Verify your email</h2>
        <p style=""margin:0 0 24px;color:#9CA3AF"">Hi {Esc(name)}, thanks for joining SimPle. Click the button below to verify your email address. The link expires in <strong>24 hours</strong>.</p>
        {Cta("Verify email address", url)}
        <p style=""margin:24px 0 0;color:#6B7280;font-size:13px"">If you didn't create a SimPle account you can safely ignore this email.</p>");

    private static string BuildWelcomeHtml(string name, string appUrl) => Wrap(
        $@"<h2 style=""margin:0 0 8px"">Welcome to SimPle, {Esc(name)}!</h2>
        <p style=""margin:0 0 24px;color:#9CA3AF"">Your email is verified and your account is ready. Head to the dashboard to find friends, join lobbies, and climb the ladder.</p>
        {Cta("Go to dashboard", $"{appUrl}/dashboard")}");

    private static string BuildPasswordResetHtml(string name, string url) => Wrap(
        $@"<h2 style=""margin:0 0 8px"">Reset your password</h2>
        <p style=""margin:0 0 24px;color:#9CA3AF"">Hi {Esc(name)}, we received a request to reset your password. Click the button below to choose a new one. The link expires in <strong>1 hour</strong>.</p>
        {Cta("Reset password", url)}
        <p style=""margin:24px 0 0;color:#6B7280;font-size:13px"">If you didn't request a password reset you can safely ignore this email. Your password won't change.</p>");

    private static string BuildPasswordChangedHtml(string name, string appUrl) => Wrap(
        $@"<h2 style=""margin:0 0 8px"">Your password was changed</h2>
        <p style=""margin:0 0 0;color:#9CA3AF"">Hi {Esc(name)}, your SimPle account password was just changed. All active sessions have been signed out.</p>
        <p style=""margin:16px 0 0;color:#9CA3AF"">If this was you, no action is needed. If you didn't make this change, <a href=""{appUrl}/login"" style=""color:#F0394B"">sign in to secure your account</a>.</p>");

    private static string Wrap(string content) =>
        $@"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background:#0A0E18;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;color:#E5E7EB"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#0A0E18"">
    <tr><td align=""center"" style=""padding:40px 16px"">
      <table width=""100%"" style=""max-width:560px;background:#111827;border-radius:12px;border:1px solid #1F2937"" cellpadding=""0"" cellspacing=""0"">
        <tr>
          <td style=""padding:32px 40px 0;border-bottom:1px solid #1F2937"">
            <table cellpadding=""0"" cellspacing=""0""><tr>
              <td style=""width:28px;height:28px;background:#F0394B;border-radius:6px""></td>
              <td style=""padding-left:10px;font-size:18px;font-weight:700;letter-spacing:-0.02em"">SimPle</td>
            </tr></table>
          </td>
        </tr>
        <tr><td style=""padding:32px 40px"">{content}</td></tr>
        <tr>
          <td style=""padding:20px 40px 28px;border-top:1px solid #1F2937;color:#4B5563;font-size:12px"">
            &copy; {DateTime.UtcNow.Year} SimPle &bull; You're receiving this because you have a SimPle account.
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

    private static string Cta(string label, string url) =>
        $@"<a href=""{url}"" style=""display:inline-block;background:#F0394B;color:#fff;text-decoration:none;font-size:14px;font-weight:600;padding:12px 28px;border-radius:8px"">{Esc(label)}</a>";

    private static string Esc(string s) => System.Net.WebUtility.HtmlEncode(s);
}
