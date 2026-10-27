using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only user operations (search by email, create internal users).
/// </summary>
public sealed class UsersAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal UsersAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get a list of users with optional filters (admin scope -- searches across all groups).
    /// </summary>
    /// <param name="email">Filter users by email (partial match).</param>
    /// <param name="userType">Filter users by type (user, service).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetUsersAsync(
        string? email = null,
        string? userType = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "email", email },
            { "userType", userType },
        });

        return await _client.RequestAsync($"/user{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new internal (SignalHouse staff) user.
    /// </summary>
    public async Task<SignalHouseResponse> CreateInternalUserAsync(
        Dictionary<string, object> data,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "data", data } });

        return await _client.RequestAsync("/user/signalhouse", HttpMethod.Post, data, options);
    }
}

/// <summary>
/// User account management operations.
/// </summary>
public sealed class UsersDomain
{
    private readonly HttpClientWrapper _client;

    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public UsersAdminDomain? Admin { get; }

    internal UsersDomain(HttpClientWrapper client, bool enableAdmin)
    {
        _client = client;
        Admin = enableAdmin ? new UsersAdminDomain(client) : null;
    }

    /// <summary>
    /// Get a list of users with optional filters.
    /// </summary>
    public async Task<SignalHouseResponse> GetUsersAsync(
        string? id = null,
        string? groupId = null,
        string? userType = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
            { "groupId", groupId },
            { "userType", userType },
        });

        return await _client.RequestAsync($"/user{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    public async Task<SignalHouseResponse> CreateUserAsync(
        Dictionary<string, object> data,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "data", data } });

        return await _client.RequestAsync("/user", HttpMethod.Post, data, options);
    }

    /// <summary>
    /// Create a new service user (API key / non-human user).
    /// Customer API keys (role "api") may set "allowedIps" (IPv4/IPv6 addresses or CIDR ranges, max 100)
    /// and "allowedCountries" (ISO 3166-1 alpha-2 codes such as "US", max 250); both must match when both are set.
    /// "allowedDestinationCountries" (ISO 3166-1 alpha-2, max 250) limits the countries the key can send messages to.
    /// </summary>
    public async Task<SignalHouseResponse> CreateServiceUserAsync(
        Dictionary<string, object> data,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "data", data } });

        return await _client.RequestAsync("/user/serviceuser", HttpMethod.Post, data, options);
    }

    /// <summary>
    /// Update an existing user's information.
    /// For a customer API key, "allowedIps" / "allowedCountries" / "allowedDestinationCountries" replace its restrictions (an empty list removes one);
    /// this needs an interactive admin or developer session, so an API key cannot change its own.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateUserAsync(
        string id,
        Dictionary<string, object> data,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "data", data } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/user/{safeId}", HttpMethod.Put, data, options);
    }

    /// <summary>
    /// Delete a user (mark as inactive).
    /// </summary>
    public async Task<SignalHouseResponse> DeleteUserAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/user/{safeId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Get notification preferences for a user.
    /// </summary>
    public async Task<SignalHouseResponse> GetNotificationPreferencesAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/user/{safeId}/notification-preferences", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Update notification preferences for a user.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateNotificationPreferencesAsync(
        string id,
        List<Dictionary<string, object>> notificationPreferences,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "notificationPreferences", notificationPreferences } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync(
            $"/user/{safeId}/notification-preferences",
            HttpMethod.Put,
            new { notificationPreferences },
            options);
    }
}
