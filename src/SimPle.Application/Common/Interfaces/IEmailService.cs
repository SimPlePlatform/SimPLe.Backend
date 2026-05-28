namespace SimPle.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string toName, string verificationUrl, CancellationToken ct = default);
    Task SendWelcomeEmailAsync(string toEmail, string toName, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl, CancellationToken ct = default);
    Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken ct = default);
}
