using SimPle.Domain.Users;

namespace SimPle.Application.Common.Interfaces;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<EmailVerificationToken?> GetLatestByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task UpdateAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(DateTime before, CancellationToken ct = default);
}
