using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only registration review and recovery operations (review, corrections, number allocation, reconciliation,
/// application download). Enabling the SDK admin namespace grants no server permission.
/// </summary>
public sealed class RegistrationsAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal RegistrationsAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Approve or reject a registration awaiting Signal House review (admin only).
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="action">"APPROVE" (submits the registration to the provider) or "REJECT".</param>
    /// <param name="reason">Why the registration was rejected; required for "REJECT".</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The updated registration.</returns>
    public async Task<SignalHouseResponse> ReviewRegistrationAsync(
        string registrationId,
        string action,
        string? reason = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationId", registrationId }, { "action", action } });

        var safeRegistrationId = Uri.EscapeDataString(registrationId);
        var body = new Dictionary<string, object?> { { "action", action } };
        if (reason is not null)
        {
            body["reason"] = reason;
        }
        return await _client.RequestAsync($"/registration/{safeRegistrationId}/review", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Save corrections for a registration the provider sent back for more information (admin only).
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="reason">Why the correction is being made.</param>
    /// <param name="data">The type's correctable fields (VIRTUAL_LONG_CODE: useCases and, for SMS + Voice, voice). Owner, quantity, capabilities and the provider order are fixed.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The updated registration.</returns>
    public async Task<SignalHouseResponse> CorrectRegistrationAsync(
        string registrationId,
        string reason,
        Dictionary<string, object> data,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationId", registrationId }, { "reason", reason }, { "data", data } });

        var safeRegistrationId = Uri.EscapeDataString(registrationId);
        var body = new Dictionary<string, object?> { { "reason", reason }, { "data", data } };
        return await _client.RequestAsync($"/registration/{safeRegistrationId}/corrections", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Allocate a provider-owned number to a registration through the staff recovery path (admin only).
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="phoneNumber">The allocated number in E.164 digits (10-15 digits, optional leading +).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The updated registration.</returns>
    public async Task<SignalHouseResponse> AddNumberAsync(
        string registrationId,
        string phoneNumber,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationId", registrationId }, { "phoneNumber", phoneNumber } });

        var safeRegistrationId = Uri.EscapeDataString(registrationId);
        var body = new Dictionary<string, object?> { { "phoneNumber", phoneNumber } };
        return await _client.RequestAsync($"/registration/{safeRegistrationId}/numbers", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Link a RECONCILIATION_REQUIRED registration to the provider order that was actually placed (admin only).
    /// Never place a second order to retry an ambiguous submission.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="externalId">The provider's identifier for the existing order.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The updated registration.</returns>
    public async Task<SignalHouseResponse> ReconcileAsync(
        string registrationId,
        string externalId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationId", registrationId }, { "externalId", externalId } });

        var safeRegistrationId = Uri.EscapeDataString(registrationId);
        var body = new Dictionary<string, object?> { { "externalId", externalId } };
        return await _client.RequestAsync($"/registration/{safeRegistrationId}/reconcile", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Get the completed carrier application as { filename, contentType, base64 }; decode base64 to save the file (admin only).
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The document envelope.</returns>
    public async Task<SignalHouseResponse> GetApplicationAsync(
        string registrationId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationId", registrationId } });

        var safeRegistrationId = Uri.EscapeDataString(registrationId);
        return await _client.RequestAsync($"/registration/{safeRegistrationId}/application?format=json", HttpMethod.Get, options: options);
    }
}

/// <summary>
/// Sender registrations: the catalog, quotes, submission and status tracking for every jurisdiction.
/// </summary>
public sealed class RegistrationsDomain
{
    private readonly HttpClientWrapper _client;

    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public RegistrationsAdminDomain? Admin { get; }

    internal RegistrationsDomain(HttpClientWrapper client, bool enableAdmin)
    {
        _client = client;
        Admin = enableAdmin ? new RegistrationsAdminDomain(client) : null;
    }

    /// <summary>
    /// Get the registration types a jurisdiction offers and whether each can be acquired right now. The data is
    /// { region, jurisdiction, sendingRequiresRegistration, registeredThrough, provisioning, types: [{ type, kind, channel,
    /// bundles, quantity: { min, max }, requiresParent, available }] }.
    /// </summary>
    /// <param name="region">The jurisdiction (US, GB, CA, AU; any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The catalog.</returns>
    public async Task<SignalHouseResponse> GetCatalogAsync(
        string region,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "region", region } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "region", region } });
        return await _client.RequestAsync($"/registration/catalog{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get setup and monthly prices, in integer microdollars, for each capability bundle of a type. The data is one
    /// quote per bundle: capabilities, quantity, currency, setupAmount, monthlyAmount, totalSetupAmount,
    /// totalMonthlyAmount, etaDays. setupAmount/monthlyAmount are per sender; the totals cover the quantity.
    /// </summary>
    /// <param name="region">The jurisdiction (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB).</param>
    /// <param name="type">The registration type, e.g. "VIRTUAL_LONG_CODE", or "ALPHANUMERIC_SENDER_ID" for GB (one ["SMS"] bundle, 6000000 setup and 6000000 monthly by default).</param>
    /// <param name="quantity">How many senders the order covers (1-100); defaults to 1.</param>
    /// <param name="groupId">Staff only: quote on behalf of another group.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The quotes.</returns>
    public async Task<SignalHouseResponse> GetQuotesAsync(
        string region,
        string type,
        int quantity = 1,
        string? groupId = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "region", region }, { "type", type } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "region", region },
            { "type", type },
            { "quantity", quantity.ToString() },
            { "groupId", groupId },
        });

        return await _client.RequestAsync($"/registration/quotes{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a list of registrations with optional filters and pagination. The response carries data, totalCount, page and limit.
    /// </summary>
    /// <param name="groupId">Optional group ID filter; staff may omit it for the review queue.</param>
    /// <param name="subgroupId">Optional subgroup ID filter.</param>
    /// <param name="region">Optional jurisdiction filter: one jurisdiction or a comma-separated list (e.g. "GB,CA").</param>
    /// <param name="type">Optional registration type filter.</param>
    /// <param name="kind">Optional kind filter: identity, program or sender.</param>
    /// <param name="status">Optional filter by one registration status or a comma-separated list (e.g. "PENDING_PROVIDER,APPROVED"), from DRAFT, SIGNAL_HOUSE_REVIEW, SIGNAL_HOUSE_APPROVED, SIGNAL_HOUSE_REJECTED, SUBMITTING, PENDING_PROVIDER, NEEDS_INFORMATION, PAYMENT_REQUIRED, APPROVED, REJECTED, CANCELLED, PENDING_DELETE, DELETED, EXPIRED or RECONCILIATION_REQUIRED.</param>
    /// <param name="parentRegistrationId">Optional parent registration filter (e.g. a program's identity).</param>
    /// <param name="phoneNumber">Optional filter to registrations holding this phone number.</param>
    /// <param name="sortBy">Optional sort field: createdAt (default), subgroupId, registrationId, region or status.</param>
    /// <param name="sortOrder">Optional sort direction: asc or desc (default).</param>
    /// <param name="page">Page number.</param>
    /// <param name="limit">Items per page (max 100).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The registrations.</returns>
    public async Task<SignalHouseResponse> GetRegistrationsAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? region = null,
        string? type = null,
        string? kind = null,
        string? status = null,
        string? parentRegistrationId = null,
        string? phoneNumber = null,
        string? sortBy = null,
        string? sortOrder = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "region", region },
            { "type", type },
            { "kind", kind },
            { "status", status },
            { "parentRegistrationId", parentRegistrationId },
            { "phoneNumber", phoneNumber },
            { "sortBy", sortBy },
            { "sortOrder", sortOrder },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/registration{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single registration with its type data, review outcome and status history.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The registration.</returns>
    public async Task<SignalHouseResponse> GetRegistrationAsync(
        string registrationId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationId", registrationId } });

        var safeRegistrationId = Uri.EscapeDataString(registrationId);
        return await _client.RequestAsync($"/registration/{safeRegistrationId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Cancel a registration. Allowed from SIGNAL_HOUSE_REVIEW, SIGNAL_HOUSE_APPROVED, PENDING_PROVIDER, NEEDS_INFORMATION,
    /// PAYMENT_REQUIRED and APPROVED (409 otherwise). Numbers already allocated stay on the account and stop sending.
    /// </summary>
    /// <param name="registrationId">The registration ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The cancelled registration.</returns>
    public async Task<SignalHouseResponse> CancelRegistrationAsync(
        string registrationId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationId", registrationId } });

        var safeRegistrationId = Uri.EscapeDataString(registrationId);
        return await _client.RequestAsync($"/registration/{safeRegistrationId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Submit a registration for Signal House review (HTTP 201).
    /// </summary>
    /// <param name="registrationData">{ region, type, subgroupId, capabilities (one of the type's bundles), data }. The type selects the data shape; for VIRTUAL_LONG_CODE it is { quantity (1-100), useCases (1-10), voice? (required exactly when capabilities include VOICE) }. For GB ALPHANUMERIC_SENDER_ID (capabilities ["SMS"]) it is { requestedSenderId (3-11 letters/digits/space/dot/dash, at least one letter), trafficOrigin (LOCAL | INTERNATIONAL), trafficType (TRANSACTIONAL | PROMOTIONAL), companyName, companyCountry, companyWebsite, industry, messageExample, senderRelationship? (required when requestedSenderId differs from companyName) }; quantity is always 1 and data.phoneNumbers carries the granted sender (e.g. ["ACME"]) once APPROVED. Set RequestOptions.IdempotencyKey to make a retry safe.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The new registration.</returns>
    public async Task<SignalHouseResponse> CreateRegistrationAsync(
        Dictionary<string, object> registrationData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "registrationData", registrationData } });

        return await _client.RequestAsync("/registration", HttpMethod.Post, registrationData, options);
    }
}
