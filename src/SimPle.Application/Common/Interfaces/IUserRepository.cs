using SimPle.Domain.Users;

namespace SimPle.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken ct = default);
    Task<User?> GetByNormalizedEmailOrUsernameAsync(string normalized, CancellationToken ct = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<bool> ExistsByUsernameAsync(string normalizedUsername, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
