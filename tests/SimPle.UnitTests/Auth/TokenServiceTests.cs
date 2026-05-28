using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SimPle.Infrastructure.Auth;

namespace SimPle.UnitTests.Auth;

public sealed class TokenServiceTests
{
    private readonly JwtTokenService _service = new(Options.Create(new JwtSettings
    {
        SecretKey = "unit-tests-have-a-long-secret-key-value-123456789",
        Issuer = "SimPle.Tests",
        Audience = "SimPle.Tests",
        ExpiryMinutes = 15
    }));

    [Fact]
    public void GenerateAccessToken_WritesExpectedClaimsAndExpiry()
    {
        var userId = Guid.NewGuid();

        var (token, expiresAt) = _service.GenerateAccessToken(userId, "mohan", "Player");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Subject.Should().Be(userId.ToString());
        jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value.Should().Be("mohan");
        jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value.Should().Be("Player");
        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        _service.ValidateAccessToken(token).Should().Be(userId);
    }

    [Fact]
    public void RefreshTokens_AreRandomAndHashDeterministically()
    {
        var token = _service.GenerateRawRefreshToken();
        var anotherToken = _service.GenerateRawRefreshToken();

        Convert.FromBase64String(token).Should().HaveCount(32);
        token.Should().NotBe(anotherToken);
        _service.HashToken(token).Should().Be(_service.HashToken(token));
        _service.HashToken(token).Should().NotBe(_service.HashToken(anotherToken));
    }

    [Fact]
    public void ValidateAccessToken_RejectsInvalidInput()
    {
        _service.ValidateAccessToken("not-a-jwt").Should().BeNull();
    }
}
