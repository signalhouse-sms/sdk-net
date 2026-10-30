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

    /// <summary>
    /// Get a group's Enterprise SSO configuration: whether SSO is available, the redirect URL to register at
    /// the identity provider, the saved provider (never its client secret), and each claimed domain with the
    /// DNS TXT record that verifies it. Role: admin.
    /// </summary>
    /// <param name="groupId">The group.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { groupId, available, callbackUrl, connection, domains }.</returns>
    public async Task<SignalHouseResponse> GetSsoConfigurationAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
        });

        return await _client.RequestAsync($"/group/sso{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create or update a group's OpenID Connect identity provider. Requires a fresh
    /// Auth.ConfirmTwoFactorIdentityAsync from the same session. Role: admin.
    /// </summary>
    /// <param name="groupId">The group.</param>
    /// <param name="issuer">The provider's https issuer URL.</param>
    /// <param name="clientId">The client ID registered at the provider.</param>
    /// <param name="clientSecret">The client secret; required the first time, omit to keep the saved one.</param>
    /// <param name="enabled">Whether sign-in through the provider is on (default true).</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { groupId, available, callbackUrl, connection, domains }.</returns>
    public async Task<SignalHouseResponse> SaveSsoConnectionAsync(
        string groupId,
        string issuer,
        string clientId,
        string? clientSecret = null,
        bool? enabled = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "groupId", groupId },
            { "issuer", issuer },
            { "clientId", clientId },
        });

        var body = new Dictionary<string, object?>
        {
            { "groupId", groupId },
            { "issuer", issuer },
            { "clientId", clientId },
        };
        if (clientSecret is not null)
        {
            body["clientSecret"] = clientSecret;
        }
        if (enabled is not null)
        {
            body["enabled"] = enabled;
        }

        return await _client.RequestAsync("/group/sso", HttpMethod.Put, body, options);
    }

    /// <summary>
    /// Remove a group's identity provider and its domains, and unlink every account that signed in through it.
    /// Requires a fresh Auth.ConfirmTwoFactorIdentityAsync from the same session. Role: admin.
    /// </summary>
    /// <param name="groupId">The group.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the (now empty) { groupId, available, callbackUrl, connection, domains }.</returns>
    public async Task<SignalHouseResponse> DeleteSsoConnectionAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
        });

        return await _client.RequestAsync($"/group/sso{qs}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Claim an email domain for a group's SSO; the response includes the DNS TXT record that verifies it.
    /// Role: admin.
    /// </summary>
    /// <param name="groupId">The group.</param>
    /// <param name="domain">The domain, e.g. "acme.com".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { groupId, available, callbackUrl, connection, domains }.</returns>
    public async Task<SignalHouseResponse> AddSsoDomainAsync(string groupId, string domain, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "domain", domain } });

        var body = new Dictionary<string, object?>
        {
            { "groupId", groupId },
            { "domain", domain },
        };

        return await _client.RequestAsync("/group/sso/domains", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Verify a claimed domain by its DNS TXT record. Role: admin.
    /// </summary>
    /// <param name="groupId">The group.</param>
    /// <param name="domain">The claimed domain.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { groupId, available, callbackUrl, connection, domains }.</returns>
    public async Task<SignalHouseResponse> VerifySsoDomainAsync(string groupId, string domain, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "domain", domain } });

        var body = new Dictionary<string, object?>
        {
            { "groupId", groupId },
        };

        var safeDomain = Uri.EscapeDataString(domain);
        return await _client.RequestAsync($"/group/sso/domains/{safeDomain}/verify", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Remove a domain from a group's SSO. Role: admin.
    /// </summary>
    /// <param name="groupId">The group.</param>
    /// <param name="domain">The domain.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { groupId, available, callbackUrl, connection, domains }.</returns>
    public async Task<SignalHouseResponse> RemoveSsoDomainAsync(string groupId, string domain, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "domain", domain } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
        });

        var safeDomain = Uri.EscapeDataString(domain);
        return await _client.RequestAsync($"/group/sso/domains/{safeDomain}{qs}", HttpMethod.Delete, options: options);
    }
}
