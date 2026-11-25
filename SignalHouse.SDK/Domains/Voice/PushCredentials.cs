using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Mobile push credentials — the Apple APNs auth key and Firebase service account
/// the Voice service uses to wake your mobile app for incoming calls. One credential
/// per app; secrets are encrypted and never returned (responses show a
/// <c>fingerprint</c>). Calls the Voice service mounted under /voice. Accessed via
/// <c>client.Voice.PushCredentials</c>.
/// </summary>
public sealed class PushCredentialsDomain
{
    private readonly HttpClientWrapper _client;

    internal PushCredentialsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// List the account's push credentials (metadata only).
    /// Returns <c>{ success, data: [{ id, provider, name, appId, keyId, teamId, bundleId, projectId, clientEmail, fingerprint, createdAt, updatedAt }] }</c>.
    /// <c>GET /voice/api/v1/push-credentials</c>.
    /// </summary>
    public async Task<SignalHouseResponse> ListAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/api/v1/push-credentials", HttpMethod.Get, options: options);

    /// <summary>
    /// Get one push credential (metadata only).
    /// <c>GET /voice/api/v1/push-credentials/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/push-credentials/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Upload push credentials for one app; the key is checked on upload. Apple keys
    /// (camelCase): <c>provider</c> = "apns", <c>name</c> (optional), <c>keyId</c>,
    /// <c>teamId</c>, <c>bundleId</c>, <c>privateKey</c> (the .p8 file's contents).
    /// Firebase: <c>provider</c> = "fcm", <c>name</c> (optional),
    /// <c>serviceAccount</c> (the key file as JSON text or an object).
    /// Returns <c>{ success, data: { ...credential } }</c>.
    /// <c>POST /voice/api/v1/push-credentials</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CreateAsync(IDictionary<string, object?> credentialData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "credentialData", credentialData } });
        return await _client.RequestAsync("/voice/api/v1/push-credentials", HttpMethod.Post, credentialData, options);
    }

    /// <summary>
    /// Replace a credential's key or service account (and name). Same body as
    /// <see cref="CreateAsync"/>; the provider cannot change. Registered devices keep working.
    /// <c>PUT /voice/api/v1/push-credentials/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateAsync(string id, IDictionary<string, object?> credentialData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "credentialData", credentialData } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/push-credentials/{safeId}", new HttpMethod("PUT"), credentialData, options);
    }

    /// <summary>
    /// Delete a credential and every device registered for that app.
    /// Returns <c>{ success }</c>. <c>DELETE /voice/api/v1/push-credentials/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/push-credentials/{safeId}", HttpMethod.Delete, options: options);
    }
}
