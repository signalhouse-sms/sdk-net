using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only subscription template and custom subscription operations.
/// </summary>
public sealed class SubscriptionsAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal SubscriptionsAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get subscription templates with optional filtering by template ID.
    /// </summary>
    public async Task<SignalHouseResponse> GetTemplatesAsync(string? templateId = null, RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "templateId", templateId },
        });

        return await _client.RequestAsync($"/subscription/template{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new subscription template.
    /// </summary>
    public async Task<SignalHouseResponse> CreateTemplateAsync(
        Dictionary<string, object> templateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "templateData", templateData } });

        return await _client.RequestAsync("/subscription/template", HttpMethod.Post, templateData, options);
    }

    /// <summary>
    /// Update an existing subscription template.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateTemplateAsync(
        string templateId,
        Dictionary<string, object> templateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "templateId", templateId }, { "templateData", templateData } });

        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/subscription/template/{safeTemplateId}", HttpMethod.Put, templateData, options);
    }

    /// <summary>
    /// Delete an existing subscription template.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteTemplateAsync(string templateId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "templateId", templateId } });

        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/subscription/template/{safeTemplateId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Create a custom subscription for a group.
    /// </summary>
    public async Task<SignalHouseResponse> CreateCustomSubscriptionAsync(
        string groupId,
        Dictionary<string, object> subscriptionData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "subscriptionData", subscriptionData } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/subscription/user/custom/{safeGroupId}", HttpMethod.Post, subscriptionData, options);
    }

    /// <summary>
    /// Update volume tiers, ramp, and monthly minimum on an active custom subscription.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateCustomSubscriptionAsync(
        string groupId,
        Dictionary<string, object> updateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "updateData", updateData } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/subscription/user/custom/{safeGroupId}", HttpMethod.Put, updateData, options);
    }
}

/// <summary>
/// Subscription and template management operations.
/// </summary>
public sealed class SubscriptionsDomain
{
    private readonly HttpClientWrapper _client;

    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public SubscriptionsAdminDomain? Admin { get; }

    internal SubscriptionsDomain(HttpClientWrapper client, bool enableAdmin)
    {
        _client = client;
        Admin = enableAdmin ? new SubscriptionsAdminDomain(client) : null;
    }

    /// <summary>
    /// Get subscription history for a group with optional filters.
    /// </summary>
    public async Task<SignalHouseResponse> GetSubscriptionsAsync(
        string groupId,
        bool? onlyActive = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "onlyActive", onlyActive?.ToString().ToLowerInvariant() },
        });

        return await _client.RequestAsync($"/subscription/user{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Subscribe a group to a template.
    /// </summary>
    public async Task<SignalHouseResponse> SubscribeAsync(
        string groupId,
        string templateId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "templateId", templateId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync(
            $"/subscription/user/subscribe/{safeGroupId}/{safeTemplateId}",
            HttpMethod.Post,
            new { },
            options);
    }

    /// <summary>
    /// Unsubscribe (cancel) a group's active subscription.
    /// </summary>
    public async Task<SignalHouseResponse> UnsubscribeAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/subscription/user/cancel/{safeGroupId}", HttpMethod.Post, options: options);
    }
}
