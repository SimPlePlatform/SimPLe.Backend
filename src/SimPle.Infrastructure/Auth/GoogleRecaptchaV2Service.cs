using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;

namespace SimPle.Infrastructure.Auth;

public sealed class GoogleRecaptchaV2Service : ICaptchaVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly RecaptchaOptions _options;

    public GoogleRecaptchaV2Service(HttpClient httpClient, IOptions<RecaptchaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<bool> VerifyAsync(string responseToken, string? remoteIpAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(responseToken))
            return false;

        // Dev/test bypass — only active when DevBypassToken is explicitly configured.
        // Never set this in production; leave DevBypassToken unset or empty.
        if (!string.IsNullOrEmpty(_options.DevBypassToken) &&
            responseToken == _options.DevBypassToken)
            return true;

        var values = new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = responseToken
        };

        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
            values["remoteip"] = remoteIpAddress;

        using var response = await _httpClient.PostAsync(
            _options.VerificationUrl,
            new FormUrlEncodedContent(values),
            ct);

        if (!response.IsSuccessStatusCode)
            return false;

        var verification = await response.Content.ReadFromJsonAsync<RecaptchaVerificationResponse>(cancellationToken: ct);
        return verification?.Success == true;
    }

    private sealed class RecaptchaVerificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
