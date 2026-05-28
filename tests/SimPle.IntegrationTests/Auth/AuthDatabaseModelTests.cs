using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimPle.Domain.Users;
using SimPle.Infrastructure.Persistence;

namespace SimPle.IntegrationTests.Auth;

public sealed class AuthDatabaseModelTests
{
    [Fact]
    public void AuthTokenTables_HaveUserOwnershipAndUniqueTokenHashIndexes()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var model = scope.ServiceProvider.GetRequiredService<AppDbContext>().Model;

        AssertOwnedTokenTable<RefreshToken>(model, "refresh_tokens");
        AssertOwnedTokenTable<EmailVerificationToken>(model, "email_verification_tokens");
        AssertOwnedTokenTable<PasswordResetToken>(model, "password_reset_tokens");
    }

    private static void AssertOwnedTokenTable<TToken>(
        Microsoft.EntityFrameworkCore.Metadata.IModel model,
        string tableName)
    {
        var entity = model.FindEntityType(typeof(TToken))!;

        entity.GetTableName().Should().Be(tableName);
        entity.GetForeignKeys().Should().ContainSingle(fk =>
            fk.PrincipalEntityType.ClrType == typeof(User) &&
            fk.Properties.Single().Name == "UserId");
        entity.GetIndexes().Should().ContainSingle(index =>
            index.IsUnique && index.Properties.Single().Name == "TokenHash");
    }
}
