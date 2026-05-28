using FluentAssertions;
using SimPle.Domain.Users;

namespace SimPle.UnitTests.Domain;

public sealed class UserEntityTests
{
    private static User MakeUser(string username = "mohan", string email = "mohan@example.com") =>
        User.Create(username, email, "hash", "Mohan Ehab");

    // ── Factory methods ───────────────────────────────────────────────────────

    [Fact]
    public void Create_sets_normalized_fields_and_defaults()
    {
        var user = MakeUser("  TestUser  ", "  Test@Example.com  ");

        user.Username.Should().Be("TestUser");
        user.NormalizedUsername.Should().Be("TESTUSER");
        user.Email.Should().Be("Test@Example.com");
        user.NormalizedEmail.Should().Be("TEST@EXAMPLE.COM");
        user.IsEmailVerified.Should().BeFalse();
        user.GoogleId.Should().BeNull();
    }

    [Fact]
    public void Create_builds_initials_from_display_name()
    {
        var single = User.Create("u", "a@b.com", "h", "Mohan");
        var multi = User.Create("u", "a@b.com", "h", "Mohan Ehab");
        var extra = User.Create("u", "a@b.com", "h", "Mohan Ehab Extra");

        single.Initials.Should().Be("M");
        multi.Initials.Should().Be("ME");
        extra.Initials.Should().Be("ME");  // only first two words
    }

    [Fact]
    public void CreateWithGoogle_sets_google_id_and_verifies_email()
    {
        var user = User.CreateWithGoogle("guser", "g@google.com", "Google User", "gid-123", "https://avatar.url");

        user.GoogleId.Should().Be("gid-123");
        user.IsEmailVerified.Should().BeTrue();
        user.AvatarUrl.Should().Be("https://avatar.url");
        user.PasswordHash.Should().BeEmpty();
    }

    [Fact]
    public void CreateWithGoogle_handles_null_avatar_url()
    {
        var user = User.CreateWithGoogle("guser", "g@google.com", "Google User", "gid-123", null);

        user.AvatarUrl.Should().BeNull();
    }

    // ── Mutation methods ──────────────────────────────────────────────────────

    [Fact]
    public void ConnectGoogle_sets_google_id_and_bumps_updated_at()
    {
        var user = MakeUser();
        var before = user.UpdatedAt;

        user.ConnectGoogle("new-gid");

        user.GoogleId.Should().Be("new-gid");
        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateProfile_changes_display_fields()
    {
        var user = MakeUser();

        user.UpdateProfile("New Name", "My bio", "https://avatar", "https://banner");

        user.DisplayName.Should().Be("New Name");
        user.Bio.Should().Be("My bio");
        user.AvatarUrl.Should().Be("https://avatar");
        user.BannerUrl.Should().Be("https://banner");
        user.Initials.Should().Be("NN");
    }

    [Fact]
    public void UpdatePassword_rotates_security_stamp()
    {
        var user = MakeUser();
        var stampBefore = user.SecurityStamp;

        user.UpdatePassword("new-hash");

        user.PasswordHash.Should().Be("new-hash");
        user.SecurityStamp.Should().NotBe(stampBefore);
    }

    [Fact]
    public void RecordFailedLogin_increments_count_and_locks_at_threshold()
    {
        var user = MakeUser();

        for (var i = 0; i < 9; i++) user.RecordFailedLogin(maxAttempts: 10);
        user.IsLockedOut().Should().BeFalse();

        user.RecordFailedLogin(maxAttempts: 10, lockoutDurationMinutes: 15);
        user.IsLockedOut().Should().BeTrue();
        user.LockoutEnd.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ResetFailedLogin_clears_lockout_and_records_last_login()
    {
        var user = MakeUser();
        user.RecordFailedLogin(maxAttempts: 1);  // trigger lockout

        user.ResetFailedLogin();

        user.FailedLoginCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsLockedOut_returns_false_when_no_lockout_set()
    {
        MakeUser().IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void SetStatus_updates_status()
    {
        var user = MakeUser();

        user.SetStatus(UserStatus.Online);

        user.Status.Should().Be(UserStatus.Online);
    }

    [Fact]
    public void VerifyEmail_marks_email_as_verified()
    {
        var user = MakeUser();
        user.IsEmailVerified.Should().BeFalse();

        user.VerifyEmail();

        user.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void AddXp_accumulates_correctly()
    {
        var user = MakeUser();

        user.AddXp(100);
        user.AddXp(50);

        user.Xp.Should().Be(150);
    }

    [Fact]
    public void UpdateElo_sets_new_elo_value()
    {
        var user = MakeUser();

        user.UpdateElo(1350);

        user.Elo.Should().Be(1350);
    }

    [Fact]
    public void Suspend_sets_flag_and_optional_end_date()
    {
        var user = MakeUser();
        var until = DateTime.UtcNow.AddDays(7);

        user.Suspend(until);

        user.IsSuspended.Should().BeTrue();
        user.SuspendedUntil.Should().BeCloseTo(until, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Suspend_without_end_date_sets_permanent_flag()
    {
        var user = MakeUser();

        user.Suspend();

        user.IsSuspended.Should().BeTrue();
        user.SuspendedUntil.Should().BeNull();
    }

    [Fact]
    public void Unsuspend_clears_suspension()
    {
        var user = MakeUser();
        user.Suspend(DateTime.UtcNow.AddDays(1));

        user.Unsuspend();

        user.IsSuspended.Should().BeFalse();
        user.SuspendedUntil.Should().BeNull();
    }
}
