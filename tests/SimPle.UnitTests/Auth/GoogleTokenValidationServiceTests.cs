using FluentAssertions;
using Microsoft.Extensions.Options;
using SimPle.Application.Common.Options;
using SimPle.Infrastructure.Auth;

namespace SimPle.UnitTests.Auth;

public sealed class GoogleTokenValidationServiceTests
{
    private static GoogleTokenValidationService Build(string clientId = "test-client-id")
    {
        var options = Options.Create(new GoogleOptions { ClientId = clientId });
        return new GoogleTokenValidationService(options);
    }

    [Fact]
    public async Task Returns_null_for_obviously_invalid_token_string()
    {
        var service = Build();

        var result = await service.ValidateAsync("not-a-jwt-at-all");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_for_empty_string()
    {
        var service = Build();

        var result = await service.ValidateAsync(string.Empty);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_for_malformed_three_part_jwt()
    {
        var service = Build();
        // Three base64 parts but garbage payload — signature will never verify.
        var fakeJwt = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.invalidsignature";

        var result = await service.ValidateAsync(fakeJwt);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_cancellation_is_already_requested()
    {
        var service = Build();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should not throw — any exception is swallowed and null returned.
        var result = await service.ValidateAsync("some-token", cts.Token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_regardless_of_client_id_when_token_is_invalid()
    {
        var service = Build("totally-different-client-id");

        var result = await service.ValidateAsync("invalid.token.here");

        result.Should().BeNull();
    }
}
