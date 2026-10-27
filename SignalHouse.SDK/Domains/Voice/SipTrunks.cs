using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// SIP Trunks — peer-to-peer SIP connections to a customer PBX or carrier.
/// Routes are mounted under /voice/sip-trunks on the Voice service.
/// Accessed via <c>client.Voice.SipTrunks</c>.
/// </summary>
public sealed class SipTrunksDomain
{
    private readonly HttpClientWrapper _client;

    internal SipTrunksDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>List all SIP trunks for the current account. <c>GET /voice/sip-trunks</c>.</summary>
    public async Task<SignalHouseResponse> ListAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/sip-trunks", HttpMethod.Get, options: options);

    /// <summary>Get a single SIP trunk by ID. <c>GET /voice/sip-trunks/:id</c>.</summary>
    public async Task<SignalHouseResponse> GetAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-trunks/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a SIP trunk. Required: name, region, connectionType
    /// (<c>IP_AUTH</c> or <c>REGISTRATION</c>). Optional: allowedIps, transport,
    /// maxSpendPerMinute, destinationHosts, sourceHosts. Registration trunks
    /// return a one-time-visible password on the response.
    /// <c>POST /voice/sip-trunks</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CreateAsync(IDictionary<string, object?> trunkData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "trunkData", trunkData } });
        return await _client.RequestAsync("/voice/sip-trunks", HttpMethod.Post, trunkData, options);
    }

    /// <summary>Update a SIP trunk. <c>PATCH /voice/sip-trunks/:id</c>.</summary>
    public async Task<SignalHouseResponse> UpdateAsync(string id, IDictionary<string, object?> updateData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-trunks/{safeId}", new HttpMethod("PATCH"), updateData, options);
    }

    /// <summary>Delete a SIP trunk. <c>DELETE /voice/sip-trunks/:id</c>.</summary>
    public async Task<SignalHouseResponse> DeleteAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-trunks/{safeId}", HttpMethod.Delete, options: options);
    }

    /// <summary>Toggle a trunk's active/inactive status. <c>POST /voice/sip-trunks/:id/toggle-active</c>.</summary>
    public async Task<SignalHouseResponse> ToggleActiveAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-trunks/{safeId}/toggle-active", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Regenerate the SIP password for a REGISTRATION-type trunk. The old
    /// password stops working immediately. Returns <c>{ password: "..." }</c>.
    /// <c>POST /voice/sip-trunks/:id/regenerate-password</c>.
    /// </summary>
    public async Task<SignalHouseResponse> RegeneratePasswordAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/sip-trunks/{safeId}/regenerate-password", HttpMethod.Post, options: options);
    }

    /// <summary>List available SIP Points of Presence. <c>GET /voice/sip-trunks/pops</c>.</summary>
    public async Task<SignalHouseResponse> GetPopsAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/sip-trunks/pops", HttpMethod.Get, options: options);

    /// <summary>
    /// Assign one or more phone numbers to a SIP trunk so inbound calls to those
    /// numbers route over the trunk. <c>POST /voice/sip-trunks/:id/assign-numbers</c>.
    /// </summary>
    public async Task<SignalHouseResponse> AssignNumbersAsync(string id, IEnumerable<string> phoneNumbers, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "phoneNumbers", phoneNumbers } });
        var safeId = Uri.EscapeDataString(id);
        var body = new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers } };
        return await _client.RequestAsync($"/voice/sip-trunks/{safeId}/assign-numbers", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Unassign one or more phone numbers from a SIP trunk. <c>POST /voice/sip-trunks/:id/unassign-numbers</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UnassignNumbersAsync(string id, IEnumerable<string> phoneNumbers, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "phoneNumbers", phoneNumbers } });
        var safeId = Uri.EscapeDataString(id);
        var body = new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers } };
        return await _client.RequestAsync($"/voice/sip-trunks/{safeId}/unassign-numbers", HttpMethod.Post, body, options);
    }
}
