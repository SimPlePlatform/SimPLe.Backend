using SimPle.Domain.Common;

namespace SimPle.Domain.Users;

public class User : Entity
{
    public string Username { get; private set; } = default!;
    public string NormalizedUsername { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? AvatarObjectKey { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? BannerObjectKey { get; private set; }
    public string Color { get; private set; } = "#F0394B";
    public string Initials { get; private set; } = default!;
    public int Level { get; private set; } = 1;
    public long Xp { get; private set; }
    public int Elo { get; private set; } = 1200;
    public string Region { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; } = UserStatus.Offline;
    public UserRole Role { get; private set; } = UserRole.Player;
    public bool IsEmailVerified { get; private set; }
    public bool IsSuspended { get; private set; }
    public DateTime? SuspendedUntil { get; private set; }
    public SubscriptionTier SubscriptionTier { get; private set; } = SubscriptionTier.Free;

    // Profile social identity
    public string? StatusMessage { get; private set; }
    public ProfileVisibility Visibility { get; private set; } = ProfileVisibility.Public;

    // Auth security fields
    public int FailedLoginCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public Guid SecurityStamp { get; private set; } = Guid.NewGuid();
    public DateTime? LastLoginAt { get; private set; }

    // OAuth providers
    public string? GoogleId { get; private set; }

    // EF Core needs a parameterless constructor
    private User() { }

    public static User Create(string username, string email, string passwordHash, string displayName)
    {
        return new User
        {
            Username = username.Trim(),
            NormalizedUsername = username.Trim().ToUpperInvariant(),
            Email = email.Trim(),
            NormalizedEmail = email.Trim().ToUpperInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName.Trim(),
            Initials = BuildInitials(displayName),
            IsEmailVerified = false,
        };
    }

    public static User CreateWithGoogle(string username, string email, string displayName, string googleId, string? avatarUrl)
    {
        return new User
        {
            Username = username.Trim(),
            NormalizedUsername = username.Trim().ToUpperInvariant(),
            Email = email.Trim(),
            NormalizedEmail = email.Trim().ToUpperInvariant(),
            PasswordHash = string.Empty,
            DisplayName = displayName.Trim(),
            Initials = BuildInitials(displayName),
            AvatarUrl = avatarUrl,
            IsEmailVerified = true,
            GoogleId = googleId,
        };
    }

    public void ConnectGoogle(string googleId)
    {
        GoogleId = googleId;
        Touch();
    }

    public void UpdateProfile(
        string displayName, string? bio, string? avatarUrl, string? bannerUrl,
        string? region = null, string? statusMessage = null,
        ProfileVisibility? visibility = null)
    {
        DisplayName = displayName;
        Bio = bio;
        AvatarUrl = avatarUrl;
        BannerUrl = bannerUrl;
        Initials = BuildInitials(displayName);
        if (region is not null) Region = region;
        StatusMessage = statusMessage;
        if (visibility.HasValue) Visibility = visibility.Value;
        Touch();
    }

    public void SetAvatarMedia(string objectKey)
    {
        AvatarObjectKey = objectKey;
        AvatarUrl = null;
        Touch();
    }

    public void ClearAvatarMedia()
    {
        AvatarObjectKey = null;
        AvatarUrl = null;
        Touch();
    }

    public void SetBannerMedia(string objectKey)
    {
        BannerObjectKey = objectKey;
        BannerUrl = null;
        Touch();
    }

    public void ClearBannerMedia()
    {
        BannerObjectKey = null;
        BannerUrl = null;
        Touch();
    }

    public void UpdateAvatarFallbackColor(string color)
    {
        Color = color.Trim();
        Touch();
    }

    public void UpdateUsername(string newUsername)
    {
        Username = newUsername.Trim();
        NormalizedUsername = newUsername.Trim().ToUpperInvariant();
        Touch();
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        // Bump the security stamp so any existing sessions tied to the old stamp become invalid.
        SecurityStamp = Guid.NewGuid();
        Touch();
    }

    public void RecordFailedLogin(int maxAttempts = 10, int lockoutDurationMinutes = 15)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutDurationMinutes);
        Touch();
    }

    public void ResetFailedLogin()
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        LastLoginAt = DateTime.UtcNow;
        Touch();
    }

    public bool IsLockedOut() =>
        LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public bool IsAccountSuspended() =>
        IsSuspended && (SuspendedUntil == null || SuspendedUntil.Value > DateTime.UtcNow);

    public void UpdateEmail(string newEmail)
    {
        Email = newEmail.Trim();
        NormalizedEmail = newEmail.Trim().ToUpperInvariant();
        IsEmailVerified = true;
        SecurityStamp = Guid.NewGuid();
        Touch();
    }

    public void DisconnectGoogle() { GoogleId = null; Touch(); }

    public void SetStatus(UserStatus status) { Status = status; Touch(); }
    public void VerifyEmail() { IsEmailVerified = true; Touch(); }
    public void AddXp(long amount) { Xp += amount; Touch(); }
    public void UpdateElo(int newElo) { Elo = newElo; Touch(); }
    public void Suspend(DateTime? until = null) { IsSuspended = true; SuspendedUntil = until; Touch(); }
    public void Unsuspend() { IsSuspended = false; SuspendedUntil = null; Touch(); }

    private static string BuildInitials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => char.ToUpper(w[0])));
}

public enum UserStatus { Offline, Online, Away, Playing, InLobby }
public enum UserRole { Player, Moderator, Admin }
public enum SubscriptionTier { Free, Plus, Pro }
public enum ProfileVisibility { Public, FriendsOnly, Private }
