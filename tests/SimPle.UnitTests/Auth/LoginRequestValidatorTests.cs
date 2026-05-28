using FluentAssertions;
using SimPle.Application.Auth.DTOs;
using SimPle.Application.Auth.Validators;

namespace SimPle.UnitTests.Auth;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    private static LoginRequestDto Valid() =>
        new("user@example.com", "validpassword", "test-captcha-token");

    private IReadOnlyList<string> ErrorsFor(LoginRequestDto dto) =>
        _validator.Validate(dto).Errors.Select(e => e.ErrorMessage).ToList();

    [Fact]
    public void Valid_request_has_no_errors()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_email_or_username_fails_with_required_message()
    {
        ErrorsFor(Valid() with { EmailOrUsername = "" })
            .Should().Contain("Email or username is required.");
    }

    [Fact]
    public void EmailOrUsername_over_254_chars_fails()
    {
        ErrorsFor(Valid() with { EmailOrUsername = new string('a', 255) })
            .Should().Contain("Input is too long.");
    }

    [Fact]
    public void EmailOrUsername_at_max_length_passes()
    {
        _validator.Validate(Valid() with { EmailOrUsername = new string('a', 254) })
            .Errors.Should().NotContain(e => e.PropertyName == nameof(LoginRequestDto.EmailOrUsername));
    }

    [Fact]
    public void Empty_password_fails_with_required_message()
    {
        ErrorsFor(Valid() with { Password = "" })
            .Should().Contain("Password is required.");
    }

    [Fact]
    public void Password_over_128_chars_fails()
    {
        ErrorsFor(Valid() with { Password = new string('a', 129) })
            .Should().Contain("Password is too long.");
    }

    [Fact]
    public void Password_at_max_length_passes()
    {
        _validator.Validate(Valid() with { Password = new string('a', 128) })
            .Errors.Should().NotContain(e => e.PropertyName == nameof(LoginRequestDto.Password));
    }

    [Fact]
    public void Empty_captcha_token_fails_with_required_message()
    {
        ErrorsFor(Valid() with { CaptchaToken = "" })
            .Should().Contain("Please complete the CAPTCHA check.");
    }
}
