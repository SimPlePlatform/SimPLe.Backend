using FluentAssertions;
using SimPle.Application.Auth.DTOs;
using SimPle.Application.Common.Options;
using SimPle.Shared.Common;

namespace SimPle.UnitTests.Shared;

public sealed class PagedResultTests
{
    [Fact]
    public void TotalPages_rounds_up_correctly()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), TotalCount: 21, Page: 1, PageSize: 10);

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void HasNextPage_true_when_more_pages_exist()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), TotalCount: 21, Page: 1, PageSize: 10);

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_false_on_last_page()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), TotalCount: 20, Page: 2, PageSize: 10);

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_false_on_first_page()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), TotalCount: 30, Page: 1, PageSize: 10);

        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_true_on_second_page()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), TotalCount: 30, Page: 2, PageSize: 10);

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Single_page_result_has_no_next_or_previous()
    {
        var result = new PagedResult<string>(new[] { "a" }, TotalCount: 1, Page: 1, PageSize: 10);

        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }
}

public sealed class PaginationParamsTests
{
    [Fact]
    public void Skip_is_zero_on_first_page()
    {
        new PaginationParams(Page: 1, PageSize: 20).Skip.Should().Be(0);
    }

    [Fact]
    public void Skip_offsets_correctly_on_subsequent_pages()
    {
        new PaginationParams(Page: 3, PageSize: 10).Skip.Should().Be(20);
    }

    [Fact]
    public void Default_values_are_page_1_size_20()
    {
        var p = new PaginationParams();

        p.Page.Should().Be(1);
        p.PageSize.Should().Be(20);
        p.Skip.Should().Be(0);
    }
}

public sealed class AuthDtoTests
{
    [Fact]
    public void UserDto_properties_are_accessible()
    {
        var id = Guid.NewGuid();
        var dto = new UserDto(id, "mohan", "Mohan Ehab", "m@example.com", "ME", "#F0394B", "Player", true, DateTime.UtcNow);

        dto.Id.Should().Be(id);
        dto.Username.Should().Be("mohan");
        dto.DisplayName.Should().Be("Mohan Ehab");
        dto.Email.Should().Be("m@example.com");
        dto.Initials.Should().Be("ME");
        dto.Color.Should().Be("#F0394B");
        dto.Role.Should().Be("Player");
        dto.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void AuthTokenResult_properties_are_accessible()
    {
        var user = new UserDto(Guid.NewGuid(), "u", "U", "u@e.com", "U", "#000", "Player", false, DateTime.UtcNow);
        var expiry = DateTime.UtcNow.AddMinutes(15);
        var token = new AuthTokenResult("access-token", "refresh-token", expiry, user);

        token.AccessToken.Should().Be("access-token");
        token.RawRefreshToken.Should().Be("refresh-token");
        token.AccessExpiresAt.Should().Be(expiry);
        token.User.Should().Be(user);
    }

    [Fact]
    public void GoogleCallbackRequestDto_holds_id_token()
    {
        var dto = new GoogleCallbackRequestDto("google-id-token");

        dto.IdToken.Should().Be("google-id-token");
    }

    [Fact]
    public void VerifyEmailRequestDto_holds_token()
    {
        var dto = new VerifyEmailRequestDto("verify-token");

        dto.Token.Should().Be("verify-token");
    }

    [Fact]
    public void GoogleOptions_SectionName_is_Google()
    {
        GoogleOptions.SectionName.Should().Be("Google");
    }

    [Fact]
    public void GoogleOptions_default_client_id_is_empty()
    {
        new GoogleOptions().ClientId.Should().BeEmpty();
    }

    [Fact]
    public void GoogleOptions_accepts_configured_client_id()
    {
        var opts = new GoogleOptions { ClientId = "123.apps.googleusercontent.com" };

        opts.ClientId.Should().Be("123.apps.googleusercontent.com");
    }
}
