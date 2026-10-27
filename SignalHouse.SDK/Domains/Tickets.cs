using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only customer ticket operations (create/search Jira issues for groups).
/// </summary>
public sealed class TicketsAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal TicketsAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Load Jira form metadata (issue types, priorities, active sprint, field IDs). Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> GetJiraMetadataAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/admin/ticket/jira/metadata", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Search open epics for the parent picker. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> SearchJiraEpicsAsync(string? query = null, RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "query", query } });
        return await _client.RequestAsync($"/admin/ticket/jira/epics{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Search open non-Epic, non-Subtask issues for the Subtask parent picker. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> SearchJiraParentIssuesAsync(string? query = null, RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "query", query } });
        return await _client.RequestAsync($"/admin/ticket/jira/parent-issues{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Search assignable Jira users for the assignee picker. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> SearchJiraAssigneesAsync(string? query = null, RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "query", query } });
        return await _client.RequestAsync($"/admin/ticket/jira/assignees{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Suggest Jira labels matching a query. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> SearchJiraLabelsAsync(string? query = null, RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "query", query } });
        return await _client.RequestAsync($"/admin/ticket/jira/labels{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a Jira ticket from the admin customer ticket form. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> CreateJiraTicketAsync(object data, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "data", data } });
        return await _client.RequestAsync("/admin/ticket/jira", HttpMethod.Post, data, options);
    }
}

/// <summary>
/// Tickets domain. Exposes only admin-level operations (staff-only customer tickets).
/// </summary>
public sealed class TicketsDomain
{
    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public TicketsAdminDomain? Admin { get; }

    internal TicketsDomain(HttpClientWrapper client, bool enableAdmin)
    {
        Admin = enableAdmin ? new TicketsAdminDomain(client) : null;
    }
}
