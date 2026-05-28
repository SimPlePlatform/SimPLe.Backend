using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SimPle.Api.Models;
using SimPle.Application.Auth.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimPle.Api.OpenApi;

/// <summary>Enriches request and response schemas with field-level descriptions, examples, and constraints.</summary>
public sealed class AuthSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(RegisterRequestDto))
        {
            schema.Description = "New account details. Requires a valid reCAPTCHA v2 token.";
            DescribeString(schema, "username",
                "Public-facing username. Letters, numbers, underscores and hyphens only.",
                3, 30, pattern: "^[a-zA-Z0-9_\\-]+$", example: "mohan_g");
            DescribeString(schema, "email",
                "Email address used to sign in. A verification link is sent here after registration.",
                1, 254, example: "mohan@example.com");
            DescribeString(schema, "password",
                "Password — minimum 8 characters with at least 2 of: uppercase, lowercase, numbers, symbols. " +
                "Common passwords (e.g. 'password1') are rejected.",
                8, 128, example: "MyPass1!", writeOnly: true);
            DescribeString(schema, "confirmPassword",
                "Must exactly match the password field.",
                8, 128, example: "MyPass1!", writeOnly: true);
            DescribeString(schema, "captchaToken",
                "One-time Google reCAPTCHA v2 response token from the browser checkbox widget. " +
                "Obtain from `grecaptcha.getResponse()` on the frontend.",
                1, 4096, example: "03AGdBq24PBgg...", writeOnly: true);
        }
        else if (context.Type == typeof(LoginRequestDto))
        {
            schema.Description = "Sign-in credentials. Requires a valid reCAPTCHA v2 token.";
            DescribeString(schema, "emailOrUsername",
                "Registered email address or username.",
                1, 254, example: "mohan@example.com");
            DescribeString(schema, "password",
                "Account password.",
                1, 128, example: "MyPass1!", writeOnly: true);
            DescribeString(schema, "captchaToken",
                "One-time Google reCAPTCHA v2 response token from the browser checkbox widget.",
                1, 4096, example: "03AGdBq24PBgg...", writeOnly: true);
        }
        else if (context.Type == typeof(ForgotPasswordRequestDto))
        {
            schema.Description = "Email address to send the password reset link to. " +
                "The endpoint always returns 204 regardless of whether the address is registered.";
            DescribeString(schema, "email",
                "Email address associated with the account.",
                1, 254, example: "mohan@example.com");
        }
        else if (context.Type == typeof(ResetPasswordRequestDto))
        {
            schema.Description = "New password and the single-use reset token from the email link.";
            DescribeString(schema, "token",
                "Raw reset token copied from the `?token=` query parameter in the password reset email. " +
                "Single-use and expires after 1 hour.",
                1, 4096, example: "abc123...", writeOnly: true);
            DescribeString(schema, "newPassword",
                "New password — minimum 8 characters with at least 2 character types.",
                8, 128, example: "NewSecure1!", writeOnly: true);
            DescribeString(schema, "confirmNewPassword",
                "Must exactly match the newPassword field.",
                8, 128, example: "NewSecure1!", writeOnly: true);
        }
        else if (context.Type == typeof(VerifyEmailRequestDto))
        {
            schema.Description = "Single-use email verification token from the link sent to the user's inbox.";
            DescribeString(schema, "token",
                "Raw verification token copied from the `?token=` query parameter in the verification email. " +
                "Single-use and expires after 24 hours.",
                1, 4096, example: "abc123...", writeOnly: true);
        }
        else if (context.Type == typeof(GoogleCallbackRequestDto))
        {
            schema.Description =
                "Google ID token returned by the Google Identity Services (GIS) client-side SDK after the user " +
                "selects a Google account. The token is a signed JWT validated server-side against Google's " +
                "public JWKS endpoint — the client secret is never used.";
            DescribeString(schema, "idToken",
                "Signed JWT credential from `response.credential` in the GIS callback. " +
                "Expires after 1 hour; never reuse a stale credential.",
                1, 4096, example: "eyJhbGciOiJSUzI1NiIs...", writeOnly: true);
        }
        else if (context.Type == typeof(UserDto))
        {
            schema.Description =
                "Safe user representation returned after sign-in, registration, refresh, and /me. " +
                "Never includes password hashes, raw tokens, or OAuth credentials.";
            DescribeReadOnlyString(schema, "id", "Unique user identifier (UUID v4).");
            DescribeReadOnlyString(schema, "username", "Public-facing username. Unique across the platform.");
            DescribeReadOnlyString(schema, "displayName", "User's display name shown in the UI.");
            DescribeReadOnlyString(schema, "email", "Registered email address.");
            DescribeReadOnlyString(schema, "initials", "One or two initials derived from the display name. Used for avatar placeholders.");
            DescribeReadOnlyString(schema, "color", "Hex accent colour assigned to the user's avatar.");
            DescribeReadOnlyString(schema, "role", "Account role: Player, Moderator, or Admin.");
            if (schema.Properties.TryGetValue("isEmailVerified", out var verified))
                verified.Description = "Whether the user has verified their email address. Unverified users may have restricted access.";
            if (schema.Properties.TryGetValue("createdAt", out var created))
                created.Description = "UTC timestamp of account creation.";
        }
        else if (context.Type == typeof(ApiErrorResponse))
        {
            schema.Description = "Standard error envelope returned on all non-2xx responses.";
        }
        else if (context.Type == typeof(ApiErrorDetail))
        {
            schema.Description = "Machine-readable error detail.";
            DescribeReadOnlyString(schema, "code",
                "Stable dot-namespaced error code such as `Auth.InvalidCredentials`. Safe to switch on in client code.");
            DescribeReadOnlyString(schema, "message",
                "Human-readable message safe to display to the end user. Not intended for parsing.");
        }
    }

    private static void DescribeString(
        OpenApiSchema schema,
        string propertyName,
        string description,
        int minimumLength,
        int maximumLength,
        string? pattern = null,
        string? example = null,
        bool writeOnly = false)
    {
        if (!schema.Properties.TryGetValue(propertyName, out var property))
            return;

        property.Description = description;
        property.MinLength = minimumLength;
        property.MaxLength = maximumLength;
        property.Pattern = pattern;
        property.WriteOnly = writeOnly;
        property.Example = example is null ? null : new OpenApiString(example);
        schema.Required.Add(propertyName);
    }

    private static void DescribeReadOnlyString(OpenApiSchema schema, string propertyName, string description)
    {
        if (schema.Properties.TryGetValue(propertyName, out var property))
            property.Description = description;
    }
}
