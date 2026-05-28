using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimPle.Api.OpenApi;

/// <summary>Injects tag-level descriptions so each group in Swagger UI has a clear header.</summary>
public sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new()
            {
                Name = "Registration",
                Description =
                    "Create a new account and check email availability before submitting the form. " +
                    "Registration requires a valid **reCAPTCHA v2** token and a strong password. " +
                    "A verification email is sent immediately; the account is active but gated until verified.",
            },
            new()
            {
                Name = "Session",
                Description =
                    "Sign in, sign out, rotate tokens, and fetch the current user. " +
                    "On success, **login** and **refresh** set two HttpOnly cookies: " +
                    "`access_token` (15-min JWT) and `refresh_token` (7-day opaque token scoped to `/api/auth`). " +
                    "Neither token appears in the response body. " +
                    "Refresh token rotation uses **family-based reuse detection**: presenting a previously rotated " +
                    "token immediately revokes the entire session family.",
            },
            new()
            {
                Name = "Email Verification",
                Description =
                    "Confirm ownership of the registered email address. " +
                    "The verification link sent to the inbox contains a single-use token that expires after **24 hours**. " +
                    "Resending is rate-limited to 3 requests per 5 minutes per IP with a 60-second per-user cooldown.",
            },
            new()
            {
                Name = "Password Reset",
                Description =
                    "Secure, token-based password recovery. " +
                    "The forgot-password endpoint **always returns 204** regardless of whether the email is registered " +
                    "(prevents account enumeration). Reset tokens expire after **1 hour** and are single-use. " +
                    "On success, all existing sessions are revoked and a security notification email is sent.",
            },
            new()
            {
                Name = "Social Login",
                Description =
                    "Sign in or register using a **Google account** via the ID token flow. " +
                    "The browser obtains a signed JWT from Google Identity Services; the server validates it " +
                    "against Google's public keys without ever using or storing the OAuth client secret. " +
                    "Existing password accounts with the same email are linked automatically.",
            },
        };
    }
}
