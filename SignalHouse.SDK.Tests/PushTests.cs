using System.Net;
using SignalHouse.SDK;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;
using Xunit;

namespace SignalHouse.SDK.Tests;

public sealed class PushTests
{
    private const string Id = "5a3c2f8e-1b7d-4c9a-9e2f-0d4b6a8c1e33";
    private const string Credentials = "https://example.invalid/voice/api/v1/push-credentials";
    private const string Devices = "https://example.invalid/voice/api/v1/push-devices";

    [Fact]
    public async Task Push_credentials_use_the_expected_methods_paths_and_bodies()
    {
        var (client, handler) = CreateClient();
        var creds = client.Voice.PushCredentials;

        await creds.ListAsync();
        await creds.GetAsync(Id);
        await creds.CreateAsync(new Dictionary<string, object?> { ["provider"] = "apns", ["keyId"] = "ABC123DEFG" });
        await creds.UpdateAsync(Id, new Dictionary<string, object?> { ["provider"] = "fcm", ["serviceAccount"] = "{}" });
        await creds.DeleteAsync(Id);

        Assert.Collection(handler.Requests,
            r => Assert.Equal(("GET", Credentials, (string?)null), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("GET", $"{Credentials}/{Id}", (string?)null), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("POST", Credentials, "{\"provider\":\"apns\",\"keyId\":\"ABC123DEFG\"}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("PUT", $"{Credentials}/{Id}", "{\"provider\":\"fcm\",\"serviceAccount\":\"{}\"}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("DELETE", $"{Credentials}/{Id}", (string?)null), (r.Method, r.Url, r.Body)));
    }

    [Fact]
    public async Task Push_devices_send_the_voice_token_and_escape_the_device_token()
    {
        var (client, handler) = CreateClient();
        var devices = client.Voice.PushDevices;
        var voice = new RequestOptions { Token = "voice-jwt" };

        await devices.RegisterAsync(new Dictionary<string, object?> { ["provider"] = "fcm", ["deviceToken"] = "fcm:tok/1" }, voice);
        await devices.UnregisterAsync("fcm:tok/1", voice);
        await devices.ListDeliveriesAsync(5, voice);
        await devices.ListDeliveriesAsync(options: voice);

        Assert.Collection(handler.Requests,
            r => Assert.Equal(("POST", Devices, "{\"provider\":\"fcm\",\"deviceToken\":\"fcm:tok/1\"}", "voice-jwt"), (r.Method, r.Url, r.Body, r.Token)),
            r => Assert.Equal(("DELETE", $"{Devices}/fcm%3Atok%2F1", "voice-jwt"), (r.Method, r.Url, r.Token)),
            r => Assert.Equal(("GET", $"{Devices}/deliveries?limit=5"), (r.Method, r.Url)),
            r => Assert.Equal(("GET", $"{Devices}/deliveries"), (r.Method, r.Url)));
    }

    [Fact]
    public async Task Missing_arguments_are_rejected_before_sending()
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ValidationException>(() => client.Voice.PushCredentials.GetAsync(null!));
        await Assert.ThrowsAsync<ValidationException>(() => client.Voice.PushCredentials.CreateAsync(null!));
        await Assert.ThrowsAsync<ValidationException>(() => client.Voice.PushDevices.UnregisterAsync(null!));
        Assert.Empty(handler.Requests);
    }

    private static (SignalHouseClient Client, CaptureHandler Handler) CreateClient()
    {
        var handler = new CaptureHandler();
        return (new SignalHouseClient("default-key", "https://example.invalid", httpClient: new HttpClient(handler)), handler);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Method.Method,
                request.RequestUri!.AbsoluteUri,
                request.Headers.Authorization?.Parameter,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }

    private sealed record CapturedRequest(string Method, string Url, string? Token, string? Body);
}
