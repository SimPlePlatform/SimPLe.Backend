using Microsoft.EntityFrameworkCore;
using SimPle.Domain.Profiles;
using SimPle.Domain.Users;

namespace SimPle.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Module 2 — profile social identity
    public DbSet<ProfileExternalLink> ProfileExternalLinks => Set<ProfileExternalLink>();
    public DbSet<ProfileInterestTag> ProfileInterestTags => Set<ProfileInterestTag>();
    public DbSet<UsernameChangeRequest> UsernameChangeRequests => Set<UsernameChangeRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
