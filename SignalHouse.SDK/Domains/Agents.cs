using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// AI Agents domain — customer-facing agent configuration (AgentProfile CRUD)
/// served under the <c>/agent</c> prefix.
///
/// Profile data is supplied as a dictionary whose keys map directly to the API's
/// camelCase JSON fields. Create requires <c>groupId</c> (starts with 'G') and
/// <c>name</c>; <c>subgroupId</c> (starts with 'S') is optional/nullable
/// (null/omitted = group-level agent). Other fields: <c>status</c>
/// ("active" | "inactive"), <c>systemPrompt</c>, <c>greeting</c>, <c>guardrails</c>,
/// Model, voice and sampling settings are per-channel and live on the channel
/// setting, not here. Update
/// accepts the same keys except the scope fields <c>groupId</c>/<c>subgroupId</c>,
/// which are immutable.
///
/// Accessed via <c>client.Agents</c>.
/// </summary>
public sealed class AgentsDomain
{
    private readonly HttpClientWrapper _client;

    internal AgentsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// List the voices an agent can be configured to speak with.
    /// The catalog is platform-wide, not per-account, so this takes no scope; the
    /// <c>voiceId</c> of an entry is the value to set on a spoken channel's setting.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/voices</c>.
    /// </summary>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentVoicesAsync(RequestOptions? options = null)
    {
        return await _client.RequestAsync("/agent/voices", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// List the agent profiles under a group.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/profiles</c>.
    /// </summary>
    /// <param name="groupId">The group whose profiles to list (required).</param>
    /// <param name="subgroupId">Narrow to the agents this subgroup can use — its own, plus the group-level agents shared with every subgroup.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="limit">The number of items per page for pagination.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentProfilesAsync(
        string groupId,
        string? subgroupId = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/agent/profiles{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single agent profile by id.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/profiles/:agentProfileId</c>.
    /// </summary>
    /// <param name="agentProfileId">The id of the agent profile to fetch.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentProfileAsync(string agentProfileId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        return await _client.RequestAsync($"/agent/profiles/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new agent profile. <paramref name="profileData"/> must include the
    /// required fields <c>groupId</c> and <c>name</c>. Optional <c>sendAuthority</c>
    /// ("review" or "autopilot", defaults to "review") sets the default send authority,
    /// which deployments may override per subgroup. <c>publishStatus</c> and
    /// <c>publishedAt</c> are read-only and change only through
    /// <see cref="PublishAgentProfileAsync"/> and <see cref="UnpublishAgentProfileAsync"/>.
    /// Allowed roles: api, admin, developer.
    /// <c>POST /agent/profiles</c>.
    /// </summary>
    /// <param name="profileData">The data for the agent profile to create.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> CreateAgentProfileAsync(
        IDictionary<string, object?> profileData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "profileData", profileData } });

