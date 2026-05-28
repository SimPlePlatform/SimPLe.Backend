using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SimPle.Api.Middleware;

namespace SimPle.IntegrationTests.Auth;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Middleware_ReturnsGenericJsonWithoutExceptionDetails()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("database connection secret detail"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("An unexpected error occurred.")
            .And.NotContain("database connection secret detail");
    }
}
