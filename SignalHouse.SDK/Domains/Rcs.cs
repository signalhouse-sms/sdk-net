using System.Net.Http.Headers;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// RCS (Google RBM) resources. Templates for RCS live under <c>client.Templates</c>.
/// <list type="bullet">
///   <item><c>client.Rcs.Agents</c> — RCS agents under /rcs/agent.</item>
/// </list>
/// </summary>
public sealed class RcsDomain
{
    /// <summary>RCS agents under /rcs/agent.</summary>
    public RcsAgentsDomain Agents { get; }

    internal RcsDomain(HttpClientWrapper client)
    {
        Agents = new RcsAgentsDomain(client);
    }
}

/// <summary>
/// RCS agents under /rcs/agent. An agent links one 10DLC brand and one 10DLC campaign: the campaign's numbers are the
/// SMS fallback, and the brand and campaign supply the company identity and consent text for the carrier launch. One
/// brand or campaign can serve many agents. An agent is a registration (type RCS_AGENT, id RCSA…), so the
/// Registrations methods read and edit the same record. 404 while the environment's RCS switch is off; 400 while the
/// group's rcsEnabled flag is off.
/// </summary>
public sealed class RcsAgentsDomain
{
    private readonly HttpClientWrapper _client;

    internal RcsAgentsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Create an agent draft on a 10DLC brand, optionally with its campaign (HTTP 201, status DRAFT). The agent's
    /// subgroup is the brand's. Blank consent fields fill from the linked campaign. Set RequestOptions.IdempotencyKey
    /// to make a retry safe.
    /// </summary>
    /// <param name="brandId">The 10DLC brand: carrier brand ID, provisional UUID or _id.</param>
    /// <param name="campaignId">Optional 10DLC campaign of that brand (campaign ID or _id); required before submit.</param>
    /// <param name="agent">Optional profile { name, description, color, logoUrl, heroUrl, phone, website, email, phoneLabel, websiteLabel, emailLabel (captions, max 40; submit defaults them to Call, Website and Email), privacyPolicyUrl, termsUrl } (a Dictionary, JsonElement or JsonNode).</param>
    /// <param name="consent">Optional launch text { optInDetails, optInUrl, optInResponse, optOutDetails, optOutResponse, helpResponse, campaignDescription, messageExamples, estimatedMonthlyVolume, launchVideoUrl }; every field is required before the launch request.</param>
    /// <param name="contact">Optional launch contact { firstName, lastName, role, email, mobile }: the individual at the brand the carrier partner's reviewers may write to; required in full before the launch request.</param>
    /// <param name="useCase">Optional OTP, TRANSACTIONAL, PROMOTIONAL or MULTI_USE; required before submit.</param>
    /// <param name="billingCategory">Optional NON_CONVERSATIONAL (default) or CONVERSATIONAL; submit refuses CONVERSATIONAL.</param>
    /// <param name="defaultFallback">Optional { mode (auto, rcs_only, sms_only, none), channel (auto, sms, mms), from (a number on the linked campaign) }.</param>
    /// <param name="excludedCarriers">Optional [] or ["T_MOBILE"] (the carrier partner's carrier name).</param>
    /// <param name="testers">Optional tester numbers (up to 200), invited once the agent exists at the carrier.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The new agent.</returns>
    /// <exception cref="ValidationException">brandId is null.</exception>
    public async Task<SignalHouseResponse> CreateAsync(
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
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var payload = RcsAgentRequests.Body(brandId, campaignId, false, agent, consent, contact, useCase, billingCategory, defaultFallback, excludedCarriers, testers);
        return await _client.RequestAsync("/rcs/agent", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// List agents, newest first, as { data, total, page, limit }.
    /// </summary>
    /// <param name="groupId">Optional group ID filter.</param>
    /// <param name="subgroupId">Optional subgroup ID filter.</param>
    /// <param name="status">Optional status or comma-separated list from DRAFT, PENDING_REVIEW, REJECTED, AGENT_CREATING and ARCHIVED.</param>
    /// <param name="brandId">Optional: agents linked to this 10DLC brand (carrier brand ID, provisional UUID or _id).</param>
    /// <param name="campaignId">Optional: agents linked to this 10DLC campaign (campaign ID or _id).</param>
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
    /// <returns>The agent.</returns>
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
    /// agent exists at the partner and billingCategory once a launch was requested.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="brandId">Optional: relink to another 10DLC brand of the same subgroup.</param>
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
            clearUseCase, clearDefaultFallback);
        return await _client.RequestAsync(path, HttpMethod.Put, payload, options);
    }

    /// <summary>
    /// Check an agent against every submit requirement without writing. Returns { valid, errors: [{ field, reason }],
    /// warnings: [{ field, reason }] }.
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
    /// IN_TESTING once the partner reports it ready). It needs the full profile, a useCase and NON_CONVERSATIONAL
    /// billing; otherwise a 400 whose details carry { code: "REGISTRATION_INCOMPLETE", errors, warnings }.
    /// </summary>
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
    /// { code: "REGISTRATION_INCOMPLETE", errors, warnings }. Sends POST /registration/:agentId/launch: the agent is a
    /// registration.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The agent.</returns>
    /// <exception cref="ValidationException">agentId is null.</exception>
    public async Task<SignalHouseResponse> LaunchAsync(
        string agentId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentId", agentId } });
        return await _client.RequestAsync($"/registration/{Uri.EscapeDataString(agentId)}/launch", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Archive an agent (status ARCHIVED).
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
    /// clearDefaultFallback send their field as null.
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
        bool clearDefaultFallback = false)
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
