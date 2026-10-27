using System.Net.Http.Headers;
using System.Text.Json;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// 10DLC brand registration and management operations.
/// Brand lookup id: carrier Brand ID (B… or TFNB…), record _id, or internal reference UUID.
/// Read/create responses return brandId: null for pending brands — poll GET /brand?id=&lt;record _id&gt;.
/// </summary>
public sealed class BrandsDomain
{
    private readonly HttpClientWrapper _client;

    internal BrandsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get a list of brands with optional filters. brandId may be null until carrier assignment.
    /// </summary>
    /// <param name="registrationType">Optional registration-type filter ("TEN_DLC", "TOLL_FREE", or "SHORT_CODE").</param>
    /// <param name="currentTollFreeCarrier">With groupId or subgroupId: when true, return only toll-free brands that a new toll-free campaign in that group can use (those on the group's current toll-free carrier).</param>
    public async Task<SignalHouseResponse> GetBrandsAsync(
        string? id = null,
        string? subgroupId = null,
        string? groupId = null,
        int? page = null,
        int? limit = null,
        string? status = null,
        string? registrationType = null,
        RequestOptions? options = null,
        bool? currentTollFreeCarrier = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
            { "subgroupId", subgroupId },
            { "groupId", groupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "status", status },
            { "registrationType", registrationType },
            // bool.ToString() is "True"; the API expects lowercase.
            { "currentTollFreeCarrier", currentTollFreeCarrier?.ToString().ToLowerInvariant() },
        });

        return await _client.RequestAsync($"/brand{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get external vetting information for a brand.
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    public async Task<SignalHouseResponse> GetExternalVettingAsync(string brandId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        return await _client.RequestAsync($"/brand/externalvetting/{safeBrandId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new brand. Response brandId may be null until carrier assignment.
    /// </summary>
    /// <param name="brandData">Brand creation payload (see API docs for required fields).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CreateBrandAsync(Dictionary<string, object> brandData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandData", brandData } });

        return await _client.RequestAsync("/brand", HttpMethod.Post, brandData, options);
    }

    /// <summary>
    /// Create a new Toll-Free (TFN) brand. registrationType is forced to TOLL_FREE server-side.
    /// TFN-specific fields live under brandData["tollFree"]. Response brandId is a TFNB-prefixed id
    /// when assigned; otherwise null — poll by _id.
    /// </summary>
    /// <param name="brandData">Toll-Free brand creation payload (see API docs for required fields, including the tollFree sub-object). tollFree.supportPhone must be E.164 with an optional leading "+" and no spaces, parentheses or hyphens (e.g. "17025550100").</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CreateTollFreeBrandAsync(Dictionary<string, object> brandData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandData", brandData } });

        return await _client.RequestAsync("/brand/toll-free", HttpMethod.Post, brandData, options);
    }

    /// <summary>
    /// Get a group's business verification, or one subgroup's when the group verifies each subgroup's business separately:
    /// whether the group's toll-free brands need one, its status and the submitted details. The response includes
    /// subgroupId only when the business verification belongs to one subgroup.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="subgroupId">The subgroup whose business verification to get, sent as a query parameter only when given. Required for a group that verifies each subgroup's business separately; in any other group it must belong to the group and is otherwise ignored.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetBusinessVerificationAsync(string groupId, string? subgroupId = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "subgroupId", subgroupId } });
        return await _client.RequestAsync($"/brand/business-verification/{safeGroupId}{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Submit or update a group's business verification, or one subgroup's when the group verifies each subgroup's business
    /// separately. Allowed when it is not started or needs changes.
    /// Review usually takes 1–2 business days; the toll-free brands it covers stay pending until it is verified.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="details">The business verification: businessName, businessType, industry, registrationType, registrationNumber, website, street, street2 (optional), city, state, postalCode, country ("US" or "CA") and a representative object (firstName, lastName, email, phone in E.164, jobTitle, jobLevel). See API docs for the allowed values.</param>
    /// <param name="subgroupId">The subgroup whose business verification to submit, sent as a query parameter only when given. Required for a group that verifies each subgroup's business separately; in any other group it must belong to the group and is otherwise ignored. The subgroup must be active.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> SubmitBusinessVerificationAsync(string groupId, Dictionary<string, object> details, string? subgroupId = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "details", details } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "subgroupId", subgroupId } });
        return await _client.RequestAsync($"/brand/business-verification/{safeGroupId}{qs}", HttpMethod.Put, details, options);
    }

    /// <summary>
    /// Get a group's toll-free ISV account: the group's own parent account at the toll-free provider, registered by
    /// Signal House staff. The API key secret is never returned. Staff-only (signalhouse_api, signalhouse_admin or
    /// signalhouse_user).
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetTollFreeIsvAccountAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/brand/toll-free-isv-account/{safeGroupId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Register a group's toll-free ISV account, or replace the API key of the one already registered. The provider checks
    /// the account and key before they are stored. Once the group has toll-free records at that provider, only the API key
    /// of the registered account can be replaced. signalhouse_admin only.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="accountSid">The ISV's own top-level account SID (AC followed by 32 lowercase hex characters).</param>
    /// <param name="apiKeySid">The SID of a Main API key in that account (SK followed by 32 lowercase hex characters).</param>
    /// <param name="apiKeySecret">That API key's secret (1 to 128 characters). It is stored encrypted and never returned.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> SetTollFreeIsvAccountAsync(string groupId, string accountSid, string apiKeySid, string apiKeySecret, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "accountSid", accountSid }, { "apiKeySid", apiKeySid }, { "apiKeySecret", apiKeySecret } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/brand/toll-free-isv-account/{safeGroupId}", HttpMethod.Put, new
        {
            accountSid,
            apiKeySid,
            apiKeySecret,
        }, options);
    }

    /// <summary>
    /// Create a local Short Code brand for manual Signal House review. registrationType is forced to
    /// SHORT_CODE server-side; the synchronous response is a 201-created PENDING_APPROVAL record
    /// with an SCB-prefixed brandId.
    /// </summary>
    /// <param name="brandData">Short Code payload with contact/address fields and a shortCode object.</param>
    public async Task<SignalHouseResponse> CreateShortCodeBrandAsync(Dictionary<string, object> brandData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandData", brandData } });
        return await _client.RequestAsync("/brand/short-code", HttpMethod.Post, brandData, options);
    }

    /// <summary>Approve a pending Short Code brand. signalhouse_admin only.</summary>
    public async Task<SignalHouseResponse> ApproveShortCodeBrandAsync(string brandId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });
        return await _client.RequestAsync($"/brand/approve/{Uri.EscapeDataString(brandId)}", HttpMethod.Post, options: options);
    }

    /// <summary>Reject a pending Short Code brand with a customer-visible reason. signalhouse_admin only.</summary>
    public async Task<SignalHouseResponse> RejectShortCodeBrandAsync(string brandId, string internalRejectionReason, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId }, { "internalRejectionReason", internalRejectionReason } });
        return await _client.RequestAsync($"/brand/reject/{Uri.EscapeDataString(brandId)}", HttpMethod.Post, new { internalRejectionReason }, options);
    }

    /// <summary>
    /// Transfer one or more brands to a different subgroup.
    /// </summary>
    /// <param name="brandIds">Brand lookup ids to transfer.</param>
    public async Task<SignalHouseResponse> TransferBrandAsync(
        string subgroupId,
        List<string> brandIds,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "subgroupId", subgroupId }, { "brandIds", brandIds } });

        var safeSubgroupId = Uri.EscapeDataString(subgroupId);
        return await _client.RequestAsync($"/brand/transfer/{safeSubgroupId}", HttpMethod.Post, new { brandIds }, options);
    }

    /// <summary>
    /// Create external vetting for a brand.
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    /// <param name="vettingProviderId">External vetting provider: AEGIS or WMC.</param>
    /// <param name="vettingClass">Vetting class: STANDARD, ENHANCED, AUTHPLUS, or RCS (AEGIS accepts all four; WMC accepts STANDARD only).</param>
    /// <param name="additionalData">Provider-specific additional data forwarded to TCR; null to omit.</param>
    public async Task<SignalHouseResponse> CreateExternalVettingAsync(string brandId, string vettingProviderId, string vettingClass, Dictionary<string, object>? additionalData = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId }, { "vettingProviderId", vettingProviderId }, { "vettingClass", vettingClass } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        var body = new Dictionary<string, object> { { "vettingProviderId", vettingProviderId }, { "vettingClass", vettingClass } };
        if (additionalData is not null) body["additionalData"] = additionalData;
        return await _client.RequestAsync($"/brand/externalvetting/{safeBrandId}", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Import an existing external vetting record for a brand.
    ///
    /// Unlike <see cref="CreateExternalVettingAsync"/> (which orders a new, billable vetting), this
    /// attaches a vetting the brand already completed directly with the provider, using the
    /// provider-issued vettingId and vettingToken. It is synchronous and not billable.
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    /// <param name="vettingProviderId">External vetting provider (AEGIS, WMC, CV).</param>
    public async Task<SignalHouseResponse> ImportExternalVettingAsync(string brandId, string vettingProviderId, string vettingId, string? vettingToken = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId }, { "vettingProviderId", vettingProviderId }, { "vettingId", vettingId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        var body = new Dictionary<string, object> { { "vettingProviderId", vettingProviderId }, { "vettingId", vettingId } };
        if (vettingToken is not null) body["vettingToken"] = vettingToken;
        return await _client.RequestAsync($"/brand/externalvetting/import/{safeBrandId}", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Update a brand's information.
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    /// <param name="brandData">Fields to update. For a Toll-Free brand, pass the editable Toll-Free fields under a "tollFree" key (legalEntityType, businessRegistrationType, taxId, countryCode, supportPhone, taxIdIssuingCountry, businessDBA, businessWebsite); supportPhone must be E.164 with an optional leading "+" and no spaces, parentheses or hyphens (e.g. "17025550100"); subgroupId is immutable.</param>
    public async Task<SignalHouseResponse> UpdateBrandAsync(
        string brandId,
        Dictionary<string, object> brandData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        return await _client.RequestAsync($"/brand/{safeBrandId}", HttpMethod.Put, brandData, options);
    }

    /// <summary>
    /// Re-vet a brand that is in UNVERIFIED status after a previous verification.
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    public async Task<SignalHouseResponse> RevetBrandAsync(string brandId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        return await _client.RequestAsync($"/brand/revet/{safeBrandId}", HttpMethod.Put, options: options);
    }

    /// <summary>
    /// Delete a brand (mark as DELETED).
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    public async Task<SignalHouseResponse> DeleteBrandAsync(string brandId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        return await _client.RequestAsync($"/brand/{safeBrandId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Get appeal history for a brand.
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetAppealHistoryAsync(string brandId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        return await _client.RequestAsync($"/brand/appeal/{safeBrandId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Submit an appeal for a brand (multipart/form-data).
    /// </summary>
    /// <param name="brandId">Brand lookup id (carrier Brand ID, record _id, or internal reference).</param>
    /// <param name="appealCategories">List of appeal category strings.</param>
    /// <param name="explanation">Explanation text for the appeal.</param>
    /// <param name="file">Optional supporting document stream.</param>
    /// <param name="fileName">Filename for the supporting document (required when file is provided).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> SubmitAppealAsync(
        string brandId,
        List<string> appealCategories,
        string explanation,
        Stream? file = null,
        string? fileName = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "brandId", brandId },
            { "appealCategories", appealCategories },
            { "explanation", explanation },
        });

        var safeBrandId = Uri.EscapeDataString(brandId);
        var content = new MultipartFormDataContent();

        if (file is not null)
        {
            var streamContent = new StreamContent(file);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName ?? "file");
        }

        content.Add(new StringContent(JsonSerializer.Serialize(appealCategories)), "appealCategories");
        content.Add(new StringContent(explanation), "explanation");

        return await _client.MultipartRequestAsync($"/brand/appeal/{safeBrandId}", HttpMethod.Post, content, options);
    }
}
