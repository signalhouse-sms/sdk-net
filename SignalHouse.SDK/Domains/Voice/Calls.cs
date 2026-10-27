using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Voice calls — REST API for placing outbound calls and inspecting call logs.
/// Wraps the Voice service's <c>/voice/v1/calls</c> surface.
/// Accessed via <c>client.Voice.Calls</c>.
///
/// Origination modes:
/// <list type="bullet">
///   <item><b>Single-leg</b> (no trunk/profile/identity): Twilio-style direct outbound — dial <c>to</c> from caller-ID <c>from</c>. Customer's <c>from</c> must be an account-owned number.</item>
///   <item><b>SIP trunk</b> (<c>sipTrunkId</c>): Route via a configured trunk; the trunk's auth/ACL governs caller-ID rules.</item>
///   <item><b>Two-leg-with-bridge</b> (<c>sipProfileId</c> OR <c>toIdentity</c>): Ring the SDK-registered endpoint first, then dial <c>to</c> and bridge. <c>toIdentity</c> resolves to the most recently-minted unexpired ephemeral identity for that name.</item>
/// </list>
/// </summary>
public sealed class CallsDomain
{
    private readonly HttpClientWrapper _client;

    internal CallsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Create an outbound call. Required: <c>to</c>, <c>from</c>. Optional routing fields
    /// are mutually exclusive: <c>sipTrunkId</c>, <c>sipProfileId</c>, or <c>toIdentity</c>.
    /// Other optional fields: <c>answerUrl</c>, <c>answerMethod</c>, <c>statusCallback</c>,
    /// <c>statusCallbackMethod</c>, <c>recordingEnabled</c>, <c>transcriptionEnabled</c>,
    /// <c>ivrFlowId</c>, <c>metadata</c>.
    /// <c>POST /voice/v1/calls</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CreateAsync(IDictionary<string, object?> callData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "callData", callData } });
        return await _client.RequestAsync("/voice/v1/calls", HttpMethod.Post, callData, options);
    }

    /// <summary>
    /// List call logs for the current account, paginated and filterable.
    /// Optional filters: <c>page</c>, <c>limit</c>, <c>status</c>, <c>direction</c>,
    /// <c>from</c>, <c>to</c>, <c>dateFrom</c>, <c>dateTo</c>.
    /// <c>GET /voice/v1/calls</c>.
    /// </summary>
    public async Task<SignalHouseResponse> ListAsync(
        int? page = null,
        int? limit = null,
        string? status = null,
        string? direction = null,
        string? from = null,
        string? to = null,
        string? dateFrom = null,
        string? dateTo = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "status", status },
            { "direction", direction },
            { "from", from },
            { "to", to },
            { "date_from", dateFrom },
            { "date_to", dateTo },
        });

        return await _client.RequestAsync($"/voice/v1/calls{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single call log by ID. <c>GET /voice/v1/calls/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/v1/calls/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Hang up an in-progress call. Optional <paramref name="reason"/> recorded on the
    /// call log (<c>NORMAL</c>, <c>BUSY</c>, <c>NO_ANSWER</c>, or <c>REJECTED</c>);
    /// defaults to <c>NORMAL</c>.
    /// <c>POST /voice/v1/calls/:id/hangup</c>.
    /// </summary>
    public async Task<SignalHouseResponse> HangupAsync(string id, string? reason = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        var body = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(reason))
        {
            body["reason"] = reason;
        }
        return await _client.RequestAsync($"/voice/v1/calls/{safeId}/hangup", HttpMethod.Post, body, options);
    }
}
