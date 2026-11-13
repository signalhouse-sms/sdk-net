using System.Net;
using System.Text.Json;
using SignalHouse.SDK;
using SignalHouse.SDK.Models;
using Xunit;

namespace SignalHouse.SDK.Tests;

public sealed class RcsAgentRequestOptionsTests
{
    [Fact]
    public async Task CreateAsync_preserves_positional_and_named_request_options()
    {
        var (client, handler) = CreateClient();
        var options = CreateOptions();

        await client.Rcs.Agents.CreateAsync("B1", null, null, null, null, null, null, null, null, null, options);
        await client.Rcs.Agents.CreateAsync("B1", options: options);

        Assert.Equal(2, handler.Requests.Count);
        foreach (var request in handler.Requests)
        {
            AssertRequestOptions(request);
            using var body = JsonDocument.Parse(request.Body);
            Assert.False(body.RootElement.TryGetProperty("autofill", out _));
        }
    }

    [Fact]
    public async Task UpdateAsync_preserves_positional_and_named_request_options()
    {
        var (client, handler) = CreateClient();
        var options = CreateOptions();

        await client.Rcs.Agents.UpdateAsync("R1", null, null, false, null, null, null, null, null, null, null, null, false, false, options);
        await client.Rcs.Agents.UpdateAsync("R1", options: options);

        Assert.Equal(2, handler.Requests.Count);
        foreach (var request in handler.Requests)
        {
            AssertRequestOptions(request);
            using var body = JsonDocument.Parse(request.Body);
            Assert.False(body.RootElement.TryGetProperty("autofill", out _));
        }
    }

    [Fact]
    public async Task CreateAsync_sends_autofill_and_request_options()
    {
        var (client, handler) = CreateClient();
        var options = CreateOptions();
        var autofill = new Dictionary<string, object?> { ["consent.optInDetails"] = new { source = "brand" } };

        await client.Rcs.Agents.CreateAsync("B1", autofill: autofill, options: options);

        var request = Assert.Single(handler.Requests);
        AssertRequestOptions(request);
        using var body = JsonDocument.Parse(request.Body);
        Assert.True(body.RootElement.GetProperty("autofill").TryGetProperty("consent.optInDetails", out _));
    }

    [Fact]
    public async Task UpdateAsync_sends_autofill_and_request_options()
    {
        var (client, handler) = CreateClient();
        var options = CreateOptions();
        var autofill = new Dictionary<string, object?> { ["consent.optInDetails"] = new { source = "brand" } };

        await client.Rcs.Agents.UpdateAsync("R1", autofill: autofill, options: options);

        var request = Assert.Single(handler.Requests);
        AssertRequestOptions(request);
        using var body = JsonDocument.Parse(request.Body);
        Assert.True(body.RootElement.GetProperty("autofill").TryGetProperty("consent.optInDetails", out _));
    }

    private static (SignalHouseClient Client, CaptureHandler Handler) CreateClient()
    {
        var handler = new CaptureHandler();
        return (new SignalHouseClient("default-key", "https://example.invalid", httpClient: new HttpClient(handler)), handler);
    }

    private static RequestOptions CreateOptions() => new()
    {
        Token = "override-token",
        IdempotencyKey = "request-key",
        Headers = new Dictionary<string, string> { ["X-Test-Header"] = "present" },
    };

    private static void AssertRequestOptions(CapturedRequest request)
    {
        Assert.Equal("Bearer", request.AuthorizationScheme);
        Assert.Equal("override-token", request.AuthorizationParameter);
        Assert.Equal("request-key", request.IdempotencyKey);
        Assert.Equal("present", request.TestHeader);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                request.Headers.TryGetValues("Idempotency-Key", out var idempotencyValues) ? idempotencyValues.Single() : null,
                request.Headers.TryGetValues("X-Test-Header", out var testValues) ? testValues.Single() : null,
                await request.Content!.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }

    private sealed record CapturedRequest(
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        string? IdempotencyKey,
        string? TestHeader,
        string Body);
}
