using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only number porting operations (change status, approve port requests).
/// </summary>
public sealed class NumbersAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal NumbersAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Change the status of a port-in request (admin only). Infobip exposes no porting-status
    /// API or webhook, so ON_HOLD and its reason are always staff-entered here.
    /// </summary>
    /// <param name="portingId">The port-in request ID.</param>
    /// <param name="status">The new status value (PENDING, IN_PROGRESS, ON_HOLD, REJECTED).</param>
    /// <param name="description">Optional description/reason for the status change.</param>
    /// <param name="options">Additional request options.</param>
    /// <param name="onHoldReason">Required when status is ON_HOLD (DOCUMENTATION_MISMATCH, ACCOUNT_INFO_INCORRECT, CARRIER_REJECTED, PENDING_CUSTOMER_ACTION, OTHER). Appended last, after options, so existing positional callers are unaffected.</param>
    /// <returns>The updated port-in request.</returns>
    public async Task<SignalHouseResponse> ChangePortingStatusAsync(
        string portingId,
        string status,
        string? description = null,
        RequestOptions? options = null,
        string? onHoldReason = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "portingId", portingId }, { "status", status } });

        var safePortingId = Uri.EscapeDataString(portingId);
        var body = new Dictionary<string, object?> { { "status", status }, { "description", description }, { "onHoldReason", onHoldReason } };
        return await _client.RequestAsync($"/number/portin/{safePortingId}/status", HttpMethod.Put, body, options);
    }

    /// <summary>
    /// Approve a port-in request (admin only).
    /// </summary>
    /// <param name="portingId">The port-in request ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <param name="subGroupId">Subgroup to assign when the request has none yet. Ignored if the request
    /// already has a subgroup assigned -- an existing assignment is never overwritten. Appended last,
    /// after options, so existing positional callers are unaffected.</param>
    /// <returns>The approved port-in request.</returns>
    public async Task<SignalHouseResponse> ApprovePortRequestAsync(
        string portingId,
        RequestOptions? options = null,
        string? subGroupId = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "portingId", portingId } });

        var safePortingId = Uri.EscapeDataString(portingId);
        var body = new Dictionary<string, object?> { { "subGroupId", subGroupId } };
        return await _client.RequestAsync($"/number/portin/{safePortingId}/approve", HttpMethod.Post, body, options);
    }

    /// <summary>List campaign-bound Short Code acquisition requests (admin only).</summary>
    public async Task<SignalHouseResponse> GetShortCodeAcquisitionRequestsAsync(string? page = null, string? limit = null, string? status = null, string? search = null, RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "page", page }, { "limit", limit }, { "status", status }, { "search", search } });
        return await _client.RequestAsync($"/number/short-code/requests{qs}", HttpMethod.Get, options: options);
    }
}

/// <summary>
/// Phone number purchase, assignment, lookup, and management operations.
/// </summary>
public sealed class NumbersDomain
{
    private readonly HttpClientWrapper _client;

    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public NumbersAdminDomain? Admin { get; }

    internal NumbersDomain(HttpClientWrapper client, bool enableAdmin)
    {
        _client = client;
        Admin = enableAdmin ? new NumbersAdminDomain(client) : null;
    }

