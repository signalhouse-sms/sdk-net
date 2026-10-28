using System.Net.Http.Headers;
using System.Text.Json;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Admin-only campaign operations (approve, reject).
/// </summary>
public sealed class CampaignsAdminDomain
{
    private readonly HttpClientWrapper _client;

    internal CampaignsAdminDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Approve a campaign that is pending approval.
    /// </summary>
    public async Task<SignalHouseResponse> ApproveCampaignAsync(string campaignId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/approve/{safeCampaignId}", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Reject a campaign that is pending approval.
    /// Pass <c>rejectionReason</c> (10-256 characters) in options body.
    /// </summary>
    public async Task<SignalHouseResponse> RejectCampaignAsync(string campaignId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/reject/{safeCampaignId}", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Transition a Short Code campaign's review status. A customer-visible reason is
    /// required for both rejection states ("REJECTED" = Signal House Rejected, "DCA_REJECTED" =
    /// Rejected). Selecting <c>PENDING_DCA_APPROVAL</c> runs the guarded carrier-review transition;
    /// fulfillment remains a dedicated staff operation.
    /// </summary>
    /// <param name="campaignId">The ID of the campaign to transition.</param>
    /// <param name="status">The target status: "PENDING_REVIEW", "REJECTED", "PENDING_DCA_APPROVAL", "DCA_REJECTED", or "ACTIVE". PENDING_DCA_APPROVAL requires a Signal House-approved brand, issued code, and captured opt-in proof.</param>
    /// <param name="rejectionReason">Required and nonblank for "REJECTED"/"DCA_REJECTED" (max 1024 characters).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> UpdateShortCodeCampaignStatusAsync(
        string campaignId,
        string status,
        string? rejectionReason = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId }, { "status", status } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        var body = new Dictionary<string, object?> { { "status", status }, { "rejectionReason", rejectionReason } };
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}/status", HttpMethod.Put, body, options);
    }

    /// <summary>Record the issued code and fixed lease end date for a campaign-bound Signal House Short Code request. A date-only leaseEndDate expires at 23:59:59 UTC.</summary>
    public async Task<SignalHouseResponse> FulfillShortCodeCampaignAsync(string campaignId, string actualCode, string leaseEndDate, string? internalNotes = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId }, { "actualCode", actualCode }, { "leaseEndDate", leaseEndDate } });
        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}/fulfill", HttpMethod.Post, new { actualCode, leaseEndDate, internalNotes }, options);
    }

    /// <summary>Submit an internally approved Short Code campaign to carrier review.</summary>
    public async Task<SignalHouseResponse> SubmitShortCodeCampaignToCarrierAsync(string campaignId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });
        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}/submit-to-carrier", HttpMethod.Post, options: options);
    }

    /// <summary>Confirm Registry offboarding and complete a pending Short Code cancellation.</summary>
    public async Task<SignalHouseResponse> CompleteShortCodeCancellationAsync(string campaignId, string requestId, string? note = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId }, { "requestId", requestId } });
        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}/cancellation/complete", HttpMethod.Post, new { requestId, note }, options);
    }

    /// <summary>Dismiss a pending Short Code cancellation and restore its prior lifecycle state.</summary>
    public async Task<SignalHouseResponse> DismissShortCodeCancellationAsync(string campaignId, string requestId, string? note = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId }, { "requestId", requestId } });
        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}/cancellation/dismiss", HttpMethod.Post, new { requestId, note }, options);
    }

    /// <summary>Read expired Short Code leases awaiting manual Registry offboarding.</summary>
    public async Task<SignalHouseResponse> GetExpiredShortCodeLeasesAsync(int? page = null, int? limit = null, RequestOptions? options = null)
    {
        var query = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "page", page?.ToString() }, { "limit", limit?.ToString() } });
        return await _client.RequestAsync($"/campaign/short-code/expired-leases{query}", HttpMethod.Get, options: options);
    }

    /// <summary>Confirm Registry offboarding and mark an expired Short Code released.</summary>
    public async Task<SignalHouseResponse> ReleaseExpiredShortCodeLeaseAsync(string campaignId, string? note = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });
        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}/expired-lease/release", HttpMethod.Post, new { note }, options);
    }
}

