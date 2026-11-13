using System.Net.Http.Headers;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// RCS (Google RBM) resources. Templates for RCS live under <c>client.Templates</c>.
/// <list type="bullet">
///   <item><c>client.Rcs.Agents</c> — RCS agents under /rcs/agent.</item>
///   <item><c>client.Rcs.Messages</c> — RCS messages: send, validate, estimate, capabilities, get and list.</item>
/// </list>
/// </summary>
public sealed class RcsDomain
{
    /// <summary>RCS agents under /rcs/agent.</summary>
    public RcsAgentsDomain Agents { get; }

    /// <summary>RCS messages: send, validate, estimate, capabilities, get and list.</summary>
    public RcsMessagesDomain Messages { get; }

    internal RcsDomain(HttpClientWrapper client)
    {
        Agents = new RcsAgentsDomain(client);
        Messages = new RcsMessagesDomain(client);
    }
}

/// <summary>
/// RCS agents under /rcs/agent. An agent links one brand and one campaign, of any registration type: the campaign's
/// numbers are the SMS fallback, and the brand and campaign supply the company identity and consent text for the
/// carrier launch. One brand or campaign can serve many agents. An agent is a registration (type RCS_AGENT, id RCSA…),
/// so the Registrations methods read and edit the same record. 404 while the environment's RCS switch is off; 400 while
/// the group's rcsEnabled flag is off.
/// </summary>
public sealed class RcsAgentsDomain
{
    private readonly HttpClientWrapper _client;

    internal RcsAgentsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Create an agent draft on a brand, optionally with its campaign (HTTP 201, status DRAFT). The agent's
    /// subgroup is the brand's. Blank consent fields fill from the linked campaign. Set RequestOptions.IdempotencyKey
    /// to make a retry safe.
    /// </summary>
    /// <param name="brandId">The brand: carrier brand ID, provisional UUID or _id.</param>
    /// <param name="campaignId">Optional campaign of that brand (campaign ID or _id); required before submit.</param>
    /// <param name="agent">Optional profile { name, description, color, logoUrl, heroUrl, phone, website, email, phoneLabel, websiteLabel, emailLabel (captions, max 40; submit defaults them to Call, Website and Email), privacyPolicyUrl, termsUrl } (a Dictionary, JsonElement or JsonNode).</param>
    /// <param name="consent">Optional launch text { optInDetails, optInUrl, optInResponse, optOutDetails, optOutResponse, helpResponse, campaignDescription, messageExamples, estimatedMonthlyVolume, launchVideoUrl }; every field is required before the launch request.</param>
    /// <param name="contact">Optional launch contact { firstName, lastName, role, email, mobile }: the individual at the brand the carrier partner's reviewers may write to; required in full before the launch request.</param>
    /// <param name="useCase">Optional OTP, TRANSACTIONAL, PROMOTIONAL or MULTI_USE; required before submit.</param>
    /// <param name="billingCategory">Optional NON_CONVERSATIONAL (default, per message) or CONVERSATIONAL (per interactive session).</param>
    /// <param name="defaultFallback">Optional { mode (auto, rcs_only, sms_only, none), channel (auto, sms, mms), from (a number on the linked campaign) }.</param>
    /// <param name="excludedCarriers">Optional [] or ["T_MOBILE"] (the carrier partner's carrier name).</param>
    /// <param name="testers">Optional tester numbers (up to 200), invited once the agent exists at the carrier.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The new agent.</returns>
    /// <exception cref="ValidationException">brandId is null.</exception>
    public Task<SignalHouseResponse> CreateAsync(
        string brandId,
        string? campaignId = null,
        object? agent = null,
        object? consent = null,
        object? contact = null,
        string? useCase = null,
        string? billingCategory = null,
        object? defaultFallback = null,
        List<string>? excludedCarriers = null,
        List<string>? testers = null,
        RequestOptions? options = null)
    {
        return CreateAsync(brandId, autofill: null, campaignId: campaignId, agent: agent, consent: consent, contact: contact,
            useCase: useCase, billingCategory: billingCategory, defaultFallback: defaultFallback, excludedCarriers: excludedCarriers,
            testers: testers, options: options);
    }

