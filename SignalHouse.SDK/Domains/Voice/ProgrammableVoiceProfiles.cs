using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Programmable Voice Profiles — group a set of the account's numbers (across
/// one or more subgroups) under a single inbound call-handling decision routed
/// through Signal House's own voice servers — the same idea as a SIP trunk, but
/// without a customer SBC. A number can belong to at most one profile and is
/// mutually exclusive with SIP trunks.
///
/// Routes are mounted under /voice/api/v1/programmable-voice-profiles on the
/// Voice service. Accessed via <c>client.Voice.ProgrammableVoiceProfiles</c>.
/// </summary>
public sealed class ProgrammableVoiceProfilesDomain
{
    private readonly HttpClientWrapper _client;

    internal ProgrammableVoiceProfilesDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// List all Programmable Voice Profiles for the current account. Returns
    /// <c>{ profiles: [...] }</c>.
    /// <c>GET /voice/api/v1/programmable-voice-profiles</c>.
    /// </summary>
    public async Task<SignalHouseResponse> ListAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/api/v1/programmable-voice-profiles", HttpMethod.Get, options: options);

    /// <summary>
    /// Get a single Programmable Voice Profile by ID. Returns <c>{ profile: {...} }</c>.
    /// <c>GET /voice/api/v1/programmable-voice-profiles/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/programmable-voice-profiles/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a Programmable Voice Profile. Required: name, subgroupIds. Optional:
    /// region, routeAction (FORWARD | WEBRTC | SIP_TRUNK | SIP_PROFILE | CALL_CONTROL,
    /// default FORWARD) — deliver the call to a PSTN number (FORWARD), ring the
    /// profile's subgroup registered softphones (WEBRTC), send to a SIP trunk
    /// (SIP_TRUNK), ring a registered SIP endpoint (SIP_PROFILE), or hand the call to
    /// your programmable call-control webhook that returns SHML (CALL_CONTROL);
    /// forwardToE164 (required when routeAction is FORWARD), routeSipTrunkId (required
    /// when routeAction is SIP_TRUNK), routeSipProfileId (required when routeAction is
    /// SIP_PROFILE), webhookUrl (inbound voice webhook / SHML program URL, required
    /// when routeAction is CALL_CONTROL), forwardAfterSeconds, recordingEnabled,
    /// enabled. Returns <c>{ profile: {...} }</c>.
    /// <c>POST /voice/api/v1/programmable-voice-profiles</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CreateAsync(IDictionary<string, object?> profileData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "profileData", profileData } });
        return await _client.RequestAsync("/voice/api/v1/programmable-voice-profiles", HttpMethod.Post, profileData, options);
    }

    /// <summary>
    /// Update an existing Programmable Voice Profile (partial). Any of the create
    /// keys may be supplied, including routeAction (FORWARD | WEBRTC | SIP_TRUNK |
    /// SIP_PROFILE | CALL_CONTROL), forwardToE164, routeSipTrunkId (required when
    /// routeAction is SIP_TRUNK), routeSipProfileId (required when routeAction is
    /// SIP_PROFILE), and webhookUrl (required when routeAction is CALL_CONTROL).
    /// Returns <c>{ profile: {...} }</c>.
    /// <c>PATCH /voice/api/v1/programmable-voice-profiles/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateAsync(string id, IDictionary<string, object?> updateData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/programmable-voice-profiles/{safeId}", new HttpMethod("PATCH"), updateData, options);
    }

    /// <summary>
    /// Toggle a profile active/inactive. Returns <c>{ profile: {...} }</c>.
    /// <c>POST /voice/api/v1/programmable-voice-profiles/:id/toggle-active</c>.
    /// </summary>
    public async Task<SignalHouseResponse> ToggleActiveAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/programmable-voice-profiles/{safeId}/toggle-active", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Delete a Programmable Voice Profile. Its number assignments cascade away,
    /// so those numbers fall back to subgroup/global routing. Returns
    /// <c>{ message: "..." }</c>.
    /// <c>DELETE /voice/api/v1/programmable-voice-profiles/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/programmable-voice-profiles/{safeId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Assign a phone number (by E.164) to this profile. Rejected if the number is
    /// already configured on a SIP trunk/endpoint or another profile — it must be
    /// de-configured there first (mutual exclusivity). Returns <c>{ profile: {...} }</c>.
    /// <c>POST /voice/api/v1/programmable-voice-profiles/:id/assign-number</c>.
    /// </summary>
    public async Task<SignalHouseResponse> AssignNumberAsync(string id, string e164, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "e164", e164 } });
        var safeId = Uri.EscapeDataString(id);
        var body = new Dictionary<string, object?> { { "e164", e164 } };
        return await _client.RequestAsync($"/voice/api/v1/programmable-voice-profiles/{safeId}/assign-number", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Unassign a phone number (by E.164) from this profile. Returns
    /// <c>{ profile: {...} }</c>.
    /// <c>POST /voice/api/v1/programmable-voice-profiles/:id/unassign-number</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UnassignNumberAsync(string id, string e164, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "e164", e164 } });
        var safeId = Uri.EscapeDataString(id);
        var body = new Dictionary<string, object?> { { "e164", e164 } };
        return await _client.RequestAsync($"/voice/api/v1/programmable-voice-profiles/{safeId}/unassign-number", HttpMethod.Post, body, options);
    }
}
