using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Authentication operations: login, password reset, and auth history.
/// </summary>
public sealed class AuthDomain
{
    private readonly HttpClientWrapper _client;

    internal AuthDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the JWT token on success.</returns>
    public async Task<SignalHouseResponse> LoginAsync(string email, string password, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "email", email }, { "password", password } });

        return await _client.RequestAsync("/auth", HttpMethod.Post, new { email, password }, options);
    }

    /// <summary>
    /// Reset a user's password.
    /// </summary>
    /// <param name="userId">The ID of the user whose password should be reset.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> ResetPasswordAsync(string userId, string newPassword, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "userId", userId }, { "newPassword", newPassword } });

        var safeUserId = Uri.EscapeDataString(userId);
        return await _client.RequestAsync($"/auth/resetpassword/{safeUserId}", HttpMethod.Put, new { newPassword }, options);
    }

    /// <summary>
    /// Request a password-reset link be emailed to the given address (public). Always succeeds
    /// regardless of whether an account exists for the email, so it cannot be used to enumerate
    /// registered emails.
    /// </summary>
    /// <param name="email">The email address to send a reset link to.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { success: true }.</returns>
    public async Task<SignalHouseResponse> ForgotPasswordAsync(string email, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "email", email } });

        return await _client.RequestAsync("/auth/forgot-password", HttpMethod.Post, new { email }, options);
    }

    /// <summary>
    /// Reset a password using the single-use token from a reset email (public).
    /// </summary>
    /// <param name="token">The single-use reset token from the email link.</param>
    /// <param name="password">The new password to set (min 8 characters).</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { success: true }.</returns>
    public async Task<SignalHouseResponse> ResetPasswordWithTokenAsync(string token, string password, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "token", token }, { "password", password } });

        return await _client.RequestAsync("/auth/reset-password", HttpMethod.Post, new { token, password }, options);
    }

    /// <summary>
    /// Get token login history for a group or user.
    /// </summary>
    /// <param name="groupId">Returns history for all users in the group (one of groupId/userId required).</param>
    /// <param name="userId">Returns history for a specific user (one of groupId/userId required).</param>
    /// <param name="page">Page number (min 1, default 1).</param>
    /// <param name="limit">Results per page (min 1, max 100, default 20).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetAuthHistoryAsync(
        string? groupId = null,
        string? userId = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId or userId", (object?)groupId ?? userId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "userId", userId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/auth/history{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Log out all other users in the caller's active group.
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing loggedOutCount.</returns>
    public async Task<SignalHouseResponse> LogoutAllAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/logout-all", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Get the Group ID associated with the caller's JWT (their active group).
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { groupId }.</returns>
    public async Task<SignalHouseResponse> GetGroupIdAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/group-id", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Mint a single-use, short-lived external-link token for the authenticated caller. The token is
    /// handed to the GHL/Shopify backend so it can link the caller's existing V2 group to its tenant.
    /// </summary>
    /// <param name="product">The external system to link to ("ghl" or "shopify").</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { token }.</returns>
    public async Task<SignalHouseResponse> RequestExternalLinkTokenAsync(string product, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "product", product } });

        return await _client.RequestAsync("/auth/external-link-token", HttpMethod.Post, new { product }, options);
    }
}
