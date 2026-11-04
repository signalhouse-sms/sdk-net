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
    /// Each brand carries `carrierSuspensions` (carrier brand suspension rules; active while liftedAt is null).
    /// Each toll-free brand carries `tollFree.reviewsBusinessOnSubmit`: whether it's reviewed when it's submitted and keeps
    /// its business details from then on. When that is true, the brand also carries its tollFree businessStructure,
    /// industry, contactEmail, contactJobTitle and contactJobLevel, and `tollFree.rejectionReason`: why it was rejected,
    /// in plain English, or null. On other brands these are null or absent.
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
    /// Some groups' toll-free brands are reviewed when they're submitted: <see cref="GetTollFreeRequirementsAsync"/> says
    /// whether a group's are, and the response's tollFree.reviewsBusinessOnSubmit whether the brand is. Such a brand stays
    /// DCA_PENDING until it's reviewed, which usually takes 1–2 business days, then becomes DCA_ACTIVE if it passes, or
    /// DCA_REJECTED with tollFree.rejectionReason saying why if it doesn't. A rejected brand is fixed by creating the brand
    /// again with the corrections. Its business details can't be changed after it's submitted: companyName, street, city,
    /// state, postalCode, country, firstName, lastName, and tollFree's businessRegistrationType, taxId, countryCode,
    /// taxIdIssuingCountry, businessStructure, industry, contactEmail, contactJobTitle and contactJobLevel.
    /// </summary>
    /// <param name="brandData">Toll-Free brand creation payload (see API docs for required fields, including the tollFree sub-object). tollFree.supportPhone must be E.164 with an optional leading "+" and no spaces, parentheses or hyphens (e.g. "17025550100"). A group whose toll-free brands are reviewed when they're submitted must also send tollFree.businessStructure (CO_OPERATIVE, CORPORATION, LIMITED_LIABILITY_CORPORATION, NON_PROFIT_CORPORATION or PARTNERSHIP) and tollFree.industry (AGRICULTURE, AUTOMOTIVE, BANKING, CONSTRUCTION, CONSUMER, EDUCATION, ELECTRONICS, ENGINEERING, ENERGY, FAST_MOVING_CONSUMER_GOODS, FINANCIAL, FINTECH, FOOD_AND_BEVERAGE, GOVERNMENT, HEALTHCARE, HOSPITALITY, INSURANCE, JEWELRY, LEGAL, MANUFACTURING, MEDIA, NOT_FOR_PROFIT, OIL_AND_GAS, ONLINE, PROFESSIONAL_SERVICES, RAW_MATERIALS, REAL_ESTATE, RELIGION, RETAIL, TECHNOLOGY, TELECOMMUNICATIONS, TRANSPORTATION or TRAVEL), and for the contact named by firstName and lastName, tollFree.contactEmail (a valid email, at most 100 characters), tollFree.contactJobTitle (1 to 100 characters) and tollFree.contactJobLevel (DIRECTOR, GM, VP, CEO, CFO, GENERAL_COUNSEL or OTHER); its tollFree.countryCode must be "US" or "CA" and its state a 2-letter state or province code. Any other group gets 400 if it sends tollFree.businessStructure, industry, contactEmail, contactJobTitle or contactJobLevel.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CreateTollFreeBrandAsync(Dictionary<string, object> brandData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandData", brandData } });

        return await _client.RequestAsync("/brand/toll-free", HttpMethod.Post, brandData, options);
    }

    /// <summary>
    /// Get whether a group's new toll-free brands are reviewed when they're submitted, which decides what they need.
    /// Returns { groupId, reviewsBusinessOnSubmit }. When reviewsBusinessOnSubmit is true, a new toll-free brand needs
    /// tollFree's businessStructure, industry, contactEmail, contactJobTitle and contactJobLevel, a tollFree.countryCode of
    /// "US" or "CA" and a 2-letter state or province code as its state; it stays DCA_PENDING until it's reviewed, and its
    /// business details can't be changed after it's submitted. When it is false, tollFree's businessStructure, industry,
    /// contactEmail, contactJobTitle and contactJobLevel are refused (400).
    /// </summary>
    /// <param name="groupId">The group ID (any group the caller can view).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetTollFreeRequirementsAsync(string groupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var safeGroupId = Uri.EscapeDataString(groupId);
        return await _client.RequestAsync($"/brand/toll-free-requirements/{safeGroupId}", HttpMethod.Get, options: options);
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
