using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SimPle.Api.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimPle.Api.OpenApi;

public sealed class AuthSecurityRequirementsOperationFilter : IOperationFilter
{
    public const string AccessCookieScheme = "accessCookie";
    public const string CsrfHeaderScheme = "csrfHeader";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(AuthController))
            return;

        var requirements = new OpenApiSecurityRequirement();
        var requiresAccessCookie = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Any();
        var requiresCsrfHeader = context.MethodInfo.GetCustomAttributes(true)
            .OfType<HttpPostAttribute>()
            .Any();

        if (requiresAccessCookie)
            requirements.Add(Reference(AccessCookieScheme), Array.Empty<string>());

        if (requiresCsrfHeader)
            requirements.Add(Reference(CsrfHeaderScheme), Array.Empty<string>());

        if (requirements.Count > 0)
            operation.Security = new List<OpenApiSecurityRequirement> { requirements };
    }

    private static OpenApiSecurityScheme Reference(string scheme) => new()
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = scheme
        }
    };
}
