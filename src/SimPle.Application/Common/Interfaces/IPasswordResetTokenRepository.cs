using SimPle.Domain.Users;

namespace SimPle.Application.Common.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
    Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(DateTime before, CancellationToken ct = default);
}
