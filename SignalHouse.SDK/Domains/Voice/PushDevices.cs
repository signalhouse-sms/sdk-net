using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Mobile push devices — your app registers each device so incoming calls wake it.
/// Call these with the user's <b>voice token</b> (from <c>client.Voice.Tokens.CreateAsync</c>,
/// minted with a <c>subgroupId</c>) as <see cref="RequestOptions.Token"/>, not your API key.
/// The device is tied to that token's identity. Calls the Voice service mounted under
/// /voice. Accessed via <c>client.Voice.PushDevices</c>.
/// </summary>
public sealed class PushDevicesDomain
{
    private readonly HttpClientWrapper _client;

    internal PushDevicesDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Register this device for push. Call it after each token mint and whenever the OS
    /// gives the app a new push token; a push token already registered to another user
    /// moves to this one. Keys (camelCase): <c>provider</c> ("apns" or "fcm"),
    /// <c>deviceToken</c>, <c>environment</c> (apns only: "sandbox" for development
    /// builds, default "production"), <c>appId</c> (only when the account has several
    /// apps for the provider).
    /// Returns <c>{ success, data: { id, identity, provider, environment, appId, registeredAt } }</c>.
    /// <c>POST /voice/api/v1/push-devices</c>.
    /// </summary>
    public async Task<SignalHouseResponse> RegisterAsync(IDictionary<string, object?> deviceData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "deviceData", deviceData } });
        return await _client.RequestAsync("/voice/api/v1/push-devices", HttpMethod.Post, deviceData, options);
    }

    /// <summary>
    /// Unregister this device, e.g. when the user signs out.
    /// Returns <c>{ success, data: { removed } }</c>.
    /// <c>DELETE /voice/api/v1/push-devices/:deviceToken</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UnregisterAsync(string deviceToken, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "deviceToken", deviceToken } });
        var safeToken = Uri.EscapeDataString(deviceToken);
        return await _client.RequestAsync($"/voice/api/v1/push-devices/{safeToken}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// The pushes recently sent to the voice token's user, newest first (kept 7 days),
    /// for debugging. <paramref name="limit"/> is 1 to 100 (default 20).
    /// Returns <c>{ success, data: [{ id, callUuid, provider, kind, status, error, createdAt }] }</c>.
    /// <c>GET /voice/api/v1/push-devices/deliveries</c>.
    /// </summary>
    public async Task<SignalHouseResponse> ListDeliveriesAsync(int? limit = null, RequestOptions? options = null)
    {
        var query = limit.HasValue ? $"?limit={limit.Value}" : string.Empty;
        return await _client.RequestAsync($"/voice/api/v1/push-devices/deliveries{query}", HttpMethod.Get, options: options);
    }
}
