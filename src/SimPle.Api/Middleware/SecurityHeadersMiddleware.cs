namespace SimPle.Api.Middleware;

/// <summary>
/// Adds defensive HTTP response headers on every response.
/// Protects against MIME sniffing, clickjacking, cross-origin leaks, and (on HTTPS) downgrade attacks.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;

        // Prevent browsers from MIME-sniffing away from the declared Content-Type.
        h["X-Content-Type-Options"] = "nosniff";

        // Block the response from being framed (protects against clickjacking).
        h["X-Frame-Options"] = "DENY";

        // Control how much referrer information is included with requests.
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Disable the legacy XSS auditor (it introduced its own vulnerabilities in older browsers).
        h["X-XSS-Protection"] = "0";

        // Opt out of browser features the app does not use.
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // HSTS: tell HTTPS clients not to fall back to HTTP for 2 years.
        // Only sent over HTTPS — never over plain HTTP — to avoid poisoning dev environments.
        if (context.Request.IsHttps)
            h["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains";

        await _next(context);
    }
}
