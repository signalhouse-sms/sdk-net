using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Number Routes — one inbound route set directly on a single phone number. A
/// number's own route takes priority over its Programmable Voice Profile and
/// subgroup voice settings.
///
/// Routes are mounted under /voice/api/v1/numbers/:e164/route on the
/// Voice service. Accessed via <c>client.Voice.NumberRoutes</c>.
/// </summary>
public sealed class NumberRoutesDomain
{
    private readonly HttpClientWrapper _client;

    internal NumberRoutesDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get a number's own route and the routing currently in effect for it. A
    /// leading "+" on e164 is optional. Returns
    /// <c>{ e164, route: {...} | null, effective: {...} }</c>.
    /// <c>GET /voice/api/v1/numbers/:e164/route</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(string e164, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "e164", e164 } });
        var safeE164 = Uri.EscapeDataString(e164);
        return await _client.RequestAsync($"/voice/api/v1/numbers/{safeE164}/route", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Set (create or replace) a number's route. The whole route is replaced:
    /// fields the new routeAction doesn't use are cleared. Required: routeAction
    /// (FORWARD | WEBRTC | SIP_TRUNK | SIP_PROFILE | CALL_CONTROL | AI | VOICEMAIL).
    /// Optional: forwardToE164 (required for FORWARD, or the WEBRTC no-answer
    /// destination when noAnswerAction is FORWARD), noAnswerAction (WEBRTC only:
    /// VOICEMAIL | FORWARD | KEEP_RINGING, default VOICEMAIL), noAnswerSeconds
    /// (WEBRTC only: 5-120, default 20), routeSipTrunkId (required for SIP_TRUNK),
    /// routeSipProfileId (required for SIP_PROFILE), webhookUrl (SHML program URL,
    /// required for CALL_CONTROL), routeAgentProfileId (AI agent profile id, AGP…,
    /// required for AI), recordingEnabled. Unset fields must be omitted or null.
    /// A leading "+" on e164 is optional. Returns
    /// <c>{ e164, route: {...}, effective: {...} }</c>.
    /// <c>PUT /voice/api/v1/numbers/:e164/route</c>.
    /// </summary>
    public async Task<SignalHouseResponse> SetAsync(string e164, IDictionary<string, object?> routeData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "e164", e164 }, { "routeData", routeData } });
        var safeE164 = Uri.EscapeDataString(e164);
        return await _client.RequestAsync($"/voice/api/v1/numbers/{safeE164}/route", HttpMethod.Put, routeData, options);
    }

    /// <summary>
    /// Remove a number's own route. The number falls back to its Programmable
    /// Voice Profile, subgroup voice settings, or the default. A leading "+" on
    /// e164 is optional. Returns <c>{ e164, route: null, effective: {...} }</c>.
    /// <c>DELETE /voice/api/v1/numbers/:e164/route</c>.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteAsync(string e164, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "e164", e164 } });
        var safeE164 = Uri.EscapeDataString(e164);
        return await _client.RequestAsync($"/voice/api/v1/numbers/{safeE164}/route", HttpMethod.Delete, options: options);
    }
}
