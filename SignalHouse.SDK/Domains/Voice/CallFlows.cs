using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Call Flows — customer-built call trees (greeting, menu, forward, voicemail,
/// hang up) that run when a number's route, or its Programmable Voice Profile,
/// names them with routeAction CALL_FLOW.
///
/// Each flow has one working draft, edited with <see cref="SaveDraftAsync"/>, and
/// published versions: <see cref="PublishAsync"/> validates the draft and makes it
/// the version calls run, while the draft stays editable without affecting calls.
///
/// Routes are mounted under /voice/api/v1/call-flows on the Voice service.
/// Accessed via <c>client.Voice.CallFlows</c>.
/// </summary>
public sealed class CallFlowsDomain
{
    private readonly HttpClientWrapper _client;

    internal CallFlowsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// List the account's call flows, newest first by default. Optional filters:
    /// page, limit, q, subgroupId, includeDeleted (bool), sortBy
    /// (createdAt | updatedAt | name), sortOrder (asc | desc). Returns
    /// <c>{ data: [...], meta: { total, page, limit, totalPages } }</c>.
    /// <c>GET /voice/api/v1/call-flows</c>.
    /// </summary>
    public async Task<SignalHouseResponse> ListAsync(IDictionary<string, object?>? filters = null, RequestOptions? options = null)
    {
        var parameters = new Dictionary<string, string?>();
        if (filters is not null)
        {
            foreach (var kvp in filters)
            {
                // bool.ToString() is "True"; the service only accepts "true" / "false".
                var str = kvp.Value is bool flag ? (flag ? "true" : "false") : kvp.Value?.ToString();
                if (!string.IsNullOrEmpty(str))
                {
                    parameters[kvp.Key] = str;
                }
            }
        }

        var qs = HttpClientWrapper.GetQueryString(parameters);
        return await _client.RequestAsync($"/voice/api/v1/call-flows{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// List the step types this service can run, with the outcomes each one offers.
    /// Returns <c>{ data: { nodeTypes: [{ type, ports, configurable }] } }</c>.
    /// <c>GET /voice/api/v1/call-flows/catalog</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CatalogAsync(RequestOptions? options = null)
        => await _client.RequestAsync("/voice/api/v1/call-flows/catalog", HttpMethod.Get, options: options);

    /// <summary>
    /// Get a call flow with its working draft and the issues that stop it being
    /// published. Returns <c>{ data: { ...flow, draft: { versionId, nodes, edges, issues } } }</c>.
    /// <c>GET /voice/api/v1/call-flows/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> GetAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-flows/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a call flow, with its working draft and the Inbound Call step every
    /// flow starts from. Required: name (unique within the subgroup) and
    /// subgroupId. The subgroup is fixed once created; only numbers and
    /// Programmable Voice Profiles in it can run the flow. Returns
    /// <c>{ data: { ...flow, draft } }</c>.
    /// <c>POST /voice/api/v1/call-flows</c>.
    /// </summary>
    public async Task<SignalHouseResponse> CreateAsync(IDictionary<string, object?> flowData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "flowData", flowData } });
        return await _client.RequestAsync("/voice/api/v1/call-flows", HttpMethod.Post, flowData, options);
    }

    /// <summary>
    /// Rename a call flow, or enable or disable it (partial update). Fields: name,
    /// status (ACTIVE | DISABLED). A disabled flow does not run; numbers routed to
    /// it fall through to their next routing layer. Returns
    /// <c>{ data: { ...flow, draft } }</c>.
    /// <c>PATCH /voice/api/v1/call-flows/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateAsync(string id, IDictionary<string, object?> updateData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-flows/{safeId}", new HttpMethod("PATCH"), updateData, options);
    }

    /// <summary>
    /// Delete a call flow. Reversible with <see cref="RestoreAsync"/>: while
    /// deleted, numbers and profiles routed to it fall through to their next
    /// routing layer. Returns <c>{ data: { ...flow } }</c>.
    /// <c>DELETE /voice/api/v1/call-flows/:id</c>.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-flows/{safeId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Restore a deleted call flow. Returns <c>{ data: { ...flow, draft } }</c>.
    /// <c>POST /voice/api/v1/call-flows/:id/restore</c>.
    /// </summary>
    public async Task<SignalHouseResponse> RestoreAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-flows/{safeId}/restore", HttpMethod.Post, EmptyBody(), options);
    }

    /// <summary>
    /// Copy a call flow and its working draft into a new flow in the same
    /// subgroup. The copy is not published. <paramref name="name"/> defaults to
    /// "&lt;name&gt; (copy)". Returns <c>{ data: { ...flow, draft } }</c>.
    /// <c>POST /voice/api/v1/call-flows/:id/duplicate</c>.
    /// </summary>
    public async Task<SignalHouseResponse> DuplicateAsync(string id, string? name = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        var body = EmptyBody();
        if (!string.IsNullOrEmpty(name))
        {
            body["name"] = name;
        }
        return await _client.RequestAsync($"/voice/api/v1/call-flows/{safeId}/duplicate", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Replace the working draft: <c>{ nodes: [...], edges: [...] }</c>. Each node is
    /// <c>{ id, type, name, config, positionX, positionY }</c> and each edge
    /// <c>{ id, fromNodeId, toNodeId, port }</c>; ids are UUIDs you generate and are
    /// kept across saves. The whole draft is saved even when it is not finished;
    /// the issues that would stop it being published come back in the response.
    /// Returns <c>{ data: { nodes, edges, issues } }</c>.
    /// <c>PUT /voice/api/v1/call-flows/:id/draft</c>.
    /// </summary>
    public async Task<SignalHouseResponse> SaveDraftAsync(string id, IDictionary<string, object?> draft, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "draft", draft } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-flows/{safeId}/draft", HttpMethod.Put, draft, options);
    }

    /// <summary>
    /// Publish the working draft, making it the version calls run. A draft that is
    /// not ready returns 400 with error.issues listing each step that needs
    /// attention. Returns <c>{ data: { id, versionNumber, status, publishedAt } }</c>.
    /// <c>POST /voice/api/v1/call-flows/:id/publish</c>.
    /// </summary>
    public async Task<SignalHouseResponse> PublishAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });
        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/voice/api/v1/call-flows/{safeId}/publish", HttpMethod.Post, EmptyBody(), options);
    }

    /// <summary>
    /// The empty JSON object the input-less POSTs send, as every SDK does: the
    /// Voice service rejects a request that declares JSON but has no body.
    /// </summary>
    private static Dictionary<string, object?> EmptyBody() => new();
}
