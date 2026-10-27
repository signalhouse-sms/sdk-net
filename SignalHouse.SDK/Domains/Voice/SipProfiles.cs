using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// SIP Profiles / endpoints — single registerable SIP UAs (desk phones,
/// softphones, SIP apps) that register to Signal House with a username and
/// password. Distinct from a SIP trunk (peer-to-peer link to a PBX/carrier).
///
/// Routes are mounted under /voice/sip-profiles on the Voice service.
/// Accessed via <c>client.Voice.SipProfiles</c>.
/// </summary>
public sealed class SipProfilesDomain
{
    private readonly HttpClientWrapper _client;

    internal SipProfilesDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>List all SIP profiles for the current account. <c>GET /voice/sip-profiles</c>.</summary>
    public async Task<SignalHouseResponse> ListAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/sip-profiles", HttpMethod.Get, options: options);

    /// <summary>
    /// Get a single SIP profile by ID. Response does NOT include the password —
    /// use <see cref="GetPasswordAsync"/> to retrieve it.
    /// <c>GET /voice/sip-profiles/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-profiles/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Fetch the SIP password for this profile. Returns <c>{ password: "..." }</c>.
    /// <c>GET /voice/sip-profiles/:id/password</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetPasswordAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-profiles/{safeId}/password", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// List valid SIP transports (UDP/TCP/TLS) with their address and port.
    /// <c>GET /voice/sip-profiles/transports</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetTransportsAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/sip-profiles/transports", HttpMethod.Get, options: options);

    /// <summary>
    /// Create a SIP profile. Required: name. Optional: recordingAllowed,
    /// transcriptionEnabled, sentimentFlag. The server generates the SIP
    /// username and password; the password is returned at the TOP LEVEL of the
    /// response (not nested in sipProfile) and is the only chance to surface
    /// it without an explicit <see cref="GetPasswordAsync"/> call.
    /// <c>POST /voice/sip-profiles</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CreateAsync(IDictionary<string, object?> profileData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "profileData", profileData } });
        return await _client.RequestAsync("/voice/sip-profiles", HttpMethod.Post, profileData, options);
    }

    /// <summary>
    /// Update a SIP profile. To rotate the password, pass a new <c>password</c>
    /// value — there is no dedicated regenerate endpoint.
    /// <c>PATCH /voice/sip-profiles/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateAsync(string id, IDictionary<string, object?> updateData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-profiles/{safeId}", new HttpMethod("PATCH"), updateData, options);
    }

    /// <summary>
    /// Delete a SIP profile. Unassigns any linked numbers as a side effect.
    /// <c>DELETE /voice/sip-profiles/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-profiles/{safeId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Assign a phone number to this SIP profile (routes inbound calls on that
    /// number to the endpoint). Prefer <paramref name="e164"/>;
    /// the legacy <paramref name="phoneNumberId"/> (a
    /// Voice service phone-number UUID) is still accepted. Provide at least one.
    /// <c>POST /voice/sip-profiles/:id/assign-number</c>.
    /// </summary>
    public async Task<SignalHouseResponse> AssignNumberAsync(string id, string? phoneNumberId = null, string? e164 = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        if (e164 is null && phoneNumberId is null)
        {
            HttpClientWrapper.Require(new Dictionary<string, object?> { { "e164", e164 } });
        }
        var safeId = Uri.EscapeDataString(id);
        var body = e164 is not null
            ? new Dictionary<string, object?> { { "e164", e164 } }
            : new Dictionary<string, object?> { { "phoneNumberId", phoneNumberId } };
        return await _client.RequestAsync($"/voice/sip-profiles/{safeId}/assign-number", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Unassign a phone number from this SIP profile. Prefer <paramref name="e164"/>;
    /// the legacy <paramref name="phoneNumberId"/> (a Voice service phone-number
    /// UUID) is still accepted. Provide at least one.
    /// <c>POST /voice/sip-profiles/:id/unassign-number</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UnassignNumberAsync(string id, string? phoneNumberId = null, string? e164 = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        if (e164 is null && phoneNumberId is null)
        {
            HttpClientWrapper.Require(new Dictionary<string, object?> { { "e164", e164 } });
        }
        var safeId = Uri.EscapeDataString(id);
        var body = e164 is not null
            ? new Dictionary<string, object?> { { "e164", e164 } }
            : new Dictionary<string, object?> { { "phoneNumberId", phoneNumberId } };
        return await _client.RequestAsync($"/voice/sip-profiles/{safeId}/unassign-number", HttpMethod.Post, body, options);
    }
}
