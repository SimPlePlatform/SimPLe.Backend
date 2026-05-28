namespace SimPle.Application.Common.Interfaces;

public interface ICaptchaVerificationService
{
    Task<bool> VerifyAsync(string responseToken, string? remoteIpAddress, CancellationToken ct = default);
}
