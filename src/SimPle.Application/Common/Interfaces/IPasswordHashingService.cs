namespace SimPle.Application.Common.Interfaces;

public interface IPasswordHashingService
{
    string Hash(string password);
    bool Verify(string password, string hash);

    // Returns true if the stored hash was made with older/weaker parameters.
    // Used at login time to silently upgrade the hash without forcing a password reset.
    bool NeedsRehash(string hash);
}
