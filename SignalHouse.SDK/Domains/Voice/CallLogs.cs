using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Call Logs — the account-scoped voice call history (Voice service
/// <c>/voice/api/v1/call-logs</c>). Distinct from <c>client.Voice.Calls</c>, which is
/// the Twilio-compatible programmatic call-control surface; this is the richer log
/// read surface with cost, recording, and voice-config attribution used by the
/// portal Call Logs view. Accessed via <c>client.Voice.CallLogs</c>.
/// </summary>
public sealed class CallLogsDomain
{
    private readonly HttpClientWrapper _client;

    internal CallLogsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// List call logs for the current account, paginated and filterable.
    /// Optional filter keys (camelCase): <c>page</c>, <c>limit</c>, <c>direction</c>,
    /// <c>status</c>, <c>callSource</c>, <c>subgroupId</c>, <c>sipTrunkId</c>,
    /// <c>from</c>, <c>to</c>, <c>q</c>, <c>sentiment</c>, <c>voicemail</c>,
    /// <c>dateFrom</c>, <c>dateTo</c>, <c>sort</c>, <c>order</c>.
    /// Returns <c>{ success, data: [...], meta: {...} }</c>.
    /// <c>GET /voice/api/v1/call-logs</c>.
    /// </summary>
    public async Task<SignalHouseResponse> ListAsync(IDictionary<string, object?>? filters = null, RequestOptions? options = null)
    {
        var parameters = new Dictionary<string, string?>();
        if (filters is not null)
        {
            foreach (var kvp in filters)
            {
                if (kvp.Value is not null)
                {
                    var str = kvp.Value.ToString();
                    if (!string.IsNullOrEmpty(str))
                    {
                        parameters[kvp.Key] = str;
                    }
                }
            }
        }

        var qs = HttpClientWrapper.GetQueryString(parameters);
        return await _client.RequestAsync($"/voice/api/v1/call-logs{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single call log by its UUID or callId. Returns <c>{ success, data: {...} }</c>.
    /// <c>GET /voice/api/v1/call-logs/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-logs/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a short-TTL presigned playback URL for a call's recording. Account-scoped
    /// — only the owning account can presign. 404 if the call has no recording.
    /// Returns <c>{ success, data: { callId, url, expiresIn } }</c>.
    /// <c>GET /voice/api/v1/call-logs/:id/recording</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetRecordingAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-logs/{safeId}/recording", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Mark a voicemail (a call log with a recording) read or unread. Account-scoped.
    /// Returns <c>{ success, data: { id, voicemailReadAt } }</c>.
    /// <c>PATCH /voice/api/v1/call-logs/:id/voicemail-read</c>.
    /// </summary>
    /// <param name="id">The call log UUID or callId.</param>
    /// <param name="read"><c>true</c> = read, <c>false</c> = unread. Defaults to <c>true</c>.</param>
    /// <param name="options">Additional options for the request.</param>
    /// <returns>The API response.</returns>
    public async Task<SignalHouseResponse> MarkVoicemailReadAsync(string id, bool read = true, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        var body = new Dictionary<string, object?> { { "read", read } };
        return await _client.RequestAsync($"/voice/api/v1/call-logs/{safeId}/voicemail-read", new HttpMethod("PATCH"), body, options);
    }
}
