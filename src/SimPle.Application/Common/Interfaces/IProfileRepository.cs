using SimPle.Domain.Profiles;

namespace SimPle.Application.Common.Interfaces;

public interface IProfileRepository
{
    Task<IReadOnlyList<ProfileExternalLink>> GetLinksByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task ReplaceLinksAsync(Guid userId, IReadOnlyList<ProfileExternalLink> links, CancellationToken ct = default);

    Task<IReadOnlyList<ProfileInterestTag>> GetInterestsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task ReplaceInterestsAsync(Guid userId, IReadOnlyList<ProfileInterestTag> tags, CancellationToken ct = default);
}