    /// <summary>
    /// Create an agent draft with prefill metadata. See the overload without <paramref name="autofill"/> to create without it.
    /// </summary>
    /// <param name="brandId">The brand: carrier brand ID, provisional UUID or _id.</param>
    /// <param name="autofill">Where prefilled fields came from, keyed by path ("consent.optInDetails" to { source, sourceId, at, edited }); the API adds its own markers.</param>
    /// <param name="campaignId">Optional campaign of that brand (campaign ID or _id); required before submit.</param>
    /// <param name="agent">Optional profile fields.</param>
    /// <param name="consent">Optional launch text fields.</param>
    /// <param name="contact">Optional launch contact fields.</param>
    /// <param name="useCase">Optional OTP, TRANSACTIONAL, PROMOTIONAL or MULTI_USE.</param>
    /// <param name="billingCategory">Optional NON_CONVERSATIONAL or CONVERSATIONAL.</param>
    /// <param name="defaultFallback">Optional SMS fallback.</param>
    /// <param name="excludedCarriers">Optional excluded carriers.</param>
    /// <param name="testers">Optional tester numbers.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The new agent.</returns>
    /// <exception cref="ValidationException">brandId is null.</exception>
    public async Task<SignalHouseResponse> CreateAsync(
        string brandId,
        object? autofill,
        string? campaignId = null,
        object? agent = null,
        object? consent = null,
        object? contact = null,
        string? useCase = null,
        string? billingCategory = null,
        object? defaultFallback = null,
        List<string>? excludedCarriers = null,
        List<string>? testers = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var payload = RcsAgentRequests.Body(brandId, campaignId, false, agent, consent, contact, useCase, billingCategory, defaultFallback, excludedCarriers, testers, autofill: autofill);
        return await _client.RequestAsync("/rcs/agent", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// List agents, newest first, as { data, total, page, limit }.
    /// </summary>
    /// <param name="groupId">Optional group ID filter.</param>
    /// <param name="subgroupId">Optional subgroup ID filter.</param>
    /// <param name="status">Optional status or comma-separated list from DRAFT, PENDING_REVIEW, REJECTED, AGENT_CREATING, ARCHIVE_PENDING and ARCHIVED.</param>
    /// <param name="brandId">Optional: agents linked to this brand (carrier brand ID, provisional UUID or _id).</param>
    /// <param name="campaignId">Optional: agents linked to this campaign (campaign ID or _id).</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Agents per page (default 25, max 100).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>A page of agents.</returns>
    public async Task<SignalHouseResponse> ListAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? status = null,
        string? brandId = null,
        string? campaignId = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "status", status },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });
        return await _client.RequestAsync($"/rcs/agent{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get one agent with the live state of its brand and campaign links.
    /// </summary>
    /// <param name="agentId">The agent ID (e.g. RCSA7K2M1QZ8RP).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent; googleAgentId is Google's id for it, null until Google has created the agent.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> GetAsync(
        string agentId,
        RequestOptions? options = null)
    {
        return await _client.RequestAsync(RcsAgentRequests.AgentPath(agentId), HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Edit an agent while it may change: a draft, a Signal House rejection, a request for changes, or an agent in
    /// testing (the change is pushed to the carrier partner). agent, consent and contact merge field by field; every
    /// other key replaces. Null arguments are left out of the body. 409 in any other status; brandId is locked once the
    /// agent exists at the partner and billingCategory once a launch was requested. After a carrier rejected a launch
    /// other carriers took (PARTIALLY_LAUNCHED with a REJECTED carrier), only consent and contact may change, and nothing
    /// is pushed: send them with RequestCarrierUpdateAsync. While a carrier update is REQUESTED, edits return 409; editing
    /// opens again after staff mark it SENT.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="brandId">Optional: relink to another brand of the same subgroup.</param>
    /// <param name="campaignId">Optional: link or relink the campaign.</param>
    /// <param name="unlinkCampaign">Send campaignId: null to unlink the campaign (overrides campaignId).</param>
    /// <param name="agent">Optional profile fields to change.</param>
    /// <param name="consent">Optional consent fields to change.</param>
    /// <param name="contact">Optional launch contact fields to change.</param>
    /// <param name="useCase">Optional use case.</param>
    /// <param name="billingCategory">Optional billing category.</param>
    /// <param name="defaultFallback">Optional fallback { mode, channel, from }.</param>
    /// <param name="excludedCarriers">Optional [] or ["T_MOBILE"] (the carrier partner's carrier name).</param>
    /// <param name="testers">Optional tester numbers.</param>
    /// <param name="clearUseCase">Send useCase: null to clear the use case (overrides useCase).</param>
    /// <param name="clearDefaultFallback">Send defaultFallback: null to remove the fallback (overrides defaultFallback).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public Task<SignalHouseResponse> UpdateAsync(
        string agentId,
        string? brandId = null,
        string? campaignId = null,
        bool unlinkCampaign = false,
        object? agent = null,
        object? consent = null,
        object? contact = null,
        string? useCase = null,
        string? billingCategory = null,
        object? defaultFallback = null,
        List<string>? excludedCarriers = null,
        List<string>? testers = null,
        bool clearUseCase = false,
        bool clearDefaultFallback = false,
        RequestOptions? options = null)
    {
        return UpdateAsync(agentId, autofill: null, brandId: brandId, campaignId: campaignId, unlinkCampaign: unlinkCampaign,
            agent: agent, consent: consent, contact: contact, useCase: useCase, billingCategory: billingCategory,
            defaultFallback: defaultFallback, excludedCarriers: excludedCarriers, testers: testers, clearUseCase: clearUseCase,
            clearDefaultFallback: clearDefaultFallback, options: options);
    }

    /// <summary>
    /// Edit an agent and replace its prefill markers. While a carrier update is REQUESTED, edits return 409; editing
    /// opens again after staff mark it SENT.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="autofill">Optional: replaces the prefill markers; omitted markers are kept and changed fields turn edited.</param>
    /// <param name="brandId">Optional: relink to another brand of the same subgroup.</param>
    /// <param name="campaignId">Optional: link or relink the campaign.</param>
    /// <param name="unlinkCampaign">Send campaignId: null to unlink the campaign (overrides campaignId).</param>
    /// <param name="agent">Optional profile fields to change.</param>
    /// <param name="consent">Optional consent fields to change.</param>
    /// <param name="contact">Optional launch contact fields to change.</param>
    /// <param name="useCase">Optional use case.</param>
    /// <param name="billingCategory">Optional billing category.</param>
    /// <param name="defaultFallback">Optional fallback { mode, channel, from }.</param>
    /// <param name="excludedCarriers">Optional [] or ["T_MOBILE"] (the carrier partner's carrier name).</param>
    /// <param name="testers">Optional tester numbers.</param>
    /// <param name="clearUseCase">Send useCase: null to clear the use case (overrides useCase).</param>
    /// <param name="clearDefaultFallback">Send defaultFallback: null to remove the fallback (overrides defaultFallback).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> UpdateAsync(
        string agentId,
        object? autofill,
        string? brandId = null,
        string? campaignId = null,
        bool unlinkCampaign = false,
        object? agent = null,
        object? consent = null,
        object? contact = null,
        string? useCase = null,
        string? billingCategory = null,
        object? defaultFallback = null,
        List<string>? excludedCarriers = null,
        List<string>? testers = null,
        bool clearUseCase = false,
        bool clearDefaultFallback = false,
        RequestOptions? options = null)
    {
        var path = RcsAgentRequests.AgentPath(agentId);
        var payload = RcsAgentRequests.Body(brandId, campaignId, unlinkCampaign, agent, consent, contact, useCase, billingCategory, defaultFallback, excludedCarriers, testers,
            clearUseCase, clearDefaultFallback, autofill);
        return await _client.RequestAsync(path, HttpMethod.Put, payload, options);
    }

    /// <summary>
    /// Check an agent against every submit requirement without writing. Returns { valid, errors: [{ field, reason, code? }],
    /// warnings: [{ field, reason }] }; a copy-rule error at the launch gate carries the carrier partner's key as code
    /// (for example OPT_IN_MSG_RATE_DISCL_MISSING).
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> ValidateAsync(
        string agentId,
        RequestOptions? options = null)
    {
        return await _client.RequestAsync($"{RcsAgentRequests.AgentPath(agentId)}/validate", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Submit a draft: create the agent at the carrier partner, with no review first (status AGENT_CREATING, then
    /// IN_TESTING once the partner reports it ready). It needs the full profile, a useCase and a
    /// billingCategory; otherwise a 400 whose details carry { code: "REGISTRATION_INCOMPLETE", errors, warnings }.
    /// </summary>
    /// <remarks>A group that has not finished account verification gets a 403 with code GROUP_UNVERIFIED, and a rejected
    /// one a 403 with code GROUP_REJECTED; a pending verification passes.</remarks>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> SubmitAsync(
        string agentId,
        RequestOptions? options = null)
    {
        return await _client.RequestAsync($"{RcsAgentRequests.AgentPath(agentId)}/submit", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Request the agent's launch: the launch requirements are checked and the agent goes to Signal House review (status
    /// PENDING_REVIEW), whose approval files the launch with the carrier partner. Allowed from IN_TESTING, REJECTED and
    /// CARRIER_REJECTED (409 otherwise); a failed requirement is a 400 whose details carry
    /// { code: "REGISTRATION_INCOMPLETE", errors, warnings }, and a copy-rule error carries the carrier partner's key as its
    /// code (for example OPT_IN_MSG_RATE_DISCL_MISSING). A group that has not finished account verification gets a 403
    /// with code GROUP_UNVERIFIED, and a rejected one a 403 with code GROUP_REJECTED. Sends POST
    /// /registration/:agentId/launch: the agent is a registration.
    /// The request charges the agent's launch fees: a wallet that cannot cover them gets a 402, and the agent stays where it is. When any fee is due the request must set acknowledgeFees, or it gets a 400 with code FEE_ACKNOWLEDGEMENT_REQUIRED and nothing is held; a launch with nothing due needs no acknowledgement. The agent's launchFees quotes what the next request charges.
    /// When the agent's brand vertical is healthcare or insurance, the launch needs the protected health information (PHI) disclosure acknowledged: set acknowledgePhi, or the request gets a 400 REGISTRATION_INCOMPLETE whose errors name consent.phiAcknowledgement. The API records the acknowledgement as consent.phiAcknowledgement { acknowledgedAt, acknowledgedBy } from the caller; the client sends only the boolean, and an agent update cannot set it. Once recorded, a later launch request needs no new acknowledgement.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <param name="acknowledgeFees">Accept the fees due; required when any are.</param>
    /// <param name="acknowledgePhi">Accept the PHI disclosure; required for a healthcare or insurance brand.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> LaunchAsync(
        string agentId,
        RequestOptions? options = null,
        bool? acknowledgeFees = null,
        bool? acknowledgePhi = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId } });
        var body = RegistrationsDomain.LaunchBody(acknowledgeFees, acknowledgePhi);
        return await _client.RequestAsync($"/registration/{Uri.EscapeDataString(agentId)}/launch", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Send Signal House the corrected launch answers after a carrier rejected a launch other carriers took. The carrier
    /// partner takes no change through its API once a carrier launched, so Signal House emails it the update; edit
    /// consent and contact first. The agent's carrierUpdate reads REQUESTED for the rejected carriers, then SENT once
    /// Signal House sent it. 409 when no carrier is REJECTED after a partial launch, or an update already waits. Sends
    /// POST /registration/:agentId/carrier-update: the agent is a registration.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent's registration, with data.carrierUpdate.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> RequestCarrierUpdateAsync(
        string agentId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId } });
        return await _client.RequestAsync($"/registration/{Uri.EscapeDataString(agentId)}/carrier-update", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Archive an agent (status ARCHIVE_PENDING or ARCHIVED). One the carrier partner created stops sending at once and
    /// stays ARCHIVE_PENDING until Signal House confirms the partner cancelled it, then ARCHIVED. Archiving cannot be
    /// undone; an agent already ARCHIVE_PENDING answers 409.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> ArchiveAsync(
        string agentId,
        RequestOptions? options = null)
    {
        return await _client.RequestAsync($"{RcsAgentRequests.AgentPath(agentId)}/archive", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Make the links that start a chat with the agent: an sms: deep link in Google's format, and a short link that
    /// redirects to it, for a QR code. With the agent's defaultFallback.from, the deep link names that number, so a
    /// handset that cannot reach the agent texts it. The same request returns the same short link. The links reach
    /// approved testers until a carrier launches the agent. Before Google has created the agent the request answers 409
    /// with code GOOGLE_AGENT_PENDING.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="body">Optional message the chat opens with, prefilled on the handset (max 1,000 characters).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ googleAgentId, deepLink, shortLink }.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> CreateEntryPointsAsync(
        string agentId,
        string? body = null,
        RequestOptions? options = null)
    {
        var path = RcsAgentRequests.AgentPath(agentId);
        var payload = body is null ? null : new Dictionary<string, object?> { { "body", body } };
        return await _client.RequestAsync($"{path}/entry-points", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// Resend a tester's expired invitation, which starts a new 24 hours for the tester to accept it. The number must be
    /// a tester on the agent (404) and its invitation must be EXPIRED (409); up to 20 invitations go out a day (429).
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="phoneNumber">The tester's US number, with or without the +.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId or phoneNumber is null.</exception>
    public async Task<SignalHouseResponse> ResendTesterInviteAsync(
        string agentId,
        string phoneNumber,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber } });
        return await _client.RequestAsync($"{RcsAgentRequests.AgentPath(agentId)}/testers/{Uri.EscapeDataString(phoneNumber)}/resend", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Remove a tester at once, at any point before the agent is archived. The number must be a tester on the agent
    /// (404); an archived agent answers 409.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="phoneNumber">The tester's US number, with or without the +.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId or phoneNumber is null.</exception>
    public async Task<SignalHouseResponse> RemoveTesterAsync(
        string agentId,
        string phoneNumber,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "phoneNumber", phoneNumber } });
        return await _client.RequestAsync($"{RcsAgentRequests.AgentPath(agentId)}/testers/{Uri.EscapeDataString(phoneNumber)}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Host the agent's logo (224 × 224 px, 50 KB max) or hero image (1440 × 448 px, 200 KB max), PNG or JPEG, and set
    /// it on the draft (multipart fields kind + file). Images of any other size are refused.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="kind">"logo" or "hero".</param>
    /// <param name="file">The image bytes.</param>
    /// <param name="fileName">Optional file name sent with the upload.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent with the new URL.</returns>
    /// <exception cref="ValidationException">agentId, kind or file is null.</exception>
    public async Task<SignalHouseResponse> UploadAssetAsync(
        string agentId,
        string kind,
        Stream file,
        string? fileName = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId }, { "kind", kind }, { "file", file } });

        var content = RcsAgentRequests.AssetContent(kind, file, fileName);
        return await _client.MultipartRequestAsync($"{RcsAgentRequests.AgentPath(agentId)}/asset", HttpMethod.Post, content, options);
    }
}

