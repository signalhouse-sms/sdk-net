using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Subgroup management operations.
/// </summary>
public sealed class SubgroupsDomain
{
    private readonly HttpClientWrapper _client;

    internal SubgroupsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get a list of subgroups with optional filters.
    /// </summary>
    public async Task<SignalHouseResponse> GetSubgroupsAsync(
        string? id = null,
        string? groupId = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
            { "groupId", groupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/subgroup{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get resource counts grouped by subgroup ID. Only signalhouse_admin can query all groups;
    /// every other allowed role is restricted to its JWT group scope.
    /// </summary>
    public async Task<SignalHouseResponse> GetSubgroupCountsAsync(
        string? groupId = null,
        string? subgroupId = null,
        int? limit = null,
        int? offset = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "limit", limit?.ToString() },
            { "offset", offset?.ToString() },
        });
        return await _client.RequestAsync($"/subgroup/counts{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new subgroup.
    /// subgroupData may include optional carrierIdFamily and carrierIdRegion fields.
    /// Setting those fields requires signalhouse_api, signalhouse_admin, signalhouse_user,
    /// api, admin, developer, or billing role.
    /// </summary>
    public async Task<SignalHouseResponse> CreateSubgroupAsync(
        Dictionary<string, object> subgroupData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "subgroupData", subgroupData } });

        return await _client.RequestAsync("/subgroup", HttpMethod.Post, subgroupData, options);
    }

    /// <summary>
    /// Update an existing subgroup.
    /// updateData may include optional carrierIdFamily and carrierIdRegion fields.
    /// Setting those fields requires signalhouse_api, signalhouse_admin, signalhouse_user,
    /// api, admin, developer, or billing role.
    /// updateData may also include optional cnamAutoApply (bool, turns caller name auto-apply on or off)
    /// and cnamBrandId (string, the verified brand caller names come from for numbers with no campaign;
    /// pass null to clear it).
    /// </summary>
    public async Task<SignalHouseResponse> UpdateSubgroupAsync(
        string id,
        Dictionary<string, object> updateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/subgroup/{safeId}", HttpMethod.Put, updateData, options);
    }

    /// <summary>
    /// Delete a subgroup (mark as inactive).
    /// </summary>
    public async Task<SignalHouseResponse> DeleteSubgroupAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/subgroup/{safeId}", HttpMethod.Delete, options: options);
    }
}
