using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only onboarding operations. All onboarding data is staff-only.
/// </summary>
public sealed class OnboardingAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal OnboardingAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get all onboarding records (one per group). Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> GetOnboardingsAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/group/onboarding", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single onboarding record by group ID. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> GetOnboardingAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/group/onboarding/{safeGroupId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Set where a group's new toll-free brands and numbers go ("infobip" or "twilio"). Staff-only.
    /// Existing brands, numbers and campaigns stay where they are.
    /// </summary>
    public async Task<SignalHouseResponse> SetTollFreeProviderAsync(string groupId, string tollFreeProvider, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "tollFreeProvider", tollFreeProvider } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/group/onboarding/{safeGroupId}/toll-free-provider", HttpMethod.Put, new
        {
            tollFreeProvider,
        }, options);
    }

    /// <summary>
    /// Add an internal comment/note to a group's admin Pricing editor. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> AddPricingCommentAsync(string groupId, string comment, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "comment", comment } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/group/onboarding/{safeGroupId}/pricing-comments", HttpMethod.Post, new
        {
            comment,
        }, options);
    }

    /// <summary>
    /// Add an internal comment/note to a group's admin Credits page. Staff-only.
    /// </summary>
    public async Task<SignalHouseResponse> AddCreditCommentAsync(string groupId, string comment, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "comment", comment } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/group/onboarding/{safeGroupId}/credit-comments", HttpMethod.Post, new
        {
            comment,
        }, options);
    }
}

/// <summary>
/// Onboarding domain. Exposes only admin-level operations (staff-only data).
/// </summary>
public sealed class OnboardingDomain
{
    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public OnboardingAdminDomain? Admin { get; }

    internal OnboardingDomain(HttpClientWrapper client, bool enableAdmin)
    {
        Admin = enableAdmin ? new OnboardingAdminDomain(client) : null;
    }
}