/// <summary>
/// RCS messages. Sends go through an agent to POST /message/rcs. A message is a Message record (messageType RCS), so
/// the Messages methods read the same record. 404 while the environment's RCS switch is off; 403 while the group's
/// rcsEnabled flag is off; 409 while the agent cannot send yet.
/// </summary>
public sealed class RcsMessagesDomain
{
    private readonly HttpClientWrapper _client;

    internal RcsMessagesDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Send an RCS message to up to 100 recipients (HTTP 201). A check that blocks one recipient writes a BLOCKED
    /// message for it and the rest still send. Set RequestOptions.IdempotencyKey to make a retry safe.
    /// </summary>
    /// <param name="agentId">The agent that sends (RCSA…).</param>
    /// <param name="recipients">Up to 100 recipients, each { phoneNumber, variables?, clientMessageId?, metadata? } (a list of Dictionary, JsonElement or JsonNode).</param>
    /// <param name="templateId">A template to send; exactly one of templateId or content.</param>
    /// <param name="templateVersion">A published version of templateId; needs templateId.</param>
    /// <param name="content">Inline content (a Dictionary, JsonElement or JsonNode); exactly one of templateId or content.</param>
    /// <param name="contentFormat">content.v1 (default) or google.rcs.</param>
    /// <param name="trafficType">AUTHENTICATION, TRANSACTION, SERVICEREQUEST, ACKNOWLEDGEMENT or PROMOTION; required on a MULTI_USE agent when the template sets none.</param>
    /// <param name="ttlSeconds">Seconds the RCS message may wait for delivery; at most one of ttlSeconds or expireTime.</param>
    /// <param name="expireTime">ISO-8601 time the RCS message expires.</param>
    /// <param name="fallback">{ mode? (auto, rcs_only, sms_only), channel? (auto, sms, mms), from?, content?: { text, mediaUrls? }, validitySeconds? }. from is a Ready number on the agent's linked Active campaign. If content is omitted the template's fallback, else a generated one, is used.</param>
    /// <param name="statusCallbackUrl">URL that receives this message's status events.</param>
    /// <param name="enableShortlink">Shorten the links in the message.</param>
    /// <param name="isTest">Send as a test; approved testers only.</param>
    /// <param name="simulate">not_capable, ttl_expired, delivered or read; approved testers only.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ requested, enqueued, failed, bulkId, messages }. A blocked row carries blockReason, reason and issues; an archived template answers 409 TEMPLATE_ARCHIVED. A recipient from 19999990000 to 19999990359 makes the whole request a simulation, as on SMS: it returns { simulated: true, webhooks } and nothing is stored, sent or charged.</returns>
    /// <exception cref="ValidationException">agentId or recipients is null.</exception>
    public async Task<SignalHouseResponse> SendAsync(
        string agentId,
        object recipients,
        string? templateId = null,
        int? templateVersion = null,
        object? content = null,
        string? contentFormat = null,
        string? trafficType = null,
        int? ttlSeconds = null,
        string? expireTime = null,
        object? fallback = null,
        string? statusCallbackUrl = null,
        bool? enableShortlink = null,
        bool? isTest = null,
        string? simulate = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId }, { "recipients", recipients } });

        var payload = RcsMessageRequests.Body(new Dictionary<string, object?>
        {
            { "agentId", agentId },
            { "recipients", recipients },
            { "templateId", templateId },
            { "templateVersion", templateVersion },
            { "content", content },
            { "contentFormat", contentFormat },
            { "trafficType", trafficType },
            { "ttlSeconds", ttlSeconds },
            { "expireTime", expireTime },
            { "fallback", fallback },
            { "statusCallbackUrl", statusCallbackUrl },
            { "enableShortlink", enableShortlink },
            { "isTest", isTest },
            { "simulate", simulate },
        });
        return await _client.RequestAsync("/message/rcs", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// Send an RCS message to up to 10,000 recipients in the background (HTTP 202). The request runs the send's request
    /// checks, splits the recipients into chunks of 100 and queues each chunk. Every message carries the shared bulkId;
    /// track progress with the messages list filtered by bulkId (GET /message?bulkId=) and per-message webhooks. A chunk
    /// the wallet cannot cover, or that is refused after acceptance, gets BLOCKED messages with blockReason
    /// INSUFFICIENT_FUNDS or SEND_REFUSED; a chunk that keeps failing gets SEND_FAILED. Set RequestOptions.IdempotencyKey
    /// to make a retry safe (same bulkId, no duplicate sends); a retry without one may send duplicates.
    /// </summary>
    /// <param name="agentId">The agent that sends (RCSA…).</param>
    /// <param name="recipients">Up to 10,000 recipients, each { phoneNumber, variables?, clientMessageId?, metadata? } (a list of Dictionary, JsonElement or JsonNode).</param>
    /// <param name="templateId">A template to send; exactly one of templateId or content.</param>
    /// <param name="templateVersion">A published version of templateId; needs templateId. Pinned when the batch is accepted.</param>
    /// <param name="content">Inline content (a Dictionary, JsonElement or JsonNode); exactly one of templateId or content.</param>
    /// <param name="contentFormat">content.v1 (default) or google.rcs.</param>
    /// <param name="trafficType">AUTHENTICATION, TRANSACTION, SERVICEREQUEST, ACKNOWLEDGEMENT or PROMOTION; required on a MULTI_USE agent when the template sets none.</param>
    /// <param name="ttlSeconds">Seconds each RCS message may wait for delivery, from when its chunk is sent; at most one of ttlSeconds or expireTime.</param>
    /// <param name="expireTime">ISO-8601 time the RCS messages expire.</param>
    /// <param name="fallback">As on SendAsync.</param>
    /// <param name="statusCallbackUrl">URL that receives each message's status events.</param>
    /// <param name="enableShortlink">Shorten the links in the message.</param>
    /// <param name="isTest">Send as a test; approved testers only.</param>
    /// <param name="simulate">not_capable, ttl_expired, delivered or read; approved testers only.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ bulkId, status: "ACCEPTED", recipientCount }. A recipient from 19999990000 to 19999990359 makes the whole request a simulation, as on SendAsync.</returns>
    /// <exception cref="ValidationException">agentId or recipients is null.</exception>
    public async Task<SignalHouseResponse> SendBatchAsync(
        string agentId,
        object recipients,
        string? templateId = null,
        int? templateVersion = null,
        object? content = null,
        string? contentFormat = null,
        string? trafficType = null,
        int? ttlSeconds = null,
        string? expireTime = null,
        object? fallback = null,
        string? statusCallbackUrl = null,
        bool? enableShortlink = null,
        bool? isTest = null,
        string? simulate = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId }, { "recipients", recipients } });

        var payload = RcsMessageRequests.Body(new Dictionary<string, object?>
        {
            { "agentId", agentId },
            { "recipients", recipients },
            { "templateId", templateId },
            { "templateVersion", templateVersion },
            { "content", content },
            { "contentFormat", contentFormat },
            { "trafficType", trafficType },
            { "ttlSeconds", ttlSeconds },
            { "expireTime", expireTime },
            { "fallback", fallback },
            { "statusCallbackUrl", statusCallbackUrl },
            { "enableShortlink", enableShortlink },
            { "isTest", isTest },
            { "simulate", simulate },
        });
        return await _client.RequestAsync("/message/rcs/batch", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// Check a send without writing anything. Takes the same arguments as SendAsync.
    /// </summary>
    /// <param name="agentId">The agent that sends (RCSA…).</param>
    /// <param name="recipients">Up to 100 recipients, each { phoneNumber, variables?, clientMessageId?, metadata? } (a list of Dictionary, JsonElement or JsonNode).</param>
    /// <param name="templateId">A template to send; exactly one of templateId or content.</param>
    /// <param name="templateVersion">A published version of templateId; needs templateId.</param>
    /// <param name="content">Inline content (a Dictionary, JsonElement or JsonNode); exactly one of templateId or content.</param>
    /// <param name="contentFormat">content.v1 (default) or google.rcs.</param>
    /// <param name="trafficType">AUTHENTICATION, TRANSACTION, SERVICEREQUEST, ACKNOWLEDGEMENT or PROMOTION; required on a MULTI_USE agent when the template sets none.</param>
    /// <param name="ttlSeconds">Seconds the RCS message may wait for delivery; at most one of ttlSeconds or expireTime.</param>
    /// <param name="expireTime">ISO-8601 time the RCS message expires.</param>
    /// <param name="fallback">{ mode? (auto, rcs_only, sms_only), channel? (auto, sms, mms), from?, content?: { text, mediaUrls? }, validitySeconds? }. from is a Ready number on the agent's linked Active campaign. If content is omitted the template's fallback, else a generated one, is used.</param>
    /// <param name="statusCallbackUrl">URL that receives this message's status events.</param>
    /// <param name="enableShortlink">Shorten the links in the message.</param>
    /// <param name="isTest">Send as a test; approved testers only.</param>
    /// <param name="simulate">not_capable, ttl_expired, delivered or read; approved testers only.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ valid, errors, warnings, billingClass, segments, estimatedCostMicros, unsupportedFiles, recipients }. valid is true only with no errors; each blocked recipient adds an error and carries blockReason, reason and issues.</returns>
    /// <exception cref="ValidationException">agentId or recipients is null.</exception>
    public async Task<SignalHouseResponse> ValidateAsync(
        string agentId,
        object recipients,
        string? templateId = null,
        int? templateVersion = null,
        object? content = null,
        string? contentFormat = null,
        string? trafficType = null,
        int? ttlSeconds = null,
        string? expireTime = null,
        object? fallback = null,
        string? statusCallbackUrl = null,
        bool? enableShortlink = null,
        bool? isTest = null,
        string? simulate = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId }, { "recipients", recipients } });

        var payload = RcsMessageRequests.Body(new Dictionary<string, object?>
        {
            { "agentId", agentId },
            { "recipients", recipients },
            { "templateId", templateId },
            { "templateVersion", templateVersion },
            { "content", content },
            { "contentFormat", contentFormat },
            { "trafficType", trafficType },
            { "ttlSeconds", ttlSeconds },
            { "expireTime", expireTime },
            { "fallback", fallback },
            { "statusCallbackUrl", statusCallbackUrl },
            { "enableShortlink", enableShortlink },
            { "isTest", isTest },
            { "simulate", simulate },
        });
        return await _client.RequestAsync("/message/rcs/validate", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// Price an RCS draft in microdollars; no charge or send. Sends POST /message/estimate with channel "rcs".
    /// </summary>
    /// <param name="agentId">The agent that would send (RCSA…).</param>
    /// <param name="recipients">Named recipients; exactly one of recipients or recipientCount.</param>
    /// <param name="recipientCount">A recipient count; carriers are unknown, so the default fees apply.</param>
    /// <param name="templateId">A template to price; exactly one of templateId or content.</param>
    /// <param name="templateVersion">A published version of templateId; needs templateId.</param>
    /// <param name="content">Inline content; exactly one of templateId or content.</param>
    /// <param name="contentFormat">content.v1 (default) or google.rcs.</param>
    /// <param name="trafficType">As on SendAsync.</param>
    /// <param name="ttlSeconds">Seconds the RCS message may wait for delivery; at most one of ttlSeconds or expireTime.</param>
    /// <param name="expireTime">ISO-8601 time the RCS message expires.</param>
    /// <param name="fallback">As on SendAsync, but its from is optional here; with none, the leg is priced on the channel of the agent's linked campaign.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ billingClass, segments, recipientCount, rcsLeg, fallbackLeg, totals, balance, issues, warnings }; rcsLeg.mediaSurcharge is the carrier's surcharge on media over 5 MB for one recipient, and rcsLeg.cost already holds it for every recipient; rcsLeg.cost is null with the fallback mode sms_only; a message over 100 MB adds a MEDIA_TOO_LARGE issue.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> EstimateAsync(
        string agentId,
        object? recipients = null,
        int? recipientCount = null,
        string? templateId = null,
        int? templateVersion = null,
        object? content = null,
        string? contentFormat = null,
        string? trafficType = null,
        int? ttlSeconds = null,
        string? expireTime = null,
        object? fallback = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId } });

        var payload = RcsMessageRequests.Body(new Dictionary<string, object?>
        {
            { "channel", "rcs" },
            { "agentId", agentId },
            { "recipients", recipients },
            { "recipientCount", recipientCount },
            { "templateId", templateId },
            { "templateVersion", templateVersion },
            { "content", content },
            { "contentFormat", contentFormat },
            { "trafficType", trafficType },
            { "ttlSeconds", ttlSeconds },
            { "expireTime", expireTime },
            { "fallback", fallback },
        });
        return await _client.RequestAsync("/message/estimate", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// Report which numbers can receive RCS from an agent.
    /// </summary>
    /// <param name="agentId">The agent (RCSA…).</param>
    /// <param name="phoneNumbers">Up to 500 numbers, 10-15 digits, with or without the +.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ results: [{ phoneNumber, capability, rcsEnabled, reason, features, carrier, checkedAt, cached }] }, capability being ENABLED, UNREACHABLE or UNKNOWN, and reason CAPABLE, NOT_CAPABLE, NOT_LAUNCHED_ON_CARRIER or UNKNOWN; rcsEnabled is true only for CAPABLE, carrier and checkedAt may be null, and features is empty today.</returns>
    /// <exception cref="ValidationException">agentId or phoneNumbers is null.</exception>
    public async Task<SignalHouseResponse> CapabilitiesAsync(
        string agentId,
        IEnumerable<string> phoneNumbers,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId }, { "phoneNumbers", phoneNumbers } });
        return await _client.RequestAsync("/rcs/capabilities", HttpMethod.Post, new { agentId, phoneNumbers }, options);
    }

    /// <summary>
    /// Fake an inbound event from an approved tester of the agent, with no carrier involved: it runs the real inbound handlers and webhooks, as free test traffic. A simulated STOP really opts the tester out; a simulated START clears it. Anyone else gets a 403 with code NOT_TESTER. Returns 202 with { status, messageId }.
    /// </summary>
    /// <param name="agentId">The agent (RCSA…).</param>
    /// <param name="from">The approved tester's number.</param>
    /// <param name="type">TEXT, SUGGESTION, LOCATION, FILE or UNSUBSCRIBE.</param>
    /// <param name="text">The text; required for TEXT.</param>
    /// <param name="postbackData">The tapped suggestion's postback; SUGGESTION needs this or buttonId.</param>
    /// <param name="messageId">The message a tap answers; the latest message delivered to the tester by default.</param>
    /// <param name="buttonId">The suggestion's position, as webhooks report it.</param>
    /// <param name="latitude">Required for LOCATION.</param>
    /// <param name="longitude">Required for LOCATION.</param>
    /// <param name="fileName">The file's name, for FILE.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ status: "QUEUED", messageId }.</returns>
    /// <exception cref="ValidationException">agentId, from or type is null.</exception>
    public async Task<SignalHouseResponse> SimulateInboundAsync(
        string agentId,
        string from,
        string type,
        string? text = null,
        string? postbackData = null,
        string? messageId = null,
        string? buttonId = null,
        double? latitude = null,
        double? longitude = null,
        string? fileName = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId }, { "from", from }, { "type", type } });
        return await _client.RequestAsync("/message/rcs/simulate-inbound", HttpMethod.Post, new { agentId, from, type, text, postbackData, messageId, buttonId, latitude, longitude, fileName }, options);
    }

    /// <summary>
    /// Get one message by its ID. The same route as <c>client.Messages.GetMessageAsync</c>.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The message; an unknown ID is a 404. A settled RCS message carries carrierFeeMicros, the carrier pass-through and media surcharge it was charged.</returns>
    /// <exception cref="ValidationException">messageId is null.</exception>
    public async Task<SignalHouseResponse> GetAsync(
        string messageId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "messageId", messageId } });
        return await _client.RequestAsync($"/message/{Uri.EscapeDataString(messageId)}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// List RCS messages. Sends GET /message with messageType RCS. A fallback leg is an SMS or MMS message: read it with
    /// <c>client.Messages.GetMessageAsync</c> and the parent's fallbackMessageId.
    /// </summary>
    /// <param name="groupId">The group whose messages to list.</param>
    /// <param name="from">ISO-8601 date or timestamp; sent as startDate.</param>
    /// <param name="to">ISO-8601 date or timestamp; sent as endDate.</param>
    /// <param name="agentId">Messages this agent sent; sent as registrationId.</param>
    /// <param name="status">ENQUEUED, DEQUEUED, SENT, FAILED, DELIVERED, READ, EXPIRED, FALLBACK or BLOCKED.</param>
    /// <param name="page">Page number.</param>
    /// <param name="limit">Messages per page.</param>
    /// <param name="templateId">Messages sent from this template.</param>
    /// <param name="templateVersion">Messages sent from this template version.</param>
    /// <param name="bulkId">The messages of one send.</param>
    /// <param name="fallbackReason">NOT_CAPABLE, CARRIER_NOT_LAUNCHED, TTL_EXPIRED, RCS_ERROR, PROVIDER_ERROR or SMS_ONLY.</param>
    /// <param name="billingClass">RICH or RICH_MEDIA.</param>
    /// <param name="deliveredChannel">The channel that delivered the message: rcs, sms or mms.</param>
    /// <param name="isTest">true for test messages only, false to leave them out.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The messages; a settled RCS message carries carrierFeeMicros.</returns>
    /// <exception cref="ValidationException">groupId, from or to is null.</exception>
    public async Task<SignalHouseResponse> ListAsync(
        string groupId,
        string from,
        string to,
        string? agentId = null,
        string? status = null,
        int? page = null,
        int? limit = null,
        string? templateId = null,
        int? templateVersion = null,
        string? bulkId = null,
        string? fallbackReason = null,
        string? billingClass = null,
        string? deliveredChannel = null,
        bool? isTest = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "from", from }, { "to", to } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "registrationId", agentId },
            { "startDate", from },
            { "endDate", to },
            { "groupId", groupId },
            { "status", status },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "templateId", templateId },
            { "templateVersion", templateVersion?.ToString() },
            { "bulkId", bulkId },
            { "fallbackReason", fallbackReason },
            { "billingClass", billingClass },
            { "deliveredChannel", deliveredChannel },
            { "isTest", isTest?.ToString().ToLowerInvariant() },
            { "messageType", "RCS" },
            { "dateBounds", "exact" },
        });
        return await _client.RequestAsync($"/message{qs}", HttpMethod.Get, options: options);
    }
}

