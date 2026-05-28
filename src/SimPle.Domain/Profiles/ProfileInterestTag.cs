using SimPle.Domain.Common;

namespace SimPle.Domain.Profiles;

public class ProfileInterestTag : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;

    private ProfileInterestTag() { }

    public static ProfileInterestTag Create(Guid userId, string name)
    {
        return new ProfileInterestTag
        {
            UserId = userId,
            Name = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
        };
    }
}
