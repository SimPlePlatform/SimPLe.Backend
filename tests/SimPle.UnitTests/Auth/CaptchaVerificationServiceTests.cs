using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SimPle.Application.Common.Options;
using SimPle.Infrastructure.Auth;

namespace SimPle.UnitTests.Auth;

public sealed class CaptchaVerificationServiceTests
{
    [Fact]
    public async Task VerifyAsync_ReturnsTrueAndSendsRequiredValuesWhenGoogleAcceptsToken()
    {
        var handler = new RecordingHandler("{\"success\":true}");
        var service = CreateService(handler);

        var verified = await service.VerifyAsync("captcha-response", "127.0.0.1");

        verified.Should().BeTrue();
        handler.Body.Should().Contain("secret=server-secret")
            .And.Contain("response=captcha-response")
            .And.Contain("remoteip=127.0.0.1");
    }

    [Fact]
    public async Task VerifyAsync_FailsClosedForRejectedOrMissingToken()
    {
        var handler = new RecordingHandler("{\"success\":false}");
        var service = CreateService(handler);

        (await service.VerifyAsync("rejected-response", null)).Should().BeFalse();
        (await service.VerifyAsync("", null)).Should().BeFalse();
        handler.RequestCount.Should().Be(1);
    }

    private static GoogleRecaptchaV2Service CreateService(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new RecaptchaOptions
            {
                SecretKey = "server-secret",
                VerificationUrl = "https://www.google.com/recaptcha/api/siteverify"
            }));

    private sealed class RecordingHandler(string responseJson) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestCount++;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };
        }
    }
}