/// <summary>
/// 10DLC campaign registration and management operations.
/// </summary>
public sealed class CampaignsDomain
{
    private readonly HttpClientWrapper _client;

    /// <summary>
    /// Admin-only operations. Null when enableAdmin is false.
    /// </summary>
    public CampaignsAdminDomain? Admin { get; }

    internal CampaignsDomain(HttpClientWrapper client, bool enableAdmin)
    {
        _client = client;
        Admin = enableAdmin ? new CampaignsAdminDomain(client) : null;
    }

    /// <summary>
    /// Get a list of campaigns with optional filters.
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
    ///
    /// Every record also carries a `carrierSuspensions` array -- one entry per carrier suspension of the
    /// campaign: `{ mnoId, mnoName, source, suspensionCategory, reason, tcrEventType, suspendedAt, liftedAt }`.
    /// An entry is active while `liftedAt` is null. `mnoId` is the TCR carrier network id (same ids as `mnoIds`);
    /// `source` is CARRIER, UPSTREAM or BRAND (the carrier suspended the whole brand). A suspension applies to one
    /// carrier, so `status` stays ACTIVE. Subscribe to the CAMPAIGN_SUSPENDED / CAMPAIGN_UNSUSPENDED webhook events
    /// to be told when an entry opens or is lifted.
    /// </remarks>
    /// <param name="registrationType">Optional registration-type filter ("TEN_DLC", "TOLL_FREE", or "SHORT_CODE").</param>
    public async Task<SignalHouseResponse> GetCampaignsAsync(
        string? id = null,
        string? brandId = null,
        string? subgroupId = null,
        string? groupId = null,
        int? page = null,
        int? limit = null,
        string? status = null,
        string? registrationType = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
            { "brandId", brandId },
            { "subgroupId", subgroupId },
            { "groupId", groupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "status", status },
            { "registrationType", registrationType },
        });

        return await _client.RequestAsync($"/campaign{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get aggregated campaign health (7-day and 30-day windows) for a campaign.
    /// </summary>
    public async Task<SignalHouseResponse> GetCampaignHealthAsync(
        string campaignId,
        bool? includeNumbers = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "campaignId", campaignId },
            { "includeNumbers", includeNumbers?.ToString().ToLowerInvariant() },
        });

        return await _client.RequestAsync($"/campaign/health{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get the carrier throughput ceilings a campaign is currently sending under: the AT&amp;T per-minute
    /// limit (per campaign, held 10% under the published ceiling) and the T-Mobile daily cap (per brand),
    /// each with its current tier and the next rung on the ladder, plus today's T-Mobile usage. Resolved
    /// from the same brand status / brand score rules the send path enforces with.
    /// Response: { campaignId, brandId, usecase, brand: { status, brandScore, entityType }, att: { tier,
    /// scope, unit, sms: { limit }, mms: { limit }, marginPercent, nextTier }, tmobile: { tier, scope, unit,
    /// dailyLimit, thresholds, nextTier, usedToday: { date, segments, resetsAt } | null } }. A null
    /// tier/limit means the carrier ladder does not apply (unverified brand, GOVERNMENT entity,
    /// PUBLIC_SAFETY_RESTRICTED usecase); a null nextTier means the top rung or a usecase-capped
    /// (LOW_VOLUME) campaign.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    public async Task<SignalHouseResponse> GetCampaignThroughputLimitsAsync(
        string campaignId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "campaignId", campaignId } });

        return await _client.RequestAsync($"/campaign/throughput-limits{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new campaign.
    /// </summary>
    /// <param name="campaignData">Campaign creation payload. Optional "sampleMediaIds" (10DLC only): up to 5 mediaId strings from UploadSampleMediaAsync, attached to the TCR campaign as sample MMS media for carrier review.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CreateCampaignAsync(Dictionary<string, object> campaignData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignData", campaignData } });

        return await _client.RequestAsync("/campaign", HttpMethod.Post, campaignData, options);
    }

    /// <summary>
    /// Create a new Toll-Free (TFN) campaign and submit it for Signal House review.
    /// registrationType is forced to TOLL_FREE server-side. TFN-specific fields live under
    /// campaignData["tollFree"].
    /// </summary>
    /// <param name="campaignData">Toll-Free campaign creation payload (see API docs for required fields, including the tollFree sub-object).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CreateTollFreeCampaignAsync(Dictionary<string, object> campaignData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignData", campaignData } });

        return await _client.RequestAsync("/campaign/toll-free", HttpMethod.Post, campaignData, options);
    }

    /// <summary>
    /// Create a new Short Code campaign and submit it for Signal House review.
    /// Requires an approved (VERIFIED) Short Code brand. Upload the opt-in proof screenshot first with
    /// UploadOptInImageAsync and pass the returned URL as campaignData["shortCode"]["screenshotUrl"] (required;
    /// external URLs are rejected). campaignData["shortCode"]["optInUrl"] (required) is the public HTTPS link to the live
    /// opt-in experience. Request or register the campaign's Short Code separately through the Numbers domain after creation.
    /// </summary>
    /// <param name="campaignData">The Short Code campaign data, including the required shortCode.screenshotUrl and shortCode.optInUrl.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CreateShortCodeCampaignAsync(
        Dictionary<string, object> campaignData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignData", campaignData } });

        var content = new MultipartFormDataContent();
        content.Add(new StringContent(JsonSerializer.Serialize(campaignData)), "campaignData");

        return await _client.MultipartRequestAsync("/campaign/short-code", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Update a Short Code campaign's editable fields. Only permitted while the
    /// campaign is in Signal House Review or Signal House Rejected status; the number source
    /// cannot be changed once submitted.
    /// </summary>
    /// <param name="campaignId">The ID of the campaign to update.</param>
    /// <param name="updateData">The fields to update (top-level campaign fields plus an optional "shortCode" object of editable Short Code fields).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> UpdateShortCodeCampaignAsync(
        string campaignId,
        Dictionary<string, object> updateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}", HttpMethod.Put, updateData, options);
    }

    /// <summary>
    /// Cancel a Short Code campaign. Issued customer leases return PENDING_DELETE until Signal House
    /// confirms Registry offboarding; safe-to-dispose and staff cancellations return EXPIRED immediately.
    /// </summary>
    public async Task<SignalHouseResponse> CancelShortCodeCampaignAsync(string campaignId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/short-code/{safeCampaignId}/cancel", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Download a private Short Code external lease receipt. Opt-in screenshots are public at the
    /// shortCode.screenshotUrl returned on the campaign.
    /// </summary>
    /// <param name="artifactId">The receipt identifier from numberSource.externalLease.receiptArtifactId.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> ReadCampaignArtifactAsync(string artifactId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "artifactId", artifactId } });

        var safeArtifactId = Uri.EscapeDataString(artifactId);
        return await _client.RequestAsync($"/campaign/short-code/artifact/{safeArtifactId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Upload an opt-in proof image to be hosted on-platform (multipart/form-data), returning the
    /// hosted image's id and URL suitable for a Toll-Free campaign's optInImageURLs.
    /// </summary>
    /// <param name="file">The image file stream to upload.</param>
    /// <param name="fileName">Filename for the uploaded image (defaults to "file").</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> UploadOptInImageAsync(
        Stream file,
        string? fileName = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "file", file } });

        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName ?? "file");

        return await _client.MultipartRequestAsync("/campaign/opt-in-image", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Upload a sample media file (JPEG, PNG, GIF, MP4 or MP3, up to 5MB) for a 10DLC campaign
    /// (multipart/form-data). Reference the returned mediaId in sampleMediaIds on campaign create or
    /// update; it is attached to the TCR campaign for carrier review.
    /// Response: { mediaId, fileName, mimeType, size }.
    /// </summary>
    /// <param name="file">The media file stream to upload.</param>
    /// <param name="fileName">Filename for the uploaded media (defaults to "file").</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <param name="groupId">The group the file belongs to. Required with a staff (signalhouse_*) key, which has no active group; otherwise optional and must be your own group.</param>
    public async Task<SignalHouseResponse> UploadSampleMediaAsync(
        Stream file,
        string? fileName = null,
        RequestOptions? options = null,
        string? groupId = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "file", file } });

        var content = new MultipartFormDataContent();
        if (!string.IsNullOrEmpty(groupId))
        {
            content.Add(new StringContent(groupId), "groupId");
        }
        var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName ?? "file");

        return await _client.MultipartRequestAsync("/campaign/sample-media", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Download a campaign sample media file your group owns.
    /// </summary>
    /// <param name="mediaId">The mediaId returned by UploadSampleMediaAsync.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> ReadSampleMediaAsync(string mediaId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "mediaId", mediaId } });

        var safeMediaId = Uri.EscapeDataString(mediaId);
        return await _client.RequestAsync($"/campaign/sample-media/{safeMediaId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Auto-capture an opt-in proof image from a brand's generated landing page and host it
    /// on-platform, returning the hosted image's id and URL suitable for a Toll-Free campaign's
    /// optInImageURLs.
    /// </summary>
    /// <param name="brandId">The ID of the brand whose generated landing page should be captured.</param>
    /// <param name="landingId">The specific page to capture. Required for Toll-Free brands, which have one page per campaign — omitted, the request is refused rather than guessing. A 10DLC brand has a single page, so it may be omitted there.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CaptureOptInImageFromLandingAsync(string brandId, string? landingId = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        object body = landingId is null ? new { brandId } : new { brandId, landingId };
        return await _client.RequestAsync("/campaign/opt-in-image/from-landing", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Update an existing campaign.
    /// </summary>
    /// <param name="campaignData">Fields to update. For a Toll-Free campaign, pass the editable Toll-Free fields under a "tollFree" key (useCase, messageVolume, programSummary, exampleMessage, customerCareEmail, optInImageURLs, optIns, multiNumberReason, useCases, channels, landingId); phoneNumbers cannot be changed — Toll-Free numbers are locked to their campaign. For a 10DLC campaign, an optional "sampleMediaIds" list (up to 5 mediaId strings from UploadSampleMediaAsync) sets the sample MMS media for carrier review.</param>
    public async Task<SignalHouseResponse> UpdateCampaignAsync(
        string campaignId,
        Dictionary<string, object> campaignData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/{safeCampaignId}", HttpMethod.Put, campaignData, options);
    }

    /// <summary>
    /// Delete a campaign (mark as EXPIRED).
    /// </summary>
    public async Task<SignalHouseResponse> DeleteCampaignAsync(string campaignId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/{safeCampaignId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Appeal a DCA rejection for a campaign.
    /// </summary>
    public async Task<SignalHouseResponse> AppealDcaRejectionAsync(string campaignId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/appealDcaRejection/{safeCampaignId}", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Nudge a connectivity partner to prioritize review of a campaign.
    /// </summary>
    public async Task<SignalHouseResponse> NudgeDcaForCampaignAsync(string campaignId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "campaignId", campaignId } });

        var safeCampaignId = Uri.EscapeDataString(campaignId);
        return await _client.RequestAsync($"/campaign/nudge/{safeCampaignId}", HttpMethod.Post, options: options);
    }
}