/// <summary>
/// Request-building helpers for the RCS messages methods.
/// </summary>
internal static class RcsMessageRequests
{
    /// <summary>
    /// Build a JSON body, leaving out the null fields.
    /// </summary>
    internal static Dictionary<string, object?> Body(Dictionary<string, object?> fields)
    {
        return fields.Where(field => field.Value is not null).ToDictionary(field => field.Key, field => field.Value);
    }
}

/// <summary>
/// Request builders for /rcs/agent.
/// </summary>
internal static class RcsAgentRequests
{
    /// <summary>
    /// Build the encoded path of one agent.
    /// </summary>
    /// <exception cref="ValidationException">agentId is null.</exception>
    internal static string AgentPath(string agentId)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId } });
        return $"/rcs/agent/{Uri.EscapeDataString(agentId)}";
    }

    /// <summary>
    /// Build a create or update body; null fields are dropped, and unlinkCampaign, clearUseCase and
    /// clearDefaultFallback send their field as null. autofill holds the prefill markers by field path.
    /// </summary>
    internal static Dictionary<string, object?> Body(
        string? brandId,
        string? campaignId,
        bool unlinkCampaign,
        object? agent,
        object? consent,
        object? contact,
        string? useCase,
        string? billingCategory,
        object? defaultFallback,
        List<string>? excludedCarriers,
        List<string>? testers,
        bool clearUseCase = false,
        bool clearDefaultFallback = false,
        object? autofill = null)
    {
        var payload = new Dictionary<string, object?>();
        void Set(string key, object? value, bool clear = false)
        {
            if (clear)
            {
                payload[key] = null;
            }
            else if (value is not null)
            {
                payload[key] = value;
            }
        }

        Set("brandId", brandId);
        Set("campaignId", campaignId, unlinkCampaign);
        Set("agent", agent);
        Set("consent", consent);
        Set("contact", contact);
        Set("useCase", useCase, clearUseCase);
        Set("billingCategory", billingCategory);
        Set("defaultFallback", defaultFallback, clearDefaultFallback);
        Set("excludedCarriers", excludedCarriers);
        Set("testers", testers);
        Set("autofill", autofill);
        return payload;
    }

    /// <summary>
    /// Build the multipart kind + file upload form. A missing file name is sent as "file" so the server reads the part as a file.
    /// </summary>
    internal static MultipartFormDataContent AssetContent(string kind, Stream file, string? fileName)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(kind), "kind");
        var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", string.IsNullOrWhiteSpace(fileName) ? "file" : fileName);
        return content;
    }
}