        return await _client.RequestAsync("/agent/profiles", HttpMethod.Post, profileData, options);
    }

    /// <summary>
    /// Update an existing agent profile. All fields are optional, but the scope
    /// fields <c>groupId</c>/<c>subgroupId</c> are immutable and cannot be updated.
    /// <c>sendAuthority</c> ("review" or "autopilot") is updatable; <c>publishStatus</c> and
    /// <c>publishedAt</c> are read-only.
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /agent/profiles/:id</c>.
    /// </summary>
    /// <param name="id">The id of the agent profile to update.</param>
    /// <param name="updateData">The data for the agent profile to update.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UpdateAgentProfileAsync(
        string id,
        IDictionary<string, object?> updateData,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/agent/profiles/{safeId}", HttpMethod.Put, updateData, options);
    }

    /// <summary>
    /// Permanently delete an agent profile by its id.
    /// Permanently deletes the agent, its channel settings, and any tools and knowledge scoped to it, plus their provider-side counterparts. Tools and knowledge shared across the account are untouched. Conversation records survive — a transcript outlives the agent that produced it.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /agent/profiles/:id</c>.
    /// </summary>
    /// <param name="id">The id of the agent profile to delete.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> DeleteAgentProfileAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/agent/profiles/{safeId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// Send a message to an agent and get its reply — the app-native conversation
    /// endpoint (webchat / SMS). Omit <paramref name="conversationId"/> to start a new
    /// conversation, or pass one to continue an existing one. The agent's LLM tool loop
    /// runs server-side; the response carries <c>conversationId</c>,
    /// <c>conversationSessionId</c>, <c>reply</c>, <c>toolInvocations</c>, and <c>ok</c>.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/messages</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent to talk to (required).</param>
    /// <param name="channel">The channel: "webchat", "sms", or "voice" (required).</param>
    /// <param name="message">The user's message (required).</param>
    /// <param name="conversationId">Continue this conversation; omit to start a new one.</param>
    /// <param name="contactIdentifier">The far-end identifier (webchat visitor id, sender number).</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> SendAgentMessageAsync(
        string agentProfileId,
        string channel,
        string message,
        string? conversationId = null,
        string? contactIdentifier = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "agentProfileId", agentProfileId },
            { "channel", channel },
            { "message", message },
        });

        var body = new Dictionary<string, object?>
        {
            { "agentProfileId", agentProfileId },
            { "channel", channel },
            { "message", message },
        };
        if (conversationId is not null)
        {
            body["conversationId"] = conversationId;
        }
        if (contactIdentifier is not null)
        {
            body["contactIdentifier"] = contactIdentifier;
        }

        return await _client.RequestAsync("/agent/messages", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Start a conversation without sending a message. The counterpart to
    /// <see cref="SendAgentMessageAsync"/> for callers that run the model themselves — a
    /// voice runtime, or your own LLM. Those record what was said; SendAgentMessage decides
    /// it. Both write the same conversation records.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/conversations</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent this conversation belongs to.</param>
    /// <param name="channel">The channel: "webchat", "sms", or "voice".</param>
    /// <param name="contactIdentifier">The far-end identifier (visitor id, caller number).</param>
    /// <param name="callId">Telephony call id, for the voice channel.</param>
    /// <param name="metadata">Arbitrary metadata stored with the conversation.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> StartConversationAsync(
        string agentProfileId,
        string channel,
        string? contactIdentifier = null,
        string? callId = null,
        IDictionary<string, object?>? metadata = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "agentProfileId", agentProfileId },
            { "channel", channel },
        });

        var body = new Dictionary<string, object?>
        {
            { "agentProfileId", agentProfileId },
            { "channel", channel },
        };
        if (contactIdentifier is not null)
        {
            body["contactIdentifier"] = contactIdentifier;
        }
        if (callId is not null)
        {
            body["callId"] = callId;
        }
        if (metadata is not null)
        {
            body["metadata"] = metadata;
        }

        return await _client.RequestAsync("/agent/conversations", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Read one conversation with a page of its messages, in order. Paged rather than whole:
    /// a transcript has no natural ceiling. Omitting <paramref name="limit"/> gives 100
    /// messages, not all of them.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/conversations/:conversationId</c>.
    /// </summary>
    /// <param name="conversationId">The conversation to read.</param>
    /// <param name="page">Page number; defaults to 1.</param>
    /// <param name="limit">Messages per page; defaults to 100, capped at 500.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetConversationAsync(
        string conversationId,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "conversationId", conversationId } });

        var safeId = Uri.EscapeDataString(conversationId);
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });
        return await _client.RequestAsync($"/agent/conversations/{safeId}{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Append one message to a conversation. Records a turn rather than generating one, so
    /// <paramref name="role"/> is explicit — an assistant turn your own runtime produced is
    /// the normal case here. <paramref name="content"/> is optional so a tool-only turn
    /// needs no prose.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/conversations/:conversationId/messages</c>.
    /// </summary>
    /// <param name="conversationId">The conversation to append to.</param>
    /// <param name="role">"user", "assistant", "system", or "tool".</param>
    /// <param name="content">What was said.</param>
    /// <param name="toolCalls">Tools invoked on this turn.</param>
    /// <param name="toolResults">Their results.</param>
    /// <param name="ttfbMs">Time to first byte/audio, in milliseconds.</param>
    /// <param name="latencyMs">Total turn latency, in milliseconds.</param>
    /// <param name="bargeInOccurred">Whether the far end talked over this turn.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> AppendConversationMessageAsync(
        string conversationId,
        string role,
        string? content = null,
        IEnumerable<object>? toolCalls = null,
        IEnumerable<object>? toolResults = null,
        int? ttfbMs = null,
        int? latencyMs = null,
        bool? bargeInOccurred = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "conversationId", conversationId },
            { "role", role },
        });

        var safeId = Uri.EscapeDataString(conversationId);
        var body = new Dictionary<string, object?> { { "role", role } };
        if (content is not null)
        {
            body["content"] = content;
        }
        if (toolCalls is not null)
        {
            body["toolCalls"] = toolCalls;
        }
        if (toolResults is not null)
        {
            body["toolResults"] = toolResults;
        }
        if (ttfbMs is not null)
        {
            body["ttfbMs"] = ttfbMs;
        }
        if (latencyMs is not null)
        {
            body["latencyMs"] = latencyMs;
        }
        if (bargeInOccurred is not null)
        {
            body["bargeInOccurred"] = bargeInOccurred;
        }

        return await _client.RequestAsync($"/agent/conversations/{safeId}/messages", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// End a conversation, closing its open session. <paramref name="status"/> records HOW
    /// it ended and cannot be recovered afterwards, so pass the one that actually happened.
    /// Ending an already-ended conversation is a no-op, so a retry after a dropped
    /// connection cannot overwrite it.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>PUT /agent/conversations/:conversationId</c>.
    /// </summary>
    /// <param name="conversationId">The conversation to end.</param>
    /// <param name="status">"completed", "escalated", or "abandoned" (defaults to "completed").</param>
    /// <param name="metadata">Metadata merged onto the conversation.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> EndConversationAsync(
        string conversationId,
        string? status = null,
        IDictionary<string, object?>? metadata = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "conversationId", conversationId } });

        var safeId = Uri.EscapeDataString(conversationId);
        var body = new Dictionary<string, object?>();
        if (status is not null)
        {
            body["status"] = status;
        }
        if (metadata is not null)
        {
            body["metadata"] = metadata;
        }

        return await _client.RequestAsync($"/agent/conversations/{safeId}", HttpMethod.Put, body, options);
    }

    /// <summary>
    /// List an agent's per-channel settings.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/profiles/:agentProfileId/channels</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent whose channel settings to list.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentChannelSettingsAsync(string agentProfileId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/channels", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single per-channel setting for an agent.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/profiles/:agentProfileId/channels/:channel</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent the setting belongs to.</param>
    /// <param name="channel">The channel: "webchat", "sms", or "voice".</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentChannelSettingAsync(string agentProfileId, string channel, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId }, { "channel", channel } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        var safeChannel = Uri.EscapeDataString(channel);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/channels/{safeChannel}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create or update (upsert) an agent's per-channel setting. One row per (agent, channel);
    /// the channel is taken from the path, the fields from <paramref name="settingData"/>
    /// (keys: <c>allowedTools</c>, <c>enabled</c>, <c>channelPrompt</c>, <c>greeting</c>,
    /// <c>llmProvider</c>, <c>llmModel</c>, <c>temperature</c>, <c>voiceId</c>). Model
    /// settings are per-channel because each channel is served by a different runtime.
    /// Spoken channels also accept <c>speed</c> (0.7-1.2), <c>stability</c> (0-1),
    /// <c>similarityBoost</c> (0-1), <c>speechModel</c> (how the agent is voiced, chosen
    /// independently of <c>llmModel</c>), <c>turnEagerness</c>
    /// ("patient"|"normal"|"eager"), <c>turnTimeoutSeconds</c> and
    /// <c>initialWaitSeconds</c> (1-300, or -1 for no timeout),
    /// <c>silenceEndCallSeconds</c> (10-7200), <c>maxCallDurationSeconds</c> (60-7200),
    /// <c>allowGreetingInterruption</c>, <c>keyterms</c> (terms the transcriber is biased
    /// toward), <c>backgroundSound</c> (<c>{ preset, volume }</c> or null), and
    /// <c>workflow</c> — the conversation workflow graph, with <c>nodes</c> and <c>edges</c>
    /// keyed maps plus optional <c>subgraphs</c> and <c>prevent_subagent_loops</c>; node bodies
    /// are defined by the voice runtime and passed through as-is. <c>workflow</c> is accepted on
    /// the voice channel only — sending one on webchat or sms is a 400; null clears it.
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /agent/profiles/:agentProfileId/channels/:channel</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent the setting belongs to.</param>
    /// <param name="channel">The channel: "webchat", "sms", or "voice".</param>
    /// <param name="settingData">The fields to set.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UpsertAgentChannelSettingAsync(
        string agentProfileId,
        string channel,
        IDictionary<string, object?>? settingData = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId }, { "channel", channel } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        var safeChannel = Uri.EscapeDataString(channel);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/channels/{safeChannel}", HttpMethod.Put, settingData ?? new Dictionary<string, object?>(), options);
    }

    /// <summary>
    /// Delete an agent's per-channel setting.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /agent/profiles/:agentProfileId/channels/:channel</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent the setting belongs to.</param>
    /// <param name="channel">The channel: "webchat", "sms", or "voice".</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> DeleteAgentChannelSettingAsync(string agentProfileId, string channel, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId }, { "channel", channel } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        var safeChannel = Uri.EscapeDataString(channel);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/channels/{safeChannel}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// List a group-level agent's deployments to subgroups. Each deployment carries
    /// <c>agentDeploymentId</c>, <c>groupId</c>, <c>subgroupId</c>, <c>agentProfileId</c>,
    /// <c>sendAuthorityOverride</c>, <c>sendAuthorityChangedBy</c>, <c>sendAuthorityChangedAt</c>,
    /// <c>enabled</c>, <c>createdAt</c>, and <c>updatedAt</c>.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/profiles/:agentProfileId/deployments</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent whose deployments to list.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentDeploymentsAsync(string agentProfileId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/deployments", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Deploy a group-level agent to a subgroup, or change that deployment (upsert). One row per
    /// (agent, subgroup); the subgroup is taken from the path, the fields from
    /// <paramref name="deploymentData"/> (keys: <c>sendAuthorityOverride</c>, "review" or
    /// "autopilot", or null to clear the override so the deployment inherits the profile
    /// default; <c>enabled</c>, defaults to true).
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /agent/profiles/:agentProfileId/deployments/:subgroupId</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent to deploy.</param>
    /// <param name="subgroupId">The subgroup to deploy to (starts with 'S').</param>
    /// <param name="deploymentData">The fields to set.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UpsertAgentDeploymentAsync(
        string agentProfileId,
        string subgroupId,
        IDictionary<string, object?>? deploymentData = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId }, { "subgroupId", subgroupId } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        var safeSubgroupId = Uri.EscapeDataString(subgroupId);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/deployments/{safeSubgroupId}", HttpMethod.Put, deploymentData ?? new Dictionary<string, object?>(), options);
    }

    /// <summary>
    /// Remove an agent's deployment from a subgroup. Returns the removed deployment.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /agent/profiles/:agentProfileId/deployments/:subgroupId</c>.
    /// </summary>
    /// <param name="agentProfileId">The deployed agent.</param>
    /// <param name="subgroupId">The subgroup to remove the deployment from.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> DeleteAgentDeploymentAsync(string agentProfileId, string subgroupId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId }, { "subgroupId", subgroupId } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        var safeSubgroupId = Uri.EscapeDataString(subgroupId);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/deployments/{safeSubgroupId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// List the endpoint bindings (which agent answers which number) in a group, ordered by
    /// number. Each binding carries <c>agentEndpointId</c>, <c>groupId</c>, <c>subgroupId</c>,
    /// <c>channel</c>, <c>endpointType</c>, <c>endpointValue</c>, <c>agentProfileId</c>,
    /// <c>answerMode</c>, <c>overrides</c>, <c>enabled</c>, <c>createdAt</c>, and <c>updatedAt</c>.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/endpoints</c>.
    /// </summary>
    /// <param name="groupId">The group whose bindings to list (required).</param>
    /// <param name="subgroupId">Narrow to one subgroup's bindings.</param>
    /// <param name="agentProfileId">Narrow to one agent's bindings.</param>
    /// <param name="channel">Narrow to one channel ("sms").</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentEndpointsAsync(
        string groupId,
        string? subgroupId = null,
        string? agentProfileId = null,
        string? channel = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "agentProfileId", agentProfileId },
            { "channel", channel },
        });

        return await _client.RequestAsync($"/agent/endpoints{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Assign an agent to answer a number (upsert keyed by channel + number). Assigning again
    /// replaces the previous agent, so a number has one agent per channel. Only SMS numbers can be bound.
    /// <paramref name="endpointData"/> keys: <c>agentProfileId</c> (required; the agent must
    /// be active, in the number's group, and either belong to the number's subgroup or be a
    /// group-level agent with an enabled deployment there) and <c>answerMode</c>
    /// ("all_inbound", the default). Returns the created or updated binding.
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /agent/endpoints/:channel/:endpointValue</c>.
    /// </summary>
    /// <param name="channel">The channel to bind; must be "sms".</param>
    /// <param name="endpointValue">The phone number: 10 to 15 digits, country code included, no "+".</param>
    /// <param name="endpointData">The agent to assign and its answer mode.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UpsertAgentEndpointAsync(
        string channel,
        string endpointValue,
        IDictionary<string, object?> endpointData,
        RequestOptions? options = null)
    {
        object? agentProfileId = null;
        if (endpointData is not null)
        {
            endpointData.TryGetValue("agentProfileId", out agentProfileId);
        }
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "channel", channel },
            { "endpointValue", endpointValue },
            { "agentProfileId", agentProfileId },
        });

        var safeChannel = Uri.EscapeDataString(channel);
        var safeEndpointValue = Uri.EscapeDataString(endpointValue);
        return await _client.RequestAsync($"/agent/endpoints/{safeChannel}/{safeEndpointValue}", HttpMethod.Put, endpointData, options);
    }

    /// <summary>
    /// Remove the agent assigned to a number. Returns the removed binding.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /agent/endpoints/:channel/:endpointValue</c>.
    /// </summary>
    /// <param name="channel">The bound channel; must be "sms".</param>
    /// <param name="endpointValue">The phone number: digits only, country code included, no "+".</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> DeleteAgentEndpointAsync(string channel, string endpointValue, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "channel", channel }, { "endpointValue", endpointValue } });

        var safeChannel = Uri.EscapeDataString(channel);
        var safeEndpointValue = Uri.EscapeDataString(endpointValue);
        return await _client.RequestAsync($"/agent/endpoints/{safeChannel}/{safeEndpointValue}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// List paused agent conversations in a group, newest first. A paused conversation is one a
    /// person took over; the agent stays out of it until it is resumed. Each pause carries
    /// <c>groupId</c>, <c>subgroupId</c>, <c>channel</c>, <c>endpointValue</c>,
    /// <c>contactIdentifier</c>, <c>reason</c> ("human_reply", "escalation" or "manual"), <c>pausedBy</c>,
    /// <c>createdAt</c>, and <c>updatedAt</c>.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/pauses</c>.
    /// </summary>
    /// <param name="groupId">The group whose paused conversations to list (required).</param>
    /// <param name="subgroupId">Narrow to one subgroup.</param>
    /// <param name="channel">Narrow to one channel ("sms").</param>
    /// <param name="endpointValue">Narrow to one agent number (digits only, no "+").</param>
    /// <param name="contactIdentifier">Narrow to one contact number (digits only, no "+").</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentContactPausesAsync(
        string groupId,
        string? subgroupId = null,
        string? channel = null,
        string? endpointValue = null,
        string? contactIdentifier = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "channel", channel },
            { "endpointValue", endpointValue },
            { "contactIdentifier", contactIdentifier },
        });

        return await _client.RequestAsync($"/agent/pauses{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Pause an agent on one conversation (its number + one contact). The agent stops answering
    /// that contact on that number until the conversation is resumed; there is no timer. The
    /// conversation's pending replies are rejected. Pausing a paused conversation keeps the
    /// original pause. <paramref name="pauseData"/> keys: <c>reason</c> ("human_reply" or
    /// "manual", the default). Returns the pause.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>PUT /agent/pauses/:channel/:endpointValue/:contactIdentifier</c>.
    /// </summary>
    /// <param name="channel">The channel; must be "sms".</param>
    /// <param name="endpointValue">The agent's number: 10 to 15 digits, country code included, no "+".</param>
    /// <param name="contactIdentifier">The contact's number: 10 to 15 digits, country code included, no "+".</param>
    /// <param name="pauseData">The optional pause reason; an empty object is sent when null.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> PauseAgentConversationAsync(
        string channel,
        string endpointValue,
        string contactIdentifier,
        IDictionary<string, object?>? pauseData = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "channel", channel },
            { "endpointValue", endpointValue },
            { "contactIdentifier", contactIdentifier },
        });

        var safeChannel = Uri.EscapeDataString(channel);
        var safeEndpointValue = Uri.EscapeDataString(endpointValue);
        var safeContactIdentifier = Uri.EscapeDataString(contactIdentifier);
        return await _client.RequestAsync($"/agent/pauses/{safeChannel}/{safeEndpointValue}/{safeContactIdentifier}", HttpMethod.Put, pauseData ?? new Dictionary<string, object?>(), options);
    }

    /// <summary>
    /// Resume an agent on a paused conversation, so it answers the contact's next text again.
    /// Returns the removed pause.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>DELETE /agent/pauses/:channel/:endpointValue/:contactIdentifier</c>.
    /// </summary>
    /// <param name="channel">The channel; must be "sms".</param>
    /// <param name="endpointValue">The agent's number: 10 to 15 digits, country code included, no "+".</param>
    /// <param name="contactIdentifier">The contact's number: 10 to 15 digits, country code included, no "+".</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> ResumeAgentConversationAsync(
        string channel,
        string endpointValue,
        string contactIdentifier,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "channel", channel },
            { "endpointValue", endpointValue },
            { "contactIdentifier", contactIdentifier },
        });

        var safeChannel = Uri.EscapeDataString(channel);
        var safeEndpointValue = Uri.EscapeDataString(endpointValue);
        var safeContactIdentifier = Uri.EscapeDataString(contactIdentifier);
        return await _client.RequestAsync($"/agent/pauses/{safeChannel}/{safeEndpointValue}/{safeContactIdentifier}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// List held agent replies (the Pending Replies queue) in a group, newest first. Each draft
    /// carries <c>agentReplyDraftId</c>, <c>groupId</c>, <c>subgroupId</c>, <c>agentProfileId</c>,
    /// <c>conversationId</c>, <c>channel</c>, <c>endpointValue</c>, <c>contactPhoneNumber</c>,
    /// <c>inboundText</c>, <c>draftText</c>, <c>sentText</c>, <c>reason</c>, <c>status</c>,
    /// <c>decidedBy</c>, <c>decidedAt</c>, <c>sentMessageId</c>, <c>deliveredBy</c> ("signalhouse" or
    /// "external"; null until decided), <c>failureReason</c> ("delivery_unconfirmed" or null),
    /// <c>claimId</c> (the current claim's id; null until claimed), <c>createdAt</c>, and
    /// <c>updatedAt</c>.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/drafts</c>.
    /// </summary>
    /// <param name="groupId">The group whose drafts to list (required).</param>
    /// <param name="subgroupId">Narrow to one subgroup's drafts.</param>
    /// <param name="agentProfileId">Narrow to one agent's drafts.</param>
    /// <param name="status">Narrow to one status: "pending", "sending", "sent", "rejected" or "failed".</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="limit">Items per page (default 25, max 500).</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentReplyDraftsAsync(
        string groupId,
        string? subgroupId = null,
        string? agentProfileId = null,
        string? status = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "agentProfileId", agentProfileId },
            { "status", status },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/agent/drafts{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Approve a held agent reply and send it from the agent's number, optionally with edited
    /// text. It goes through the normal SMS send path, so opt-out and billing apply; the draft
    /// comes back "failed" when the send path accepted nothing. A draft that is no longer
    /// pending returns 409. Returns the draft, now sent (or failed).
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/drafts/:agentReplyDraftId/approve</c>.
    /// </summary>
    /// <param name="agentReplyDraftId">The draft to approve.</param>
    /// <param name="text">Replacement text when a person edited the reply (1 to 10000 characters); null sends the agent's draft as written.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> ApproveAgentReplyDraftAsync(string agentReplyDraftId, string? text = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentReplyDraftId", agentReplyDraftId } });

        var safeId = Uri.EscapeDataString(agentReplyDraftId);
        var body = new Dictionary<string, object?>();
        if (text is not null)
        {
            body["text"] = text;
        }
        return await _client.RequestAsync($"/agent/drafts/{safeId}/approve", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Reject a held agent reply so it is never sent. Returns the rejected draft. A draft that
    /// is no longer pending returns 409.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/drafts/:agentReplyDraftId/reject</c>.
    /// </summary>
    /// <param name="agentReplyDraftId">The draft to reject.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> RejectAgentReplyDraftAsync(string agentReplyDraftId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentReplyDraftId", agentReplyDraftId } });

        var safeId = Uri.EscapeDataString(agentReplyDraftId);
        return await _client.RequestAsync($"/agent/drafts/{safeId}/reject", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Claim a held agent reply for delivery through your own channel instead of Signal House's
    /// send path. Nothing is sent: the draft becomes "sending" with <c>deliveredBy</c> "external",
    /// <c>sentText</c> and a new <c>claimId</c> set, and you then report the outcome with
    /// <see cref="CompleteAgentReplyDraftAsync"/> or <see cref="ReleaseAgentReplyDraftAsync"/>,
    /// passing that <c>claimId</c> back.
    /// The claim means two approvals can never both deliver. A draft that is no longer pending
    /// returns 409; one whose number has left its subgroup returns 400 and is closed as failed.
    /// Returns the claimed draft, including its <c>claimId</c>.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/drafts/:agentReplyDraftId/claim</c>.
    /// </summary>
    /// <param name="agentReplyDraftId">The draft to claim.</param>
    /// <param name="text">Replacement text when a person edited the reply (1 to 10000 characters); null claims the agent's draft as written.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> ClaimAgentReplyDraftAsync(string agentReplyDraftId, string? text = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentReplyDraftId", agentReplyDraftId } });

        var safeId = Uri.EscapeDataString(agentReplyDraftId);
        var body = new Dictionary<string, object?>();
        if (text is not null)
        {
            body["text"] = text;
        }
        return await _client.RequestAsync($"/agent/drafts/{safeId}/claim", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Mark a claimed agent reply sent once your channel accepted it. Only the claim named by
    /// <paramref name="claimId"/> can be completed; anything else returns 409. It is also accepted
    /// after an unfinished claim was closed as "failed" with <c>failureReason</c>
    /// "delivery_unconfirmed", which it turns into "sent".
    /// Returns the draft, now sent.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/drafts/:agentReplyDraftId/complete</c>.
    /// </summary>
    /// <param name="agentReplyDraftId">The claimed draft.</param>
    /// <param name="claimId">The claim being completed, as returned by <see cref="ClaimAgentReplyDraftAsync"/>.</param>
    /// <param name="externalMessageId">Your channel's id for the sent message (up to 256 characters), stored as <c>sentMessageId</c>; null omits it.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> CompleteAgentReplyDraftAsync(string agentReplyDraftId, string claimId, string? externalMessageId = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentReplyDraftId", agentReplyDraftId }, { "claimId", claimId } });

        var safeId = Uri.EscapeDataString(agentReplyDraftId);
        var body = new Dictionary<string, object?> { { "claimId", claimId } };
        if (externalMessageId is not null)
        {
            body["externalMessageId"] = externalMessageId;
        }
        return await _client.RequestAsync($"/agent/drafts/{safeId}/complete", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Give back a claim whose reply certainly did not go out: the draft returns to "pending", or
    /// with <paramref name="permanent"/> true is closed as "failed". If you cannot tell whether it
    /// went out, do not release it; a claim left unfinished for 30 minutes is closed as failed with
    /// <c>failureReason</c> "delivery_unconfirmed". Only the claim named by
    /// <paramref name="claimId"/> can be released; anything else returns 409.
    /// Returns the draft, pending again or failed.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>POST /agent/drafts/:agentReplyDraftId/release</c>.
    /// </summary>
    /// <param name="agentReplyDraftId">The claimed draft.</param>
    /// <param name="claimId">The claim being released, as returned by <see cref="ClaimAgentReplyDraftAsync"/>.</param>
    /// <param name="permanent">True closes the draft as failed instead of returning it to pending; null omits it.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> ReleaseAgentReplyDraftAsync(string agentReplyDraftId, string claimId, bool? permanent = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentReplyDraftId", agentReplyDraftId }, { "claimId", claimId } });

        var safeId = Uri.EscapeDataString(agentReplyDraftId);
        var body = new Dictionary<string, object?> { { "claimId", claimId } };
        if (permanent is not null)
        {
            body["permanent"] = permanent;
        }
        return await _client.RequestAsync($"/agent/drafts/{safeId}/release", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Publish an agent profile, setting <c>publishStatus</c> to "published" and stamping
    /// <c>publishedAt</c>. The server rejects the call with 400 when the agent has no enabled
    /// deployment, and with 409 when the agent is inactive.
    /// Allowed roles: api, admin, developer.
    /// <c>POST /agent/profiles/:agentProfileId/publish</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent profile to publish.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> PublishAgentProfileAsync(string agentProfileId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/publish", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Return an agent profile to draft (<c>publishStatus</c> "draft").
    /// Allowed roles: api, admin, developer.
    /// <c>POST /agent/profiles/:agentProfileId/unpublish</c>.
    /// </summary>
    /// <param name="agentProfileId">The agent profile to unpublish.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UnpublishAgentProfileAsync(string agentProfileId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentProfileId", agentProfileId } });

        var safeId = Uri.EscapeDataString(agentProfileId);
        return await _client.RequestAsync($"/agent/profiles/{safeId}/unpublish", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Read the tenant AI settings for a scope (group, or a subgroup within it) — brand, tone,
    /// timezone, business hours, and after-hours message. Returns the scope's own row only (no
    /// group-level fallback).
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/tenant-settings</c>.
    /// </summary>
    /// <param name="groupId">The group (required).</param>
    /// <param name="subgroupId">The subgroup; null for the group-level settings.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetTenantAiSettingsAsync(string groupId, string? subgroupId = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "groupId", groupId }, { "subgroupId", subgroupId } });
        return await _client.RequestAsync($"/agent/tenant-settings{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create or update (upsert) the tenant AI settings for a scope. The scope is identified by
    /// the query params; <paramref name="settingsData"/> carries the fields (keys: <c>brandName</c>,
    /// <c>tone</c>, <c>timezone</c>, <c>businessHours</c>, <c>afterHoursMessage</c>).
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /agent/tenant-settings</c>.
    /// </summary>
    /// <param name="groupId">The group (required).</param>
    /// <param name="subgroupId">The subgroup; null for the group-level settings.</param>
    /// <param name="settingsData">The fields to set.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UpsertTenantAiSettingsAsync(
        string groupId,
        string? subgroupId = null,
        IDictionary<string, object?>? settingsData = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "groupId", groupId }, { "subgroupId", subgroupId } });
        return await _client.RequestAsync($"/agent/tenant-settings{qs}", HttpMethod.Put, settingsData ?? new Dictionary<string, object?>(), options);
    }

    /// <summary>
    /// Delete the tenant AI settings for a scope.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /agent/tenant-settings</c>.
    /// </summary>
    /// <param name="groupId">The group (required).</param>
    /// <param name="subgroupId">The subgroup; null for the group-level settings.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> DeleteTenantAiSettingsAsync(string groupId, string? subgroupId = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "groupId", groupId }, { "subgroupId", subgroupId } });
        return await _client.RequestAsync($"/agent/tenant-settings{qs}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// List the knowledge-base items an agent can answer from (RAG), scoped to a group and
    /// optionally narrowed to a subgroup and/or a single agent. Paginated (server default 25
    /// per page, max 100).
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/knowledge</c>.
    /// </summary>
    /// <param name="groupId">The group whose knowledge to list (required).</param>
    /// <param name="subgroupId">Narrow to a subgroup.</param>
    /// <param name="agentProfileId">Narrow to one agent.</param>
    /// <param name="page">Page number (1-based) for pagination.</param>
    /// <param name="limit">Items per page (max 100; server default 25).</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetKnowledgeItemsAsync(string groupId, string? subgroupId = null, string? agentProfileId = null, int? page = null, int? limit = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "groupId", groupId }, { "subgroupId", subgroupId }, { "agentProfileId", agentProfileId }, { "page", page?.ToString() }, { "limit", limit?.ToString() } });
        return await _client.RequestAsync($"/agent/knowledge{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single knowledge item by id.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/knowledge/:knowledgeItemId</c>.
    /// </summary>
    /// <param name="knowledgeItemId">The id of the knowledge item to fetch.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetKnowledgeItemAsync(string knowledgeItemId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "knowledgeItemId", knowledgeItemId } });

        var safeId = Uri.EscapeDataString(knowledgeItemId);
        return await _client.RequestAsync($"/agent/knowledge/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a knowledge item. The raw text is stored; Atlas Vector Search owns the embedding.
    /// <paramref name="itemData"/> must include the required fields <c>groupId</c> and <c>text</c>.
    /// Allowed roles: api, admin, developer.
    /// <c>POST /agent/knowledge</c>.
    /// </summary>
    /// <param name="itemData">The knowledge fields.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> CreateKnowledgeItemAsync(IDictionary<string, object?> itemData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "itemData", itemData } });

        return await _client.RequestAsync("/agent/knowledge", HttpMethod.Post, itemData, options);
    }

    /// <summary>
    /// Update a knowledge item. The item's scope (group/subgroup/agent) is immutable.
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /agent/knowledge/:id</c>.
    /// </summary>
    /// <param name="id">The id of the knowledge item to update.</param>
    /// <param name="updateData">The fields to update: title, text, source, enabled, metadata.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UpdateKnowledgeItemAsync(string id, IDictionary<string, object?> updateData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/agent/knowledge/{safeId}", HttpMethod.Put, updateData, options);
    }

    /// <summary>
    /// Delete a knowledge item by its id.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /agent/knowledge/:id</c>.
    /// </summary>
    /// <param name="id">The id of the knowledge item to delete.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> DeleteKnowledgeItemAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/agent/knowledge/{safeId}", HttpMethod.Delete, options: options);
    }

    /// <summary>
    /// List the tools defined under a group (webhook, configured builtin, mcp). webhookAuth
    /// secrets are redacted. Paginated (server default 25 per page, max 100).
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/tools</c>.
    /// </summary>
    /// <param name="groupId">The group whose tools to list (required).</param>
    /// <param name="subgroupId">Narrow to a subgroup.</param>
    /// <param name="agentProfileId">Narrow to one agent.</param>
    /// <param name="type">Narrow to a tool type: "builtin", "webhook", or "mcp".</param>
    /// <param name="page">Page number (1-based) for pagination.</param>
    /// <param name="limit">Items per page (max 100; server default 25).</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentToolsAsync(string groupId, string? subgroupId = null, string? agentProfileId = null, string? type = null, int? page = null, int? limit = null, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "groupId", groupId }, { "subgroupId", subgroupId }, { "agentProfileId", agentProfileId }, { "type", type }, { "page", page?.ToString() }, { "limit", limit?.ToString() } });
        return await _client.RequestAsync($"/agent/tools{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a single agent tool by id. webhookAuth secrets are redacted.
    /// Allowed roles: api, admin, developer, billing, user.
    /// <c>GET /agent/tools/:agentToolId</c>.
    /// </summary>
    /// <param name="agentToolId">The id of the tool to fetch.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> GetAgentToolAsync(string agentToolId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "agentToolId", agentToolId } });

        var safeId = Uri.EscapeDataString(agentToolId);
        return await _client.RequestAsync($"/agent/tools/{safeId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create an agent tool. A webhook tool's <c>webhookUrl</c> must be an https URL that does not
    /// target a private/internal address. <c>webhookAuth</c> MUST be
    /// <c>{ headers?: { [name]: value }, signingSecret?: string }</c> — any other shape (e.g.
    /// <c>{ header, value }</c>) is rejected with a 400. <c>headers</c> are sent verbatim on every
    /// call (reserved/framing and <c>x-signalhouse-*</c> names are rejected); <c>signingSecret</c>
    /// makes us send <c>X-SignalHouse-Signature: sha256=HMAC-SHA256(`${X-SignalHouse-Timestamp}.${rawBody}`)</c>
    /// so you can verify the call and reject replays. Stored, never echoed back.
    /// <paramref name="toolData"/> must include <c>groupId</c>, <c>name</c>, and <c>description</c>.
    /// Allowed roles: api, admin, developer.
    /// <c>POST /agent/tools</c>.
    /// </summary>
    /// <param name="toolData">The tool fields.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> CreateAgentToolAsync(IDictionary<string, object?> toolData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "toolData", toolData } });

        return await _client.RequestAsync("/agent/tools", HttpMethod.Post, toolData, options);
    }

    /// <summary>
    /// Update an agent tool. The scope is immutable; a supplied <c>webhookUrl</c> is SSRF-checked.
    /// Allowed roles: api, admin, developer.
    /// <c>PUT /agent/tools/:id</c>.
    /// </summary>
    /// <param name="id">The id of the tool to update.</param>
    /// <param name="updateData">The fields to update.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> UpdateAgentToolAsync(string id, IDictionary<string, object?> updateData, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id }, { "updateData", updateData } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/agent/tools/{safeId}", HttpMethod.Put, updateData, options);
    }

    /// <summary>
    /// Delete an agent tool by its id.
    /// Allowed roles: api, admin, developer.
    /// <c>DELETE /agent/tools/:id</c>.
    /// </summary>
    /// <param name="id">The id of the tool to delete.</param>
    /// <param name="options">Additional options for the request.</param>
    public async Task<SignalHouseResponse> DeleteAgentToolAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/agent/tools/{safeId}", HttpMethod.Delete, options: options);
    }
}
