using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only group operations (list all, create, delete).
/// </summary>
public sealed class GroupsAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal GroupsAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get resource counts grouped by group ID. Only signalhouse_admin can query all groups;
    /// every other allowed role is restricted to its JWT group scope.
    /// </summary>
    public async Task<SignalHouseResponse> GetGroupCountsAsync(
        string? groupId = null,
        int? limit = null,
        int? offset = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "limit", limit?.ToString() },
            { "offset", offset?.ToString() },
        });
        return await _client.RequestAsync($"/group/counts{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a list of all groups with optional pagination.
    /// </summary>
    public async Task<SignalHouseResponse> GetGroupsAsync(
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/group{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new group. <paramref name="groupData"/> optionally accepts <c>psPartnerKey</c> and
    /// <c>ownerEmail</c> to credit a referring partner.
    /// </summary>
    public async Task<SignalHouseResponse> CreateGroupAsync(Dictionary<string, object> groupData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupData", groupData } });

        return await _client.RequestAsync("/group", HttpMethod.Post, groupData, options);
    }

    /// <summary>
    /// Delete a group.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteGroupAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/group/{safeGroupId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Set a group's KYC compliance status. Only flips the field — it does not trigger
    /// any lockout side effects (halted sends, revoked API keys, etc.) for "rejected".
    /// </summary>
    /// <param name="groupId">The ID of the group to update.</param>
    /// <param name="complianceStatus">One of "unverified", "pending", "verified", "rejected".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>The updated group object returned from the server.</returns>
    public async Task<SignalHouseResponse> UpdateComplianceStatusAsync(
        string groupId,
        string complianceStatus,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "complianceStatus", complianceStatus } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        var body = new Dictionary<string, object?> { { "complianceStatus", complianceStatus } };
        return await _client.RequestAsync($"/group/{safeGroupId}/compliance-status", HttpMethod.Put, body, options);
    }

    /// <summary>
    /// Link an external tenant (GHL/Shopify) to a V2 group (server-to-server). Exchanges a single-use
    /// link token for a canonical group, adopting an empty portal group, repointing to an existing
    /// group, or flagging for manual review.
    /// </summary>
    /// <param name="linkToken">The single-use external-link token minted by the portal user.</param>
    /// <param name="externalSystem">The external system ("ghl" or "shopify").</param>
    /// <param name="externalId">The external tenant identifier.</param>
    /// <param name="existingGroupId">An existing V2 group ID to repoint to, if any.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { status, canonicalGroupId, ... }.</returns>
    public async Task<SignalHouseResponse> LinkExternalAsync(
        string linkToken,
        string externalSystem,
        string externalId,
        string? existingGroupId = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "linkToken", linkToken },
            { "externalSystem", externalSystem },
            { "externalId", externalId },
        });

        var body = new Dictionary<string, object?>
        {
            { "linkToken", linkToken },
            { "externalSystem", externalSystem },
            { "externalId", externalId },
        };
        if (existingGroupId is not null)
        {
            body["existingGroupId"] = existingGroupId;
        }

        return await _client.RequestAsync("/group/link-external", HttpMethod.Post, body, options);
    }
}

/// <summary>
/// Group management operations.
/// </summary>
public sealed class GroupsDomain
{
    private readonly HttpClientWrapper _client;

    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public GroupsAdminDomain? Admin { get; }

    internal GroupsDomain(HttpClientWrapper client, bool enableAdmin)
    {
        _client = client;
        Admin = enableAdmin ? new GroupsAdminDomain(client) : null;
    }

    /// <summary>
    /// Get details of a group by its ID.
    /// </summary>
    public async Task<SignalHouseResponse> GetGroupAsync(string id, RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
        });

        return await _client.RequestAsync($"/group{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Update a group. groupData may include optional CNP fields: cspId (string?), defaultCnpSubgroupId (string?).
    /// </summary>
    public async Task<SignalHouseResponse> UpdateGroupAsync(
        string id,
        Dictionary<string, object> groupData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "groupData", groupData } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/group/{safeId}", HttpMethod.Put, groupData, options);
    }
}
