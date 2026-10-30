using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only authentication operations (SignalHouse staff).
/// </summary>
public sealed class AuthAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal AuthAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Turn off two-factor authentication for a user who has lost every factor (lockout recovery).
    /// Clears their methods, backup codes and trusted devices. Audited.
    /// </summary>
    /// <param name="userId">The id of the user to reset.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { reset: true }.</returns>
    public async Task<SignalHouseResponse> ResetUserTwoFactorAsync(string userId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "userId", userId } });

        return await _client.RequestAsync($"/auth/2fa/users/{Uri.EscapeDataString(userId)}", HttpMethod.Delete, options: options);
    }
}

/// <summary>
/// Authentication operations: login, password reset, and auth history.
/// </summary>
public sealed class AuthDomain
{
    private readonly HttpClientWrapper _client;

    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public AuthAdminDomain? Admin { get; }

    internal AuthDomain(HttpClientWrapper client, bool enableAdmin)
    {
        _client = client;
        Admin = enableAdmin ? new AuthAdminDomain(client) : null;
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <param name="deviceToken">A trusted-device token from an earlier VerifyTwoFactorChallengeAsync; skips 2FA while valid.</param>
    /// <returns>A response containing the JWT token, or { mfaRequired, challengeId, ... } when the account has 2FA on, or { token, twoFactorSetupRequired } (a setup-only session) when the user's group requires 2FA and they haven't turned it on.</returns>
    public async Task<SignalHouseResponse> LoginAsync(string email, string password, RequestOptions? options = null, string? deviceToken = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "email", email }, { "password", password } });

        return await _client.RequestAsync("/auth", HttpMethod.Post, new { email, password, deviceToken }, options);
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

    /// <summary>
    /// List the OAuth sign-in providers enabled on this API (only providers with credentials configured).
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing [{ id, label }].</returns>
    public async Task<SignalHouseResponse> GetOAuthProvidersAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/oauth/providers", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Build the URL that starts an OAuth sign-in. Navigate a browser to it (it is a redirect, not an
    /// API call); the API returns the user to the portal's /auth/callback with a single-use ticket.
    /// With a <c>linkTicket</c> (from CreateOAuthLinkAsync) it links the provider to the signed-in account
    /// instead, and the API returns the user to /Settings/Security with <c>pendingLink</c> (pass it to
    /// ConfirmOAuthLinkAsync to finish linking) or <c>linkError</c>. With a <c>reauthTicket</c> (from
    /// CreateOAuthReauthAsync) the signed-in user signs in again with a linked provider to confirm it's them,
    /// and the API returns them to /Settings/Security with a single-use <c>pendingReauth</c> (the session that started it redeems it with ConfirmOAuthReauthAsync) or <c>reauthError</c>.
    /// </summary>
    /// <param name="provider">The provider id ("google", "microsoft", "apple", or "sso" for Enterprise SSO,
    /// which requires <paramref name="email"/>).</param>
    /// <param name="next">A same-site path (e.g. "/oauth/consent?request=abc") to return the user to
    /// after sign-in. The API hands it back on the /auth/callback redirect as <c>next</c>; anything
    /// that isn't a rooted path on the portal is dropped.</param>
    /// <param name="email">Required for "sso" sign-in: the user's work email, whose domain picks the organization's
    /// identity provider. Not needed with a <paramref name="reauthTicket"/>.</param>
    /// <param name="linkTicket">Link the provider to the signed-in account that requested this ticket
    /// (from CreateOAuthLinkAsync) instead of signing in. The API then returns the browser to
    /// /Settings/Security?pendingLink=&lt;ticket&gt;, which must be passed to ConfirmOAuthLinkAsync.</param>
    /// <param name="reauthTicket">Sign in again with a linked provider to confirm it's the account that requested
    /// this ticket (from CreateOAuthReauthAsync).</param>
    /// <returns>The absolute authorization URL.</returns>
    public string GetOAuthAuthorizationUrl(string provider, string? next = null, string? email = null, string? linkTicket = null, string? reauthTicket = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "provider", provider } });

        var url = $"{_client.BaseUrl}/auth/oauth/{Uri.EscapeDataString(provider)}";
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "email", string.IsNullOrEmpty(email) ? null : email },
            { "linkTicket", string.IsNullOrEmpty(linkTicket) ? null : linkTicket },
            { "reauthTicket", string.IsNullOrEmpty(reauthTicket) ? null : reauthTicket },
            { "next", string.IsNullOrEmpty(next) ? null : next },
        });

        return $"{url}{qs}";
    }

    /// <summary>
    /// List the caller's sign-in methods: whether they chose their own password (<c>passwordKnown</c> is false
    /// for an account created through a provider or Enterprise SSO), and every provider linked to the account.
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { passwordKnown, links: [{ provider, email, linkedAt, lastLoginAt }] }.</returns>
    public async Task<SignalHouseResponse> GetOAuthLinksAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/oauth/links", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Start linking Google, Microsoft or Apple to the caller's account. Returns a single-use link ticket
    /// (valid 2 minutes): navigate the browser to GetOAuthAuthorizationUrl(provider, linkTicket: ticket).
    /// Requires a fresh ConfirmTwoFactorIdentityAsync from the same session (403 otherwise), which this spends.
    /// The round trip returns the browser to /Settings/Security?pendingLink=&lt;ticket&gt;; pass that value to
    /// ConfirmOAuthLinkAsync to finish linking.
    /// </summary>
    /// <param name="provider">"google", "microsoft" or "apple".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { linkTicket, expiresInSeconds }.</returns>
    public async Task<SignalHouseResponse> CreateOAuthLinkAsync(string provider, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "provider", provider } });

        return await _client.RequestAsync($"/auth/oauth/{Uri.EscapeDataString(provider)}/link", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Start signing in again with a provider already linked to the caller's account, to confirm it's them.
    /// Spends nothing. Returns a single-use ticket (valid 2 minutes): navigate the browser to
    /// GetOAuthAuthorizationUrl(provider, reauthTicket: ticket). The browser comes back to /Settings/Security
    /// with a single-use <c>pendingReauth</c> (the session that started it redeems it with ConfirmOAuthReauthAsync) or <c>reauthError</c> (reauth_wrong_account, reauth_unsupported, reauth_stale,
    /// invalid_state, sso_not_configured, access_denied or provider_error).
    /// </summary>
    /// <param name="provider">"google", "microsoft", "apple" or "sso"; must be linked to the account.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { reauthTicket, reauthSecret, expiresInSeconds }. Keep reauthSecret in the client
    /// that started the flow: it is returned only here, and ConfirmOAuthReauthAsync needs it.</returns>
    public async Task<SignalHouseResponse> CreateOAuthReauthAsync(string provider, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "provider", provider } });

        return await _client.RequestAsync($"/auth/oauth/{Uri.EscapeDataString(provider)}/reauth", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Finish signing in again with a linked provider. The round trip started with CreateOAuthReauthAsync returns the
    /// browser to /Settings/Security with a single-use <c>pendingReauth</c> (valid 5 minutes); this redeems it, but only
    /// for the same account and the same session that called CreateOAuthReauthAsync, so a re-sign-in completed in
    /// someone else's browser confirms nothing. Fails with 400 when it expired or was used, and 403 for another
    /// account or session.
    /// </summary>
    /// <param name="pendingReauth">The <c>pendingReauth</c> query parameter from the redirect.</param>
    /// <param name="reauthSecret">The <c>reauthSecret</c> CreateOAuthReauthAsync returned; a wrong or missing secret is
    /// refused (403) and uses up the pending re-sign-in.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { identityConfirmed } (true when 2FA is off, so it counts as a confirmed identity).</returns>
    public async Task<SignalHouseResponse> ConfirmOAuthReauthAsync(string pendingReauth, string reauthSecret, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "pendingReauth", pendingReauth }, { "reauthSecret", reauthSecret } });

        return await _client.RequestAsync("/auth/oauth/reauth/confirm", HttpMethod.Post, new { pendingReauth, reauthSecret }, options);
    }

    /// <summary>
    /// Finish linking a provider. The provider round trip started with CreateOAuthLinkAsync returns the browser to
    /// /Settings/Security with a single-use <c>pendingLink</c>; this links it, but only for the account the link was
    /// started for, so a link URL opened in someone else's browser can't attach their provider to your account.
    /// </summary>
    /// <param name="pendingLink">The <c>pendingLink</c> query parameter from the redirect.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the caller's { passwordKnown, links: [{ provider, email, linkedAt, lastLoginAt }] }.</returns>
    public async Task<SignalHouseResponse> ConfirmOAuthLinkAsync(string pendingLink, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "pendingLink", pendingLink } });

        return await _client.RequestAsync("/auth/oauth/links/confirm", HttpMethod.Post, new { pendingLink }, options);
    }

    /// <summary>
    /// Unlink a provider from the caller's account. Refused (400) when it is the account's only way to sign in.
    /// Requires a fresh ConfirmTwoFactorIdentityAsync from the same session (403 otherwise).
    /// </summary>
    /// <param name="provider">"google", "microsoft", "apple" or "sso".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the remaining { passwordKnown, links }.</returns>
    public async Task<SignalHouseResponse> DeleteOAuthLinkAsync(string provider, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "provider", provider } });

        return await _client.RequestAsync($"/auth/oauth/{Uri.EscapeDataString(provider)}/link", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Read what a single-use OAuth ticket resolves to without consuming it: a "session" (ready to
    /// exchange), a "select" (pick one of several accounts) or a "signup" (finish registration).
    /// </summary>
    /// <param name="ticket">The ticket from the OAuth callback redirect.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { kind, provider, profile?, accounts? }.</returns>
    public async Task<SignalHouseResponse> ReadOAuthTicketAsync(string ticket, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "ticket", ticket } });

        return await _client.RequestAsync("/auth/oauth/ticket", HttpMethod.Post, new { ticket }, options);
    }

    /// <summary>
    /// Exchange a single-use "session" or "select" OAuth ticket for a JWT. Consumes the ticket.
    /// </summary>
    /// <param name="ticket">The ticket from the OAuth callback redirect.</param>
    /// <param name="userId">Required for a "select" ticket: the chosen account from "accounts".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { token }.</returns>
    /// <param name="deviceToken">A trusted-device token from an earlier VerifyTwoFactorChallengeAsync; skips 2FA while valid.</param>
    public async Task<SignalHouseResponse> ExchangeOAuthTicketAsync(string ticket, string? userId = null, RequestOptions? options = null, string? deviceToken = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "ticket", ticket } });

        return await _client.RequestAsync("/auth/oauth/exchange", HttpMethod.Post, new { ticket, userId, deviceToken }, options);
    }

    /// <summary>
    /// Get the caller's two-factor settings: methods (targets masked), default method, backup-code counts
    /// and trusted devices. Never returns secrets or codes.
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the settings object.</returns>
    public async Task<SignalHouseResponse> GetTwoFactorSettingsAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/2fa", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Confirm it's really the account owner before a sensitive change: StartTwoFactorEnrollmentAsync,
    /// RemoveTwoFactorMethodAsync, RegenerateBackupCodesAsync, DisableTwoFactorAsync, CreateOAuthLinkAsync,
    /// DeleteOAuthLinkAsync, or Groups.SaveSsoConnectionAsync / Groups.DeleteSsoConnectionAsync. With 2FA on, pass
    /// a current code (or a backup code); with 2FA off, pass the account password, or, for an account without a
    /// password of its own (GetOAuthLinksAsync returns passwordKnown: false), the code from
    /// SendTwoFactorIdentityCodeAsync("email") with method "email". A confirmation lasts 5 minutes, belongs
    /// to the session that made it, and unlocks exactly one change.
    /// </summary>
    /// <param name="password">The account password. Required while 2FA is off.</param>
    /// <param name="code">A code from an enrolled method, or a backup code. Required while 2FA is on.</param>
    /// <param name="method">The method the code came from; defaults to the primary method.</param>
    /// <param name="usingBackupCode">True when code is a backup code.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { confirmed: true, expiresInSeconds }.</returns>
    public async Task<SignalHouseResponse> ConfirmTwoFactorIdentityAsync(string? password = null, string? code = null, string? method = null, bool? usingBackupCode = null, RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/2fa/confirm-identity", HttpMethod.Post, new { password, code, method, usingBackupCode }, options);
    }

    /// <summary>
    /// Send a code by text or email to use with ConfirmTwoFactorIdentityAsync (not needed for "app").
    /// </summary>
    /// <param name="method">An enrolled "sms" or "email" method, or "email" for an account without a password of its own.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { sent, target, expiresInSeconds? }.</returns>
    public async Task<SignalHouseResponse> SendTwoFactorIdentityCodeAsync(string method, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "method", method } });

        return await _client.RequestAsync("/auth/2fa/confirm-identity/send", HttpMethod.Post, new { method }, options);
    }

    /// <summary>
    /// Start enrolling a two-factor method. For "app" the response carries the setup secret and otpauth URI;
    /// for "sms" and "email" a code is sent.
    /// Requires a fresh ConfirmTwoFactorIdentityAsync from the same session (403 otherwise); each confirmation unlocks one change.
    /// </summary>
    /// <param name="method">"app", "sms" or "email".</param>
    /// <param name="phone">Required for "sms".</param>
    /// <param name="address">Required for "email".</param>
    /// <param name="deviceName">Optional label for an authenticator app.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { enrollmentId, secret?, otpauthUri?, sentTo? }.</returns>
    public async Task<SignalHouseResponse> StartTwoFactorEnrollmentAsync(string method, string? phone = null, string? address = null, string? deviceName = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "method", method } });

        return await _client.RequestAsync("/auth/2fa/enroll", HttpMethod.Post, new { method, phone, address, deviceName }, options);
    }

    /// <summary>
    /// Confirm a pending enrollment with its code. The first method turns 2FA on and returns backup codes,
    /// shown only this once.
    /// </summary>
    /// <param name="enrollmentId">From StartTwoFactorEnrollmentAsync.</param>
    /// <param name="code">The 6-digit code.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { settings, backupCodes }.</returns>
    public async Task<SignalHouseResponse> ConfirmTwoFactorEnrollmentAsync(string enrollmentId, string code, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "enrollmentId", enrollmentId }, { "code", code } });

        return await _client.RequestAsync($"/auth/2fa/enroll/{Uri.EscapeDataString(enrollmentId)}/verify", HttpMethod.Post, new { code }, options);
    }

    /// <summary>
    /// Choose which enrolled method is offered first at sign-in.
    /// </summary>
    /// <param name="method">"app", "sms" or "email".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the settings object.</returns>
    public async Task<SignalHouseResponse> SetTwoFactorPrimaryMethodAsync(string method, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "method", method } });

        return await _client.RequestAsync("/auth/2fa/primary", HttpMethod.Put, new { method }, options);
    }

    /// <summary>
    /// Remove an enrolled two-factor method. Removing the last one turns 2FA off.
    /// Requires a fresh ConfirmTwoFactorIdentityAsync from the same session (403 otherwise); each confirmation unlocks one change.
    /// </summary>
    /// <param name="method">"app", "sms" or "email".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the settings object.</returns>
    public async Task<SignalHouseResponse> RemoveTwoFactorMethodAsync(string method, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "method", method } });

        return await _client.RequestAsync($"/auth/2fa/methods/{Uri.EscapeDataString(method)}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Replace all backup codes with a new set, shown only once. The previous codes stop working.
    /// Requires a fresh ConfirmTwoFactorIdentityAsync from the same session (403 otherwise); each confirmation unlocks one change.
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { codes, settings }.</returns>
    public async Task<SignalHouseResponse> RegenerateBackupCodesAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/2fa/backup-codes", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Stop trusting a device, so its next sign-in asks for the second factor again.
    /// </summary>
    /// <param name="deviceId">The trusted device's id from GetTwoFactorSettingsAsync.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the settings object.</returns>
    public async Task<SignalHouseResponse> RevokeTrustedDeviceAsync(string deviceId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "deviceId", deviceId } });

        return await _client.RequestAsync($"/auth/2fa/trusted-devices/{Uri.EscapeDataString(deviceId)}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Turn two-factor authentication off: removes every method, backup code and trusted device.
    /// Requires a fresh ConfirmTwoFactorIdentityAsync from the same session (403 otherwise); each confirmation unlocks one change.
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing the settings object.</returns>
    public async Task<SignalHouseResponse> DisableTwoFactorAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/2fa", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Whether the "secure your account" enrollment prompt is due for the caller.
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { due }.</returns>
    public async Task<SignalHouseResponse> GetTwoFactorPromptAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/2fa/prompt", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Dismiss the enrollment prompt until the next sign-in. It stops for good after three dismissals.
    /// </summary>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { due: false }.</returns>
    public async Task<SignalHouseResponse> DismissTwoFactorPromptAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/auth/2fa/prompt/dismiss", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Send a sign-in code by text or email for a pending two-factor challenge (not needed for "app").
    /// </summary>
    /// <param name="challengeId">From the mfaRequired login or exchange response.</param>
    /// <param name="method">"sms" or "email".</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { sent, target, expiresInSeconds }.</returns>
    public async Task<SignalHouseResponse> SendTwoFactorChallengeCodeAsync(string challengeId, string method, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "challengeId", challengeId }, { "method", method } });

        return await _client.RequestAsync($"/auth/2fa/challenge/{Uri.EscapeDataString(challengeId)}/send", HttpMethod.Post, new { method }, options);
    }

    /// <summary>
    /// Verify a sign-in code (or backup code) for a pending challenge and receive the session JWT.
    /// With rememberDevice the response also carries a deviceToken.
    /// </summary>
    /// <param name="challengeId">From the mfaRequired login or exchange response.</param>
    /// <param name="code">The 6-digit code, or a backup code (xxxx-xxxx).</param>
    /// <param name="method">The method the code came from; defaults to the primary method.</param>
    /// <param name="usingBackupCode">True when code is a backup code.</param>
    /// <param name="rememberDevice">Trust this device for 30 days.</param>
    /// <param name="deviceName">Label for the trusted device.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { token, deviceToken? }.</returns>
    public async Task<SignalHouseResponse> VerifyTwoFactorChallengeAsync(string challengeId, string code, string? method = null, bool? usingBackupCode = null, bool? rememberDevice = null, string? deviceName = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "challengeId", challengeId }, { "code", code } });

        return await _client.RequestAsync(
            $"/auth/2fa/challenge/{Uri.EscapeDataString(challengeId)}/verify",
            HttpMethod.Post,
            new { code, method, usingBackupCode, rememberDevice, deviceName },
            options);
    }

    /// <summary>
    /// Cancel a pending two-factor challenge.
    /// </summary>
    /// <param name="challengeId">From the mfaRequired login or exchange response.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>A response containing { cancelled: true }.</returns>
    public async Task<SignalHouseResponse> CancelTwoFactorChallengeAsync(string challengeId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "challengeId", challengeId } });

        return await _client.RequestAsync($"/auth/2fa/challenge/{Uri.EscapeDataString(challengeId)}", HttpMethod.Delete, options: options);
    }
}
