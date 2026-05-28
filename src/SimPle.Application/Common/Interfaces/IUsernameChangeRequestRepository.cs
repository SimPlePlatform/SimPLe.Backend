using SimPle.Domain.Profiles;

namespace SimPle.Application.Common.Interfaces;

public interface IUsernameChangeRequestRepository
{
    Task<UsernameChangeRequest?> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UsernameChangeRequest?> GetLatestByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UsernameChangeRequest?> GetByUserIdAndMonthAsync(Guid userId, int year, int month, CancellationToken ct = default);
    Task<UsernameChangeRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<UsernameChangeRequest>> GetAllPendingAsync(CancellationToken ct = default);
    Task AddAsync(UsernameChangeRequest request, CancellationToken ct = default);
    Task UpdateAsync(UsernameChangeRequest request, CancellationToken ct = default);
}