    /// <summary>
    /// Get a list of phone numbers with optional filters.
    /// </summary>
    /// <remarks>
    /// Every record carries a `sentiment` object -- trailing 7-day and 30-day inbound reply sentiment,
    /// refreshed periodically from analytics rather than computed per request: `{ sevenDay, thirtyDay,
    /// updatedAt }`, each window being `{ score, label, scoredCount, positive, neutral, negative }`. The
    /// object is always present and fully populated, so it can be read without a presence check.
    /// `score` is a volume-weighted average in -100..100 and is null when nothing was scored in the
    /// window, which is not the same as a score of 0 -- null means nobody replied, 0 means replies
    /// averaged neutral. `updatedAt` is null until the first rollup writes the record, and it tracks when
    /// the figures last CHANGED, not when the job last ran.
    ///
    /// Every record also carries a `health` object -- trailing 7-day and 30-day delivery/opt-out health from
    /// the same periodic refresh: `{ sevenDay, thirtyDay, updatedAt }`, each window being `{ score,
    /// deliveryRate, optOutRate, deliveryScore, optOutScore, messagesSent, messagesDelivered, messagesFailed,
    /// optOuts }`. `score` is 1.0..10.0 (the mean of the two bucket scores) and is null when nothing was sent
    /// in the window; the volumes say how much traffic sits behind it. A campaign's figure is scored from the
    /// campaign's own summed counters, never averaged from its numbers. For a live figure use GET /number/health
    /// or GET /campaign/health.
    /// </remarks>
    /// <param name="registrationId">Filter to numbers provisioned under this registration.</param>
    /// <param name="currentTollFreeCarrier">With groupId or subgroupId: when true, return only toll-free numbers that a new toll-free campaign in that group can use (those on the group's current toll-free carrier).</param>
    public async Task<SignalHouseResponse> GetPhoneNumbersAsync(
        string? phoneNumber = null,
        string? campaignId = null,
        string? brandId = null,
        string? subgroupId = null,
        string? groupId = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null,
        string? registrationId = null,
        bool? currentTollFreeCarrier = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "phoneNumber", phoneNumber },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "brandId", brandId },
            { "subgroupId", subgroupId },
            { "groupId", groupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            // bool.ToString() is "True"; the API expects lowercase.
            { "currentTollFreeCarrier", currentTollFreeCarrier?.ToString().ToLowerInvariant() },
        });

        return await _client.RequestAsync($"/number{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get the health score, the same one the portal shows, (1–10) for an owned phone number over 7-day and 30-day windows.
    /// </summary>
    public async Task<SignalHouseResponse> GetNumberHealthAsync(
        string phoneNumber,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "phoneNumber", phoneNumber },
        });

        return await _client.RequestAsync($"/number/health{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a list of available phone numbers for purchase with optional filters.
    /// </summary>
    public Task<SignalHouseResponse> GetAvailablePhoneNumbersAsync(
        bool? smsEnabled = null,
        bool? mmsEnabled = null,
        bool? voiceEnabled = null,
        string? country = null,
        string? state = null,
        string? npa = null,
        string? nxx = null,
        string? phoneNumber = null,
        int? limit = null,
        int? page = null,
        RequestOptions? options = null)
        => GetAvailablePhoneNumbersAsync(smsEnabled, mmsEnabled, voiceEnabled, country, state, npa, nxx, phoneNumber, limit, page, options, null);

    /// <summary>Search by city prefix within a US state.</summary>
    public async Task<SignalHouseResponse> GetAvailablePhoneNumbersAsync(
        bool? smsEnabled, bool? mmsEnabled, bool? voiceEnabled, string? country, string? state,
        string? npa, string? nxx, string? phoneNumber, int? limit, int? page, RequestOptions? options, string? city)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "smsEnabled", smsEnabled?.ToString().ToLowerInvariant() },
            { "mmsEnabled", mmsEnabled?.ToString().ToLowerInvariant() },
            { "voiceEnabled", voiceEnabled?.ToString().ToLowerInvariant() },
            { "country", country },
            { "state", state },
            { "city", city },
            { "npa", npa },
            { "nxx", nxx },
            { "phoneNumber", phoneNumber },
            { "limit", limit?.ToString() },
            { "page", page?.ToString() },
        });

        return await _client.RequestAsync($"/number/available{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Purchase phone numbers and assign them to a subgroup.
    /// </summary>
    /// <remarks>
    /// Each requested number is queued and processed independently. Per-number success or
    /// failure is delivered via <c>NUMBER_PURCHASE_SUCCESSFUL</c> / <c>NUMBER_PURCHASE_FAILED</c>
    /// webhooks — not in this response.
    /// </remarks>
    public Task<SignalHouseResponse> PurchasePhoneNumberAsync(List<string> phoneNumbers, string subgroupId, RequestOptions? options = null)
    {
        return PurchasePhoneNumberAsync(phoneNumbers, subgroupId, options, "US");
    }

    /// <summary>Purchase with an explicit ISO-2 country (US) while retaining the original US overload.</summary>
    public async Task<SignalHouseResponse> PurchasePhoneNumberAsync(
        List<string> phoneNumbers,
        string subgroupId,
        RequestOptions? options,
        string country)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers }, { "subgroupId", subgroupId } });

        return await _client.RequestAsync("/number", HttpMethod.Post, new { phoneNumbers, subgroupId, country }, options);
    }

    /// <summary>
    /// Purchase one or more Toll-Free numbers via the asynchronous resource-request flow.
    /// </summary>
    /// <remarks>
    /// Toll-Free numbers are ordered by quantity (not picked individually) and provisioned
    /// asynchronously; per-number completion is delivered via webhook/polling, not in this
    /// response. Poll the returned orderId with <see cref="GetTollFreeOrderStatusAsync"/>.
    /// </remarks>
    /// <param name="quantity">The number of Toll-Free numbers to purchase (1-10).</param>
    /// <param name="subgroupId">The subgroup the purchased numbers are assigned to.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>API response resolving to { message, orderId } once the request is queued.</returns>
    public Task<SignalHouseResponse> PurchaseTollFreeNumbersAsync(int quantity, string subgroupId, RequestOptions? options = null)
    {
        return PurchaseTollFreeNumbersAsync(quantity, subgroupId, options, "US");
    }

    /// <summary>Purchase with an explicit ISO-2 country (US) while retaining the original US overload.</summary>
    public async Task<SignalHouseResponse> PurchaseTollFreeNumbersAsync(
        int quantity,
        string subgroupId,
        RequestOptions? options,
        string country)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "subgroupId", subgroupId } });

        return await _client.RequestAsync("/number/toll-free", HttpMethod.Post, new { quantity, subgroupId, country }, options);
    }

    /// <summary>
    /// Request a campaign-bound Short Code acquisition or register a customer-owned Registry lease.
    /// </summary>
    /// <remarks>
    /// Random and vanity requests are fulfilled by Signal House staff. An external lease creates a pending Short Code
    /// number immediately; it becomes READY only when its campaign becomes ACTIVE.
    /// </remarks>
    /// <param name="subgroupId">The owning subgroup ID.</param>
    /// <param name="brandId">The owning Short Code brand ID.</param>
    /// <param name="campaignId">The owning Short Code campaign ID.</param>
    /// <param name="requestType">RANDOM, VANITY, or EXTERNAL_LEASE.</param>
    /// <param name="leaseTermMonths">Required 3, 6, or 12-month term for RANDOM/VANITY.</param>
    /// <param name="requestedVanityCode">Required five- or six-digit code for VANITY.</param>
    /// <param name="actualCode">Required customer-owned code for EXTERNAL_LEASE.</param>
    /// <param name="orderId">Required Registry order ID for EXTERNAL_LEASE.</param>
    /// <param name="leaseEndDate">Required future Registry lease end date for EXTERNAL_LEASE. A date-only value expires at 23:59:59 UTC.</param>
    /// <param name="options">Additional request options.</param>
    /// <param name="leaseReceiptFile">Required PNG, JPEG, or PDF receipt stream for EXTERNAL_LEASE.</param>
    /// <param name="leaseReceiptFileName">Filename for the external lease receipt.</param>
    /// <returns>A pending acquisition request or pending external-lease number.</returns>
    public async Task<SignalHouseResponse> RequestShortCodeAcquisitionAsync(
        string subgroupId,
        string brandId,
        string campaignId,
        string requestType,
        int? leaseTermMonths = null,
        string? requestedVanityCode = null,
        string? actualCode = null,
        string? orderId = null,
        DateTimeOffset? leaseEndDate = null,
        RequestOptions? options = null,
        Stream? leaseReceiptFile = null,
        string? leaseReceiptFileName = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "subgroupId", subgroupId }, { "brandId", brandId }, { "campaignId", campaignId }, { "requestType", requestType } });
        var acquisitionData = new { subgroupId, brandId, campaignId, requestType, leaseTermMonths, requestedVanityCode, actualCode, orderId, leaseEndDate };
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(System.Text.Json.JsonSerializer.Serialize(acquisitionData)), "acquisitionData");
        if (leaseReceiptFile != null)
        {
            var receiptContent = new StreamContent(leaseReceiptFile);
            receiptContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(receiptContent, "leaseReceipt", leaseReceiptFileName ?? "receipt");
        }
        return await _client.MultipartRequestAsync("/number/short-code", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Read the outcome of a Toll-Free number purchase by the orderId returned from
    /// <see cref="PurchaseTollFreeNumbersAsync"/>. Reports per-number ready/provisioning/failed
    /// counts (plus failure reasons) and the provisioned numbers; numbers fills in as the order
    /// completes, so poll until the order reaches a terminal state.
    /// </summary>
    /// <param name="orderId">The order id returned by PurchaseTollFreeNumbersAsync.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>API response resolving to { orderId, counts, failures, numbers }.</returns>
    public async Task<SignalHouseResponse> GetTollFreeOrderStatusAsync(
        string orderId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "orderId", orderId } });

        var safeOrderId = Uri.EscapeDataString(orderId);
        return await _client.RequestAsync($"/number/toll-free/order/{safeOrderId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Update an existing phone number's details (e.g. friendly name).
    /// </summary>
    public async Task<SignalHouseResponse> UpdatePhoneNumberAsync(
        string phoneNumber,
        Dictionary<string, object> updateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber }, { "updateData", updateData } });

        var safePhoneNumber = Uri.EscapeDataString(phoneNumber);
        return await _client.RequestAsync($"/number/{safePhoneNumber}", HttpMethod.Put, updateData, options);
    }

    /// <summary>
    /// Read a phone number's outbound caller name (CNAM) and whether one can be set.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /number/{phoneNumber}/cnam</c>.
    /// </summary>
    /// <param name="phoneNumber">The phone number (E.164, leading + optional).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>API response resolving to { phoneNumber, cnam, autoApply, eligible, ineligibleReason }.</returns>
    public async Task<SignalHouseResponse> GetCallerNameAsync(
        string phoneNumber,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber } });

        var safePhoneNumber = Uri.EscapeDataString(phoneNumber);
        return await _client.RequestAsync($"/number/{safePhoneNumber}/cnam", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Set a phone number's outbound caller name (CNAM). The name may only remove characters from the
    /// registered name of the number's brand (its campaign's brand, else <paramref name="brandId"/>, else
    /// the subgroup's default caller-ID brand), and must be 1-15 letters, digits, spaces, <c>-</c> or <c>_</c>.
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /number/{phoneNumber}/cnam</c>.
    /// </summary>
    /// <param name="phoneNumber">The phone number (E.164, leading + optional).</param>
    /// <param name="name">The caller name to show.</param>
    /// <param name="brandId">The verified brand the name comes from; only for numbers with no campaign (optional).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>API response resolving to { phoneNumber, cnam, autoApply, eligible, ineligibleReason }.</returns>
    public async Task<SignalHouseResponse> SetCallerNameAsync(
        string phoneNumber,
        string name,
        string? brandId = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber }, { "name", name } });

        var safePhoneNumber = Uri.EscapeDataString(phoneNumber);
        var body = new { name, brandId = string.IsNullOrEmpty(brandId) ? null : brandId };
        return await _client.RequestAsync($"/number/{safePhoneNumber}/cnam", HttpMethod.Put, body, options);
    }

    /// <summary>
    /// Remove a phone number's own caller name (CNAM). If its subgroup has caller name auto-apply on,
    /// the number goes back to the automatic name.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /number/{phoneNumber}/cnam</c>.
    /// </summary>
    /// <param name="phoneNumber">The phone number (E.164, leading + optional).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>API response resolving to { phoneNumber, cnam, autoApply, eligible, ineligibleReason }.</returns>
    public async Task<SignalHouseResponse> DeleteCallerNameAsync(
        string phoneNumber,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber } });

        var safePhoneNumber = Uri.EscapeDataString(phoneNumber);
        return await _client.RequestAsync($"/number/{safePhoneNumber}/cnam", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Submit a port-in request for one or more phone numbers from another provider.
    /// </summary>
    /// <param name="numberData">The port-in request data including owner info, address, phone numbers, and signature.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The created port-in request.</returns>
    public async Task<SignalHouseResponse> PortInPhoneNumberAsync(Dictionary<string, object> numberData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "numberData", numberData } });
        return await _client.RequestAsync("/number/portin", HttpMethod.Post, numberData, options);
    }

    /// <summary>
    /// Get port-in requests with optional filters.
    /// Signal House staff can view all requests; regular users see only their group's requests.
    /// </summary>
    /// <param name="groupId">Filter by group ID (optional).</param>
    /// <param name="phoneNumber">Filter by phone number (optional).</param>
    /// <param name="status">Filter by status: PENDING, IN_PROGRESS, ON_HOLD, REJECTED, SUCCESS (optional).</param>
    /// <param name="page">Page number for pagination (optional, default: 1).</param>
    /// <param name="limit">Items per page (optional, default: 20).</param>
    /// <param name="excludeDocuments">When true, leave signatureUrl and billingUrl out of each request (optional).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>Paginated port-in requests.</returns>
    public async Task<SignalHouseResponse> GetPortRequestsAsync(
        string? groupId = null,
        string? phoneNumber = null,
        string? status = null,
        int? page = null,
        int? limit = null,
        bool? excludeDocuments = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "phoneNumber", phoneNumber },
            { "status", status },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            // bool.ToString() is "True"; the API expects lowercase.
            { "excludeDocuments", excludeDocuments?.ToString().ToLowerInvariant() },
        });

        return await _client.RequestAsync($"/number/portin{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single port-in request by ID.
    /// </summary>
    /// <param name="id">The port-in request ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The port-in request details.</returns>
    public async Task<SignalHouseResponse> GetPortRequestByIdAsync(
        string id,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/number/portin/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get porting requests with optional filters. Alias for GetPortRequestsAsync with portingId filter.
    /// </summary>
    /// <param name="portingId">Filter by porting request ID (optional).</param>
    /// <param name="page">Page number for pagination (optional).</param>
    /// <param name="limit">Items per page (optional).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>Paginated porting requests.</returns>
    public async Task<SignalHouseResponse> GetPortingRequestsAsync(
        string? portingId = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "portingId", portingId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/number/portin{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Assign phone numbers to a campaign. Numbers that are not found, already RELEASED, already assigned
    /// to a different campaign, on a mismatched DCA, or (for a 10DLC campaign without a number pool)
    /// beyond the remaining 49-number-per-campaign limit are not queued; the response Data resolves to
    /// { Message, queued, skipped }, where skipped numbers were rejected individually for one of those
    /// reasons rather than failing the whole request. A Toll-Free campaign whose brand reports
    /// tollFree.maxNumbersPerCampaign as 1 holds one number: a request that would give it a second is
    /// refused with a 400 and nothing is queued.
    /// </summary>
    public async Task<SignalHouseResponse> AssignPhoneNumberToCampaignAsync(
        string campaignId,
        List<string> phoneNumbers,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId }, { "phoneNumbers", phoneNumbers } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/number/assign/{safeCampaignId}", HttpMethod.Post, new { phoneNumbers }, options);
    }

    /// <summary>
    /// Unassign phone numbers from any campaign they are currently assigned to.
    /// </summary>
    public async Task<SignalHouseResponse> UnassignPhoneNumbersFromCampaignsAsync(
        List<string> phoneNumbers,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers } });

        return await _client.RequestAsync("/number/unassign", HttpMethod.Post, new { phoneNumbers }, options);
    }

    /// <summary>
    /// Delete (release) a list of phone numbers. This operation is irreversible.
    /// </summary>
    public async Task<SignalHouseResponse> DeletePhoneNumbersAsync(
        List<string> phoneNumbers,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers } });

        return await _client.RequestAsync("/number", HttpMethod.Delete, new { phoneNumbers }, options);
    }

    /// <summary>
    /// Deactivate one or more phone numbers. Numbers must be in READY status.
    /// </summary>
    public async Task<SignalHouseResponse> DeactivatePhoneNumbersAsync(
        List<string> phoneNumbers,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers } });

        return await _client.RequestAsync("/number/deactivate", HttpMethod.Post, new { phoneNumbers }, options);
    }

    /// <summary>
    /// Reactivate one or more previously deactivated phone numbers.
    /// </summary>
    public async Task<SignalHouseResponse> ReactivatePhoneNumbersAsync(
        List<string> phoneNumbers,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers } });

        return await _client.RequestAsync("/number/reactivate", HttpMethod.Post, new { phoneNumbers }, options);
    }

    /// <summary>
    /// Transfer unassigned phone numbers to a different subgroup.
    /// </summary>
    public async Task<SignalHouseResponse> TransferPhoneNumbersAsync(
        List<string> phoneNumbers,
        string newSubgroupId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumbers", phoneNumbers }, { "newSubgroupId", newSubgroupId } });

        return await _client.RequestAsync("/number/transfer", HttpMethod.Post, new { phoneNumbers, newSubgroupId }, options);
    }

    /// <summary>
    /// Search NPA/NXX lookup data with optional filters.
    /// Country may scope NPA/NXX filters; state and city cannot be combined with them.
    /// </summary>
    public async Task<SignalHouseResponse> SearchNpaNxxAsync(
        string? country = null,
        string? state = null,
        string? city = null,
        string? npa = null,
        string? nxx = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "country", country },
            { "state", state },
            { "city", city },
            { "npa", npa },
            { "nxx", nxx },
        });

        return await _client.RequestAsync($"/number/lookup{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Batch lookup city/state for NPA/NXX pairs (max 50 entries).
    /// </summary>
    public async Task<SignalHouseResponse> LookupLocationsAsync(
        List<Dictionary<string, string>> entries,
        RequestOptions? options = null)
    {
        return await _client.RequestAsync("/number/lookup/location", HttpMethod.Post, new { entries }, options);
    }

    /// <summary>
    /// Migrate a phone number (or all numbers in a group) from v1 to v2.
    /// </summary>
    /// <param name="phoneNumber">Phone number to migrate (optional). If provided, groupId is ignored.</param>
    /// <param name="campaignId">Campaign ID to migrate all numbers for (optional).</param>
    /// <param name="groupId">Group ID to migrate all non-released numbers for (optional).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>API response with migration queue confirmation.</returns>
    public async Task<SignalHouseResponse> MigratePhoneNumberToV2Async(
        string? phoneNumber = null,
        string? campaignId = null,
        string? groupId = null,
        RequestOptions? options = null)
    {
        return await _client.RequestAsync("/number/migrate", HttpMethod.Post, new { phoneNumber, campaignId, groupId }, options);
    }
}
