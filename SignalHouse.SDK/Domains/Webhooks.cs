using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Webhook management operations.
/// </summary>
public sealed class WebhooksDomain
{
    private readonly HttpClientWrapper _client;

    internal WebhooksDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get a list of webhooks with optional filters. Each returned endpoint carries
    /// "hasSigningSecret" (bool) instead of the signing secret itself — see CreateWebhookAsync.
    /// </summary>
    public async Task<SignalHouseResponse> GetWebhooksAsync(
        string? id = null,
        string? groupId = null,
        string? endpointType = null,
        string? phoneNumber = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
            { "groupId", groupId },
            { "endpointType", endpointType },
            { "phoneNumber", phoneNumber },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/webhook{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new webhook. The response includes a "signingSecret" (plaintext HMAC-SHA256 secret)
    /// used to sign every delivery to this endpoint via the X-SignalHouse-Signature header. It is
    /// returned exactly once, here — store it now, it is never exposed again by any read.
    /// <para>
    /// webhookData "authType" is one of "NONE" (default), "BASIC", "API_KEY", "OAUTH2" or "HMAC". "HMAC"
    /// sends no credentials (the receiver authenticates by the signature alone) and must be sent without
    /// "credentials" or "apiHeaderPrefix".
    /// </para>
    /// <para>
    /// webhookData "eventSettings" holds per-event options keyed by event name: "NUMBER_HEALTH_BELOW_THRESHOLD"
    /// and "CAMPAIGN_HEALTH_BELOW_THRESHOLD" each take { "threshold": n }, the 7-day health score (1.5–10, in
    /// 0.5 steps) below which the event fires. A subscribed health event without a threshold is stored with
    /// the default, 6.
    /// </para>
    /// </summary>
    public async Task<SignalHouseResponse> CreateWebhookAsync(
        Dictionary<string, object> webhookData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "webhookData", webhookData } });

        return await _client.RequestAsync("/webhook", HttpMethod.Post, webhookData, options);
    }

    /// <summary>
    /// Update an existing webhook. Like every other read of a webhook, the response carries
    /// "hasSigningSecret" (bool), never the signing secret itself — see CreateWebhookAsync.
    /// <para>
    /// Switching "authType" to "NONE" or "HMAC" clears the stored credentials. "HMAC" is rejected for an
    /// endpoint with no signing secret; call RotateWebhookSecretAsync first.
    /// </para>
    /// <para>
    /// updateData "eventSettings" changes per-event options by event name ("NUMBER_HEALTH_BELOW_THRESHOLD",
    /// "CAMPAIGN_HEALTH_BELOW_THRESHOLD", each { "threshold": n }, 1.5–10 in 0.5 steps); events you omit keep
    /// their stored settings.
    /// </para>
    /// </summary>
    public async Task<SignalHouseResponse> UpdateWebhookAsync(
        string id,
        Dictionary<string, object> updateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/webhook/{safeId}", HttpMethod.Put, updateData, options);
    }

    /// <summary>
    /// Rotate a webhook's signing secret, or give a webhook created before signing its first one.
    /// The response includes the new plaintext signingSecret, returned this one time only. The replaced
    /// secret keeps working for graceSeconds (default 86400, max 604800, 0 = stop now).
    /// </summary>
    public async Task<SignalHouseResponse> RotateWebhookSecretAsync(
        string id,
        int? graceSeconds = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        var body = new Dictionary<string, object>();
        if (graceSeconds.HasValue) body["graceSeconds"] = graceSeconds.Value;
        return await _client.RequestAsync($"/webhook/{safeId}/rotate-secret", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Delete a webhook (mark as inactive).
    /// </summary>
    public async Task<SignalHouseResponse> DeleteWebhookAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/webhook/{safeId}", HttpMethod.Delete, options: options);
    }
}
