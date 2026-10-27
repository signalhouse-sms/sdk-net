using System.Net.Http.Headers;
using System.Text.Json;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// SMS, MMS, and group message operations.
/// </summary>
/// <remarks>
/// Date-range parameters (startDate, endDate) on list and analytics methods
/// accept ISO-8601 date or timestamp strings, bound per dateBounds. A bare date is
/// always that whole UTC day. A timestamp is widened to its whole UTC day in "day"
/// mode and bound as the instant it names, in whatever
/// offset it was written ("2026-09-14T00:00:00-04:00" is 04:00Z), in "exact" mode,
/// where day buckets also align to the offset endDate carries. Omitted, dateBounds is "day".
/// Business-number filters and SMS/MMS senders accept digits-only 5-6 digit
/// Short Codes or 10+ digit long numbers. Recipient numbers remain 10+ digits.
/// </remarks>
public sealed class MessagesDomain
{
    private readonly HttpClientWrapper _client;

    internal MessagesDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get a list of messages with optional filters and pagination.
    /// messageType accepts a single value (e.g. "SMS") or a comma-separated list (e.g. "SMS,MMS").
    /// Allowed values: SMS, MMS, RCS, WHATSAPP, VIBER, P2P. Null defaults to all non-P2P types.
    /// Inbound messages in the response also carry sentimentScore (-100..100, null when unscored),
    /// sentimentLabel ("positive", "neutral" or "negative", null when sentimentScore is null) and
    /// sentimentScoredAt. Outbound messages are never scored.
    /// </summary>
    /// <param name="sortField">Field to sort by: createdAt, segmentCount, status, or sentimentScore.</param>
    /// <param name="channel">Optional channel filter: tenDLC, virtualLongCode, tollFree, shortCode, or p2p (single value or comma-separated list). Filters on each message's stored channel; older messages without one count as tenDLC.</param>
    /// <param name="sentimentLabel">Optional sentiment filter: positive, neutral, or negative (single value or comma-separated list). Applied as the sentimentScore range the label threshold produces, so unscored messages match no bucket and outbound messages are excluded.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="errorCode">Optional Signal House error code filter for FAILED messages (single value or comma-separated list). "OUT" is the campaign opt-out, displayed as RECIPIENT_OPTED_OUT. Carrier codes are not filterable here; they live inside successOrFailureReason.</param>
    /// <param name="region">Optional region filter: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list.</param>
    /// <param name="regionScopes">Optional JSON array of OR-ed branches, each {region, channels, brandId?, campaignId?, phoneNumber?} with channels from tenDLC, virtualLongCode, tollFree, shortCode, p2p. "[]" matches nothing.</param>
    /// <param name="senderPhoneNumber">Filter by sender phone number. A plain value is an exact match; a value containing % is a wildcard for a partial match (e.g. "%1234" matches numbers ending in 1234). 3-15 digits are required alongside any %.</param>
    /// <param name="recipientPhoneNumber">Filter by recipient phone number. A plain value is an exact match; a value containing % is a wildcard for a partial match (e.g. "%1234" matches numbers ending in 1234). 3-15 digits are required alongside any %.</param>
    /// <param name="registrationId">Filter to messages attributed to these registrations. Cannot be combined with campaignId; each id must belong to the group. Single value or comma-separated list.</param>
    public async Task<SignalHouseResponse> GetMessagesAsync(
        string? id = null,
        string? campaignId = null,
        string? brandId = null,
        string? subgroupId = null,
        string? groupId = null,
        string? phoneNumber = null,
        string? senderPhoneNumber = null,
        string? recipientPhoneNumber = null,
        string? status = null,
        string? direction = null,
        string? messageType = null,
        string? carrier = null,
        string? startDate = null,
        string? endDate = null,
        string? sortField = null,
        string? sortOrder = null,
        int? page = null,
        int? limit = null,
        string? channel = null,
        string? sentimentLabel = null,
        RequestOptions? options = null,
        string? dateBounds = null,
        string? errorCode = null,
        string? region = null,
        string? regionScopes = null,
        string? registrationId = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "brandId", brandId },
            { "subgroupId", subgroupId },
            { "groupId", groupId },
            { "phoneNumber", phoneNumber },
            { "senderPhoneNumber", senderPhoneNumber },
            { "recipientPhoneNumber", recipientPhoneNumber },
            { "status", status },
            { "direction", direction },
            { "messageType", messageType },
            { "channel", channel },
            { "region", region },
            { "regionScopes", regionScopes },
            { "carrier", carrier },
            { "sentimentLabel", sentimentLabel },
            { "errorCode", errorCode },
            { "startDate", startDate },
            { "endDate", endDate },
            { "sortField", sortField },
            { "sortOrder", sortOrder },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "dateBounds", dateBounds },
        });

        return await _client.RequestAsync($"/message{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get aggregated analytics for messages with optional filters.
    /// subgroupId, brandId, campaignId, phoneNumber and carrier each accept a single value or a
    /// comma-separated list; groupId is single-valued.
    /// The response also includes inbound-message sentiment for the requested scope: sentimentScore
    /// (volume-weighted average of every scored inbound message in range, -100..100, null when nothing
    /// was scored), sentimentLabel, sentimentScoredCount, sentimentPositive, sentimentNeutral and
    /// sentimentNegative. sentimentScore is an average, so it must never be summed across responses.
    /// </summary>
    /// <param name="channel">Optional channel filter: tenDLC, virtualLongCode, tollFree, shortCode, or p2p (single value or comma-separated list). Filters on the stored channel; a tenDLC selection also includes older messages with no channel, and p2p is matched by carrier.</param>
    /// <param name="messageType">Message type(s) the SENTIMENT figures are scoped to: SMS, MMS or P2P. Omit for all scored types. Does NOT narrow the message counts, which are always returned split per type.</param>
    /// <param name="carrierFamily">Optional filter by the recipient's resolved carrier family (Default, ATT, TMobile, Verizon, USCellular, GoogleVoice, ClearSky, Interop, RogueMobile, P2P, Standard, IceWireless); single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400 -- omit the parameter unless it is wanted.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="region">Optional region filter: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400.</param>
    /// <param name="regionScopes">Optional JSON array of OR-ed branches, each {region, channels, brandId?, campaignId?, phoneNumber?} with channels from tenDLC, virtualLongCode, tollFree, shortCode, p2p. "[]" matches nothing. Same hourly-analytics gate as region.</param>
    /// <param name="registrationId">Filter to traffic attributed to these registrations (hourly analytics only; a 400 while hourly reads are off). A phoneNumber filter takes precedence. Single value or comma-separated list.</param>
    public async Task<SignalHouseResponse> GetAnalyticsAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? brandId = null,
        string? campaignId = null,
        string? phoneNumber = null,
        string? carrier = null,
        string? startDate = null,
        string? endDate = null,
        string? channel = null,
        string? messageType = null,
        RequestOptions? options = null,
        string? carrierFamily = null,
        string? dateBounds = null,
        string? region = null,
        string? regionScopes = null,
        string? registrationId = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "phoneNumber", phoneNumber },
            { "carrier", carrier },
            { "carrierFamily", carrierFamily },
            { "startDate", startDate },
            { "endDate", endDate },
            { "channel", channel },
            { "region", region },
            { "regionScopes", regionScopes },
            { "messageType", messageType },
            { "dateBounds", dateBounds },
        });

        return await _client.RequestAsync($"/message/analytics{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get detailed analytics snapshot records for charting and aggregation.
    /// subgroupId, brandId, campaignId, phoneNumber and carrier each accept a single value or a
    /// comma-separated list; groupId is single-valued.
    /// cardTotals and every byDate/byPhoneNumber/byCarrier row also carry that row's own inbound-message
    /// sentiment: sentimentScore (volume-weighted average, -100..100, null when nothing in that row's
    /// scope was scored), sentimentLabel, sentimentScoredCount, sentimentPositive, sentimentNeutral and
    /// sentimentNegative. The fields are always present; sentimentScore is an average and must never be
    /// summed across rows or responses.
    /// </summary>
    /// <param name="channel">Optional channel filter: tenDLC, virtualLongCode, tollFree, shortCode, or p2p (single value or comma-separated list). Filters on the stored channel; a tenDLC selection also includes older messages with no channel, and p2p is a real channel here.</param>
    /// <param name="messageType">Message type(s) the SENTIMENT figures are scoped to: SMS, MMS or P2P. Omit for all scored types. Does NOT narrow the message counts, which are always returned split per type.</param>
    /// <param name="carrierFamily">Optional filter by the recipient's resolved carrier family (Default, ATT, TMobile, Verizon, USCellular, GoogleVoice, ClearSky, Interop, RogueMobile, P2P, Standard, IceWireless); single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400 -- omit the parameter unless it is wanted.</param>
    /// <param name="granularity">Time-bucket grain of the byDate rows: "day" or "hour". Null means day (each row's _id is a "YYYY-MM-DD" date). "hour" buckets by hour (each row's _id is a "YYYY-MM-DD HH:MM:SS" UTC hour; startDate is floored to its hour and endDate honoured as given) and is capped to a 7-day span. Only accepted where the environment serves hourly analytics; elsewhere any value, including "day", is rejected with a 400 -- leave it null unless hour grain is wanted.</param>
    /// <param name="breakdown">"carrier" or "carrierFamily". Adds a byBreakdown time × dimension series to the response ({dimension, rows}: each row is one byDate bucket (_id) for one carrier or carrier family (key), top 50 keys by volume, excluding P2P) and caps the span at 31 days. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400 -- omit the parameter unless it is wanted.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="region">Optional region filter: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400.</param>
    /// <param name="regionScopes">Optional JSON array of OR-ed branches, each {region, channels, brandId?, campaignId?, phoneNumber?} with channels from tenDLC, virtualLongCode, tollFree, shortCode, p2p. "[]" matches nothing. Same hourly-analytics gate as region.</param>
    /// <param name="registrationId">Filter to traffic attributed to these registrations (hourly analytics only; a 400 while hourly reads are off). A phoneNumber filter takes precedence. Single value or comma-separated list.</param>
    public async Task<SignalHouseResponse> GetAnalyticsDetailAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? brandId = null,
        string? campaignId = null,
        string? phoneNumber = null,
        string? carrier = null,
        string? startDate = null,
        string? endDate = null,
        string? channel = null,
        string? messageType = null,
        RequestOptions? options = null,
        string? granularity = null,
        string? carrierFamily = null,
        string? breakdown = null,
        string? dateBounds = null,
        string? region = null,
        string? regionScopes = null,
        string? registrationId = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "phoneNumber", phoneNumber },
            { "carrier", carrier },
            { "carrierFamily", carrierFamily },
            { "startDate", startDate },
            { "endDate", endDate },
            { "channel", channel },
            { "region", region },
            { "regionScopes", regionScopes },
            { "messageType", messageType },
            { "granularity", granularity },
            { "breakdown", breakdown },
            { "dateBounds", dateBounds },
        });

        return await _client.RequestAsync($"/message/analytics/detail{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get per-minute send throughput (peak segments per minute, sending minutes, minutes at a ceiling)
    /// per time bucket and per carrier family. Measured from SENT transitions at campaign × carrier-family
    /// grain — the grain carrier ceilings are enforced at — so a peak here is the figure a ceiling is
    /// compared against. Only accepted where the environment serves hourly analytics; elsewhere the request
    /// is rejected with a 400. Returns totals, byDate, byCarrierFamily and byBreakdown
    /// ({dimension: "carrierFamily", rows}); every row carries segments, messages, peakSegmentsPerMinute,
    /// peakMessagesPerMinute, peakMinute (UTC "YYYY-MM-DD HH:MM:SS", null when no traffic), sendingMinutes,
    /// averageSegmentsPerSendingMinute and minutesAtCeiling.
    /// </summary>
    /// <param name="groupId">The group to scope the read to (required).</param>
    /// <param name="brandId">Brand scope (single value or comma-separated list).</param>
    /// <param name="campaignId">Campaign scope; wins over brandId (single value or comma-separated list).</param>
    /// <param name="carrierFamily">Carrier-family filter (ATT, TMobile, Verizon, USCellular, ...); single value or comma-separated list.</param>
    /// <param name="startDate">ISO-8601 date or timestamp. Bound per dateBounds: a bare date is always that whole UTC day (inclusive); a timestamp is widened to its whole UTC day in "day" mode and bound as the instant it names, in whatever offset it was written, in "exact" mode, then floored to the start of its hour at hour granularity and of its minute at day granularity (the cube is keyed by minute).</param>
    /// <param name="endDate">ISO-8601 date or timestamp. At hour granularity honoured as given; at day granularity bound per dateBounds: a bare date is always that whole UTC day (inclusive); a timestamp is widened to its whole UTC day in "day" mode and bound as the instant it names, in whatever offset it was written, in "exact" mode. In "exact" mode day buckets align to the UTC offset this bound carries, so a client that sends its own midnights gets its own calendar dates back. The span is capped at 35 days.</param>
    /// <param name="granularity">Bucket grain of the byDate and byBreakdown rows: "hour" (default; span capped at 7 days) or "day".</param>
    /// <param name="ceiling">Segments-per-minute ceiling. When given, every row also reports minutesAtCeiling; otherwise minutesAtCeiling is null.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="registrationId">Filter to traffic attributed to these registrations (hourly analytics only; a 400 while hourly reads are off). Single value or comma-separated list.</param>
    public async Task<SignalHouseResponse> GetAnalyticsThroughputAsync(
        string groupId,
        string startDate,
        string endDate,
        string? brandId = null,
        string? campaignId = null,
        string? carrierFamily = null,
        string? granularity = null,
        int? ceiling = null,
        RequestOptions? options = null,
        string? dateBounds = null,
        string? registrationId = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "startDate", startDate }, { "endDate", endDate } });

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "carrierFamily", carrierFamily },
            { "startDate", startDate },
            { "endDate", endDate },
            { "granularity", granularity },
            { "ceiling", ceiling?.ToString() },
            { "dateBounds", dateBounds },
        });

        return await _client.RequestAsync($"/message/analytics/throughput{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get filter dropdown options (subgroups, brands, campaigns, phone numbers, carriers) for the
    /// Analytics page, scoped to a single group. Only items with
    /// at least one message are returned. Each phone number carries type (TOLL_FREE, SHORT_CODE, ALPHANUMERIC,
    /// VIRTUAL_LONG_NUMBER, ...) and country (ISO-2), null when unknown: TOLL_FREE, SHORT_CODE and ALPHANUMERIC are
    /// channels by type; any other US number is 10DLC and a non-US one is a Virtual Long Code. Carriers is a plain
    /// array of carrier name strings.
    /// </summary>
    /// <param name="region">Optional region restriction: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400.</param>
    public async Task<SignalHouseResponse> GetAnalyticsFilterOptionsAsync(
        string groupId,
        RequestOptions? options = null,
        string? region = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "region", region },
        });

        return await _client.RequestAsync($"/message/analytics/filter-options{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a paginated breakdown of message metrics grouped by subgroup. Each row carries the
    /// full set of sms/mms/p2p metric columns plus smsOptOuts/mmsOptOuts (sourced from the DNC
    /// analytics MV — P2P has no opt-outs) so callers can apply channel toggles client-side.
    /// Each row also carries that subgroup's inbound-message sentiment: sentimentScore
    /// (volume-weighted, -100..100, null when nothing was scored), sentimentLabel,
    /// sentimentScoredCount and the positive/neutral/negative counts, plus the same six per
    /// message type under smsSentiment*, mmsSentiment* and p2pSentiment*. The score is derived
    /// server-side; combine counts across rows, never the scores.
    /// Limit is capped server-side at 50 per page.
    /// </summary>
    /// <param name="channel">Optional channel filter and ranking: "both" (default, all activity) or tenDLC/virtualLongCode/tollFree/shortCode/p2p (single value or comma-separated list).</param>
    /// <param name="messageType">Message type(s) the SENTIMENT figures are scoped to: SMS, MMS or P2P (single value or comma-separated list). Omit for all scored types. Does NOT narrow the message counts, which are always returned split per type and have no filterable type dimension.</param>
    /// <param name="carrierFamily">Optional filter by the recipient's resolved carrier family (Default, ATT, TMobile, Verizon, USCellular, GoogleVoice, ClearSky, Interop, RogueMobile, P2P, Standard, IceWireless); single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400 -- omit the parameter unless it is wanted.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="region">Optional region filter: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400.</param>
    /// <param name="regionScopes">Optional JSON array of OR-ed branches, each {region, channels, brandId?, campaignId?, phoneNumber?} with channels from tenDLC, virtualLongCode, tollFree, shortCode, p2p. "[]" matches nothing. Same hourly-analytics gate as region.</param>
    /// <param name="registrationId">Filter to traffic attributed to these registrations (hourly analytics only; a 400 while hourly reads are off). A phoneNumber filter takes precedence. Single value or comma-separated list.</param>
    public async Task<SignalHouseResponse> GetAnalyticsBySubgroupAsync(
        string groupId,
        string? subgroupId = null,
        string? brandId = null,
        string? campaignId = null,
        string? phoneNumber = null,
        string? carrier = null,
        string? startDate = null,
        string? endDate = null,
        int? page = null,
        int? limit = null,
        string? channel = null,
        string? messageType = null,
        RequestOptions? options = null,
        string? carrierFamily = null,
        string? dateBounds = null,
        string? region = null,
        string? regionScopes = null,
        string? registrationId = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "phoneNumber", phoneNumber },
            { "carrier", carrier },
            { "carrierFamily", carrierFamily },
            { "startDate", startDate },
            { "endDate", endDate },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "channel", channel },
            { "region", region },
            { "regionScopes", regionScopes },
            { "messageType", messageType },
            { "dateBounds", dateBounds },
        });

        return await _client.RequestAsync($"/message/analytics/by-subgroup{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a paginated breakdown of failed messages grouped by error code. Each row contains
    /// per-channel (sms/mms/p2p) error counts plus an enriched description.
    /// Limit is capped server-side at 50 per page.
    /// </summary>
    /// <param name="channel">Optional channel filter and ranking: "both" (default, every code) or tenDLC/virtualLongCode/tollFree/shortCode/p2p (single value or comma-separated list). When one channel is selected, totalErrors reflects only that channel.</param>
    /// <param name="carrierFamily">Optional filter by the recipient's resolved carrier family (Default, ATT, TMobile, Verizon, USCellular, GoogleVoice, ClearSky, Interop, RogueMobile, P2P, Standard, IceWireless); single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400 -- omit the parameter unless it is wanted.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="region">Optional region filter: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400.</param>
    /// <param name="regionScopes">Optional JSON array of OR-ed branches, each {region, channels, brandId?, campaignId?, phoneNumber?} with channels from tenDLC, virtualLongCode, tollFree, shortCode, p2p. "[]" matches nothing. Same hourly-analytics gate as region.</param>
    /// <param name="registrationId">Filter to traffic attributed to these registrations (hourly analytics only; a 400 while hourly reads are off). A phoneNumber filter takes precedence. Single value or comma-separated list.</param>
    /// <param name="errorCode">Narrow the breakdown to a single failure reason: a numeric Infobip carrier code (as a string, e.g. "607"), the Signal House campaign-opt-out code "OUT", or a no-code failure class (other, moderation, att_limit, p2p_stop, campaign_opt_out, dnc_blocked, bind_hold_expired). Omitted: every reason.</param>
    public async Task<SignalHouseResponse> GetAnalyticsByErrorCodeAsync(
        string groupId,
        string? subgroupId = null,
        string? brandId = null,
        string? campaignId = null,
        string? phoneNumber = null,
        string? carrier = null,
        string? startDate = null,
        string? endDate = null,
        int? page = null,
        int? limit = null,
        string? channel = null,
        RequestOptions? options = null,
        string? carrierFamily = null,
        string? dateBounds = null,
        string? region = null,
        string? regionScopes = null,
        string? registrationId = null,
        string? errorCode = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "phoneNumber", phoneNumber },
            { "carrier", carrier },
            { "carrierFamily", carrierFamily },
            { "startDate", startDate },
            { "endDate", endDate },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "channel", channel },
            { "region", region },
            { "regionScopes", regionScopes },
            { "dateBounds", dateBounds },
            { "errorCode", errorCode },
        });

        return await _client.RequestAsync($"/message/analytics/by-error-code{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get aggregated DNC (Do Not Contact) opt-out analytics with optional filters. Returns totals,
    /// byDate, byPhoneNumber, bySenderNumber, byCampaign, bySubgroup, byType, byCarrier, byKeyword.
    /// bySenderNumber ({senderPhoneNumber, total, sms, mms}) attributes each opt-out to the first business
    /// number that received it, so it is unchanged by later campaign moves and START replies. byCampaign
    /// counts campaign-scoped opt-outs only, by the campaign at the time of the STOP; subgroup-scoped
    /// opt-outs (non-US recipients) are never attributed to a campaign and appear under bySubgroup
    /// instead. byType splits SMS/MMS. byKeyword breaks opt-outs down by the normalized
    /// keyword that revoked consent ({optOutKeyword, total, sms, mms}); opt-outs recorded before keyword
    /// capture shipped appear as a single optOutKeyword: null bucket ("not recorded"). The distinct
    /// keyword set is open-ended (campaigns register their own keywords beyond the mandatory floor) —
    /// render top-N plus Other, never a fixed list.
    /// </summary>
    /// <param name="channel">Optional channel filter: tenDLC, virtualLongCode, tollFree, or shortCode (single value or comma-separated list). Opt-outs are A2P-only, so p2p applies no filter. A tenDLC selection also includes older opt-outs with no channel.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="region">Optional region filter: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400.</param>
    /// <param name="regionScopes">Optional JSON array of OR-ed branches, each {region, channels, brandId?, campaignId?, phoneNumber?} with channels from tenDLC, virtualLongCode, tollFree, shortCode, p2p. "[]" matches nothing. Same hourly-analytics gate as region.</param>
    /// <param name="registrationId">Filter to traffic attributed to these registrations (hourly analytics only; a 400 while hourly reads are off). A phoneNumber filter takes precedence. Single value or comma-separated list.</param>
    public async Task<SignalHouseResponse> GetDncAnalyticsAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? brandId = null,
        string? campaignId = null,
        string? phoneNumber = null,
        string? carrier = null,
        string? startDate = null,
        string? endDate = null,
        string? channel = null,
        RequestOptions? options = null,
        string? dateBounds = null,
        string? region = null,
        string? regionScopes = null,
        string? registrationId = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "phoneNumber", phoneNumber },
            { "carrier", carrier },
            { "startDate", startDate },
            { "endDate", endDate },
            { "channel", channel },
            { "region", region },
            { "regionScopes", regionScopes },
            { "dateBounds", dateBounds },
        });

        return await _client.RequestAsync($"/message/dnc/analytics{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get paginated Do Not Call records with optional filters. Each record carries senderPhoneNumber — the
    /// first business number that received the opt-out (set once, never changed by a later STOP or a campaign
    /// move; the same value as phoneNumber), campaignId (null on subgroup-scoped records for non-US
    /// recipients), optOutKeyword — the
    /// normalized keyword that revoked consent (e.g. "stop", "opt out"; a repeat opt-out records the most
    /// recent word), null for opt-outs recorded before keyword capture shipped — and carrierFamily, the
    /// resolved carrier family of the recipient number (Default, ATT, TMobile, Verizon, USCellular,
    /// GoogleVoice, ClearSky, Interop, RogueMobile, Standard, IceWireless), copied from the inbound message that triggered the
    /// opt-out.
    /// </summary>
    /// <param name="channel">Optional channel filter: tenDLC, virtualLongCode, tollFree, or shortCode (single value or comma-separated list). Opt-outs are A2P-only, so p2p applies no filter. A tenDLC selection also includes older opt-outs with no channel.</param>
    /// <param name="dateBounds">Which contract binds startDate/endDate. "day": each bound is widened to its whole UTC day. "exact": a timestamp is bound as the instant it names and a bare date is its whole UTC day. Null: "day".</param>
    /// <param name="region">Optional region filter: US, GB, or CA (any case of "UK"/"GB"/"GBR"/"United Kingdom" is accepted for GB), single value or comma-separated list. Only accepted where the environment serves hourly analytics; elsewhere any value is rejected with a 400.</param>
    /// <param name="regionScopes">Optional JSON array of OR-ed branches, each {region, channels, brandId?, campaignId?, phoneNumber?} with channels from tenDLC, virtualLongCode, tollFree, shortCode, p2p. "[]" matches nothing. Same hourly-analytics gate as region.</param>
    /// <param name="registrationId">Filter to traffic attributed to these registrations (hourly analytics only; a 400 while hourly reads are off). A phoneNumber filter takes precedence. Single value or comma-separated list.</param>
    public async Task<SignalHouseResponse> GetDncRecordsAsync(
        string? groupId = null,
        string? subgroupId = null,
        string? brandId = null,
        string? campaignId = null,
        string? phoneNumber = null,
        string? carrier = null,
        string? startDate = null,
        string? endDate = null,
        int? page = null,
        int? limit = null,
        string? sortField = null,
        string? sortOrder = null,
        string? channel = null,
        RequestOptions? options = null,
        string? dateBounds = null,
        string? region = null,
        string? regionScopes = null,
        string? registrationId = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "subgroupId", subgroupId },
            { "brandId", brandId },
            { "campaignId", campaignId },
            { "registrationId", registrationId },
            { "phoneNumber", phoneNumber },
            { "carrier", carrier },
            { "startDate", startDate },
            { "endDate", endDate },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "sortField", sortField },
            { "sortOrder", sortOrder },
            { "channel", channel },
            { "region", region },
            { "regionScopes", regionScopes },
            { "dateBounds", dateBounds },
        });

        return await _client.RequestAsync($"/message/dnc{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Send an SMS message to one or more recipients.
    /// </summary>
    /// <param name="senderPhoneNumber">The digits-only 5-6 digit Short Code or 10+ digit long number to send from, or a READY alphanumeric sender ID (e.g. "ACME"; UK recipients and SMS only). Every message from an alphanumeric sender gets "\nOpt out: sihou.io/o/{code}" appended by the server, counted in segments.</param>
    /// <param name="recipientPhoneNumbers">A single 10+ digit phone number or a list. A Short Code sender permits exactly one recipient, and it must be a +1 (US/Canada) number (a bare 10-digit number is treated as +1); the API rejects multiple recipients or a non-+1 recipient.</param>
    /// <param name="messageBody">The SMS message text.</param>
    /// <param name="statusCallbackUrl">Optional URL to receive delivery status callbacks.</param>
    /// <param name="enableShortlink">Whether to enable URL shortening in the message body.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>
    /// The standardized response. The payload reports the send outcome via requestedRecipientCount,
    /// enqueuedCount, and failedCount. enqueuedCount is the number of recipients accepted for delivery
    /// and is 0 when the send was fully blocked (by the moderation filter or a campaign opt-out); the
    /// request still succeeds in that case, so treat enqueuedCount, not the HTTP status, as the signal
    /// that a message was accepted. Blocked recipients are returned in insertedMessages with status
    /// "FAILED"; recipients on the global Do Not Contact list are reported in dncBlockedNumbers with no
    /// message record. Each FAILED message carries errorCode, a stable machine-readable cause (campaign
    /// opt-out is "OUT"; null on success or on failures with no assigned code) that is also included on
    /// the FAILED status-callback payload.
    /// </returns>
    public Task<SignalHouseResponse> SendSmsAsync(
        string senderPhoneNumber,
        object recipientPhoneNumbers,
        string messageBody,
        string? statusCallbackUrl = null,
        bool enableShortlink = false,
        RequestOptions? options = null)
        => SendSmsAsync(senderPhoneNumber, recipientPhoneNumbers, messageBody,
            statusCallbackUrl, enableShortlink, false, options);

    /// <summary>
    /// Send an SMS message to one or more recipients with landline/inactive number filtering.
    /// </summary>
    /// <param name="senderPhoneNumber">The digits-only 5-6 digit Short Code or 10+ digit long number to send from, or a READY alphanumeric sender ID (e.g. "ACME"; UK recipients and SMS only). Every message from an alphanumeric sender gets "\nOpt out: sihou.io/o/{code}" appended by the server, counted in segments.</param>
    /// <param name="recipientPhoneNumbers">A single 10+ digit phone number or a list. A Short Code sender permits exactly one recipient, and it must be a +1 (US/Canada) number (a bare 10-digit number is treated as +1); the API rejects multiple recipients or a non-+1 recipient.</param>
    /// <param name="messageBody">The SMS message text.</param>
    /// <param name="statusCallbackUrl">Optional URL to receive delivery status callbacks.</param>
    /// <param name="enableShortlink">Whether to enable URL shortening in the message body.</param>
    /// <param name="filterLandlinesAndInactiveNumbers">Whether to filter out landline and inactive numbers before sending.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>
    /// The standardized response. The payload reports the send outcome via requestedRecipientCount,
    /// enqueuedCount, and failedCount. enqueuedCount is the number of recipients accepted for delivery
    /// and is 0 when the send was fully blocked (by the moderation filter or a campaign opt-out); the
    /// request still succeeds in that case, so treat enqueuedCount, not the HTTP status, as the signal
    /// that a message was accepted. Blocked recipients are returned in insertedMessages with status
    /// "FAILED"; recipients on the global Do Not Contact list are reported in dncBlockedNumbers with no
    /// message record. Each FAILED message carries errorCode, a stable machine-readable cause (campaign
    /// opt-out is "OUT"; null on success or on failures with no assigned code) that is also included on
    /// the FAILED status-callback payload.
    /// </returns>
    public async Task<SignalHouseResponse> SendSmsAsync(
        string senderPhoneNumber,
        object recipientPhoneNumbers,
        string messageBody,
        string? statusCallbackUrl,
        bool enableShortlink,
        bool filterLandlinesAndInactiveNumbers,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "senderPhoneNumber", senderPhoneNumber },
            { "recipientPhoneNumbers", recipientPhoneNumbers },
            { "messageBody", messageBody },
        });

        object body = filterLandlinesAndInactiveNumbers
            ? new { senderPhoneNumber, recipientPhoneNumber = recipientPhoneNumbers, messageBody, statusCallbackUrl, enableShortlink, filterLandlinesAndInactiveNumbers }
            : new { senderPhoneNumber, recipientPhoneNumber = recipientPhoneNumbers, messageBody, statusCallbackUrl, enableShortlink };

        return await _client.RequestAsync("/message/sms", HttpMethod.Post, body, options);
    }

    /// <summary>
    /// Send a P2P message via Rogue Mobile SMPP.
    /// </summary>
    /// <param name="recipientPhoneNumbers">The phone number(s) to send the message to.</param>
    /// <param name="messageBody">The body of the P2P message.</param>
    /// <param name="statusCallbackUrl">Optional URL to receive status callbacks.</param>
    /// <param name="groupId">Optional. Explicit group to send from. Defaults to the group in your auth context.</param>
    /// <param name="subgroupId">Optional. Explicit subgroup to attribute the send (and its inbound reply) to. P2P has no dedicated sender number, so pass this to record the send against a specific subgroup; it also resolves the group. Defaults to the group's oldest subgroup.</param>
    /// <param name="useSignalHouseShortlinks">Optional. When false, SignalHouse applies no link-shortening or text-spin and your pre-spun links/content are sent verbatim (bring-your-own spinner). Defaults to true.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> SendP2PAsync(
        object recipientPhoneNumbers,
        string messageBody,
        string? statusCallbackUrl = null,
        string? groupId = null,
        string? subgroupId = null,
        bool? useSignalHouseShortlinks = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "recipientPhoneNumbers", recipientPhoneNumbers },
            { "messageBody", messageBody },
        });

        return await _client.RequestAsync("/message/p2p", HttpMethod.Post, new
        {
            recipientPhoneNumber = recipientPhoneNumbers,
            messageBody,
            statusCallbackUrl,
            groupId,
            subgroupId,
            useSignalHouseShortlinks,
        }, options);
    }

    /// <summary>
    /// Get the details of a P2P batch by its ID.
    /// </summary>
    /// <param name="batchId">The ID of the P2P batch to retrieve.</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetP2PBatchAsync(
        string batchId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "batchId", batchId },
        });

        var safeBatchId = Uri.EscapeDataString(batchId);
        return await _client.RequestAsync($"/message/p2p/batches/{safeBatchId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Fetch the latest upstream carrier delivery report for a set of messages by ID. Useful for
    /// reconciling messages stuck in a non-terminal state. Outbound SMS and MMS messages sent from numbers
    /// on supported carriers are looked up upstream (reports are retained about 48 hours);
    /// every other message, including P2P messages and messages sent from numbers on other carriers, is
    /// returned with its stored status and upstreamReportAvailable false. The request fails with a 500 when
    /// the sending numbers can't be read.
    /// </summary>
    /// <param name="messageIds">The message IDs to fetch carrier delivery reports for (1-100).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetDeliveryReportsAsync(
        IEnumerable<string> messageIds,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "messageIds", messageIds },
        });

        return await _client.RequestAsync("/message/delivery-report", HttpMethod.Post, new
        {
            messageIds,
        }, options);
    }

    /// <summary>
    /// Send an MMS message to one or more recipients with optional media attachments.
    /// </summary>
    /// <param name="senderPhoneNumber">The digits-only 5-6 digit Short Code or 10+ digit long number to send from.</param>
    /// <param name="recipientPhoneNumbers">List of 10+ digit recipient phone numbers. A Short Code sender permits exactly one recipient, and it must be a +1 (US/Canada) number (a bare 10-digit number is treated as +1); the API rejects multiple recipients or a non-+1 recipient.</param>
    /// <param name="messageBody">The MMS message text.</param>
    /// <param name="mediaUrls">Optional list of media URLs to attach.</param>
    /// <param name="statusCallbackUrl">Optional URL to receive delivery status callbacks.</param>
    /// <param name="enableShortlink">Whether to enable URL shortening in the message body.</param>
    /// <param name="enableCompression">Whether to compress image attachments.</param>
    /// <param name="images">Optional list of image streams to attach.</param>
    /// <param name="imageFileNames">Filenames corresponding to each image stream.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>
    /// The standardized response. The payload reports the send outcome via requestedRecipientCount,
    /// enqueuedCount, and failedCount. enqueuedCount is the number of recipients accepted for delivery
    /// and is 0 when the send was fully blocked (by the moderation filter or a campaign opt-out); the
    /// request still succeeds in that case, so treat enqueuedCount, not the HTTP status, as the signal
    /// that a message was accepted. Blocked recipients are returned in insertedMessages with status
    /// "FAILED"; recipients on the global Do Not Contact list are reported in dncBlockedNumbers with no
    /// message record. Each FAILED message carries errorCode, a stable machine-readable cause (campaign
    /// opt-out is "OUT"; null on success or on failures with no assigned code) that is also included on
    /// the FAILED status-callback payload.
    /// </returns>
    public Task<SignalHouseResponse> SendMmsAsync(
        string senderPhoneNumber,
        List<string> recipientPhoneNumbers,
        string messageBody,
        List<string>? mediaUrls = null,
        string? statusCallbackUrl = null,
        bool enableShortlink = false,
        bool enableCompression = true,
        List<Stream>? images = null,
        List<string>? imageFileNames = null,
        RequestOptions? options = null)
        => SendMmsAsync(senderPhoneNumber, recipientPhoneNumbers, messageBody,
            mediaUrls, statusCallbackUrl, enableShortlink, enableCompression,
            false, images, imageFileNames, options);

    /// <summary>
    /// Send an MMS message to one or more recipients with optional media attachments and landline/inactive number filtering.
    /// </summary>
    /// <param name="senderPhoneNumber">The digits-only 5-6 digit Short Code or 10+ digit long number to send from.</param>
    /// <param name="recipientPhoneNumbers">List of 10+ digit recipient phone numbers. A Short Code sender permits exactly one recipient, and it must be a +1 (US/Canada) number (a bare 10-digit number is treated as +1); the API rejects multiple recipients or a non-+1 recipient.</param>
    /// <param name="messageBody">The MMS message text.</param>
    /// <param name="mediaUrls">Optional list of media URLs to attach.</param>
    /// <param name="statusCallbackUrl">Optional URL to receive delivery status callbacks.</param>
    /// <param name="enableShortlink">Whether to enable URL shortening in the message body.</param>
    /// <param name="enableCompression">Whether to compress image attachments.</param>
    /// <param name="filterLandlinesAndInactiveNumbers">Whether to filter out landline and inactive numbers before sending.</param>
    /// <param name="images">Optional list of image streams to attach.</param>
    /// <param name="imageFileNames">Filenames corresponding to each image stream.</param>
    /// <param name="options">Optional per-request overrides.</param>
    /// <returns>
    /// The standardized response. The payload reports the send outcome via requestedRecipientCount,
    /// enqueuedCount, and failedCount. enqueuedCount is the number of recipients accepted for delivery
    /// and is 0 when the send was fully blocked (by the moderation filter or a campaign opt-out); the
    /// request still succeeds in that case, so treat enqueuedCount, not the HTTP status, as the signal
    /// that a message was accepted. Blocked recipients are returned in insertedMessages with status
    /// "FAILED"; recipients on the global Do Not Contact list are reported in dncBlockedNumbers with no
    /// message record. Each FAILED message carries errorCode, a stable machine-readable cause (campaign
    /// opt-out is "OUT"; null on success or on failures with no assigned code) that is also included on
    /// the FAILED status-callback payload.
    /// </returns>
    public async Task<SignalHouseResponse> SendMmsAsync(
        string senderPhoneNumber,
        List<string> recipientPhoneNumbers,
        string messageBody,
        List<string>? mediaUrls,
        string? statusCallbackUrl,
        bool enableShortlink,
        bool enableCompression,
        bool filterLandlinesAndInactiveNumbers,
        List<Stream>? images = null,
        List<string>? imageFileNames = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "senderPhoneNumber", senderPhoneNumber },
            { "recipientPhoneNumbers", recipientPhoneNumbers },
        });

        var content = BuildMessageMultipart(
            senderPhoneNumber, recipientPhoneNumbers, messageBody,
            mediaUrls, statusCallbackUrl, enableShortlink, enableCompression,
            filterLandlinesAndInactiveNumbers, images, imageFileNames);

        return await _client.MultipartRequestAsync("/message/mms", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Send a group MMS message to one or more recipients with optional media attachments.
    /// Short Codes do not support group messaging; senderPhoneNumber must be a long number.
    /// </summary>
    /// <returns>
    /// The standardized response. The payload reports the send outcome via requestedRecipientCount,
    /// enqueuedCount, and failedCount. enqueuedCount is the number of recipients accepted for delivery
    /// and is 0 when the send was fully blocked (by the moderation filter or a campaign opt-out); the
    /// request still succeeds in that case, so treat enqueuedCount, not the HTTP status, as the signal
    /// that a message was accepted. Blocked recipients are returned in insertedMessages with status
    /// "FAILED"; recipients on the global Do Not Contact list are reported in dncBlockedNumbers with no
    /// message record. Each FAILED message carries errorCode, a stable machine-readable cause (campaign
    /// opt-out is "OUT"; null on success or on failures with no assigned code) that is also included on
    /// the FAILED status-callback payload.
    /// </returns>
    public Task<SignalHouseResponse> SendGroupMessageAsync(
        string senderPhoneNumber,
        List<string> recipientPhoneNumbers,
        string messageBody,
        List<string>? mediaUrls = null,
        string? statusCallbackUrl = null,
        bool enableShortlink = false,
        bool enableCompression = true,
        List<Stream>? images = null,
        List<string>? imageFileNames = null,
        RequestOptions? options = null)
        => SendGroupMessageAsync(senderPhoneNumber, recipientPhoneNumbers, messageBody,
            mediaUrls, statusCallbackUrl, enableShortlink, enableCompression,
            false, images, imageFileNames, options);

    /// <summary>
    /// Send a group MMS message to one or more recipients with optional media attachments and landline/inactive number filtering.
    /// Short Codes do not support group messaging; senderPhoneNumber must be a long number.
    /// </summary>
    /// <returns>
    /// The standardized response. The payload reports the send outcome via requestedRecipientCount,
    /// enqueuedCount, and failedCount. enqueuedCount is the number of recipients accepted for delivery
    /// and is 0 when the send was fully blocked (by the moderation filter or a campaign opt-out); the
    /// request still succeeds in that case, so treat enqueuedCount, not the HTTP status, as the signal
    /// that a message was accepted. Blocked recipients are returned in insertedMessages with status
    /// "FAILED"; recipients on the global Do Not Contact list are reported in dncBlockedNumbers with no
    /// message record. Each FAILED message carries errorCode, a stable machine-readable cause (campaign
    /// opt-out is "OUT"; null on success or on failures with no assigned code) that is also included on
    /// the FAILED status-callback payload.
    /// </returns>
    public async Task<SignalHouseResponse> SendGroupMessageAsync(
        string senderPhoneNumber,
        List<string> recipientPhoneNumbers,
        string messageBody,
        List<string>? mediaUrls,
        string? statusCallbackUrl,
        bool enableShortlink,
        bool enableCompression,
        bool filterLandlinesAndInactiveNumbers,
        List<Stream>? images = null,
        List<string>? imageFileNames = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "senderPhoneNumber", senderPhoneNumber },
            { "recipientPhoneNumbers", recipientPhoneNumbers },
        });

        var content = BuildMessageMultipart(
            senderPhoneNumber, recipientPhoneNumbers, messageBody,
            mediaUrls, statusCallbackUrl, enableShortlink, enableCompression,
            filterLandlinesAndInactiveNumbers, images, imageFileNames);

        return await _client.MultipartRequestAsync("/message/groupMessage", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Look up the carrier for a phone number. Always performs a fresh lookup (not cached). Billed per request.
    /// </summary>
    /// <param name="phoneNumber">The phone number to look up (10+ digits, no + prefix).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CarrierIdLookupAsync(
        string phoneNumber,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "phoneNumber", phoneNumber },
        });

        return await _client.RequestAsync("/message/carrier-id", HttpMethod.Post, new
        {
            phoneNumber,
        }, options);
    }

    private static MultipartFormDataContent BuildMessageMultipart(
        string senderPhoneNumber,
        List<string> recipientPhoneNumbers,
        string messageBody,
        List<string>? mediaUrls,
        string? statusCallbackUrl,
        bool enableShortlink,
        bool enableCompression,
        bool filterLandlinesAndInactiveNumbers,
        List<Stream>? images,
        List<string>? imageFileNames)
    {
        var content = new MultipartFormDataContent();

        if (images is not null)
        {
            for (var i = 0; i < images.Count; i++)
            {
                var streamContent = new StreamContent(images[i]);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var name = imageFileNames is not null && i < imageFileNames.Count
                    ? imageFileNames[i]
                    : $"image{i}";
                content.Add(streamContent, "images", name);
            }
        }

        content.Add(new StringContent(senderPhoneNumber), "senderPhoneNumber");
        content.Add(new StringContent(JsonSerializer.Serialize(recipientPhoneNumbers)), "recipientPhoneNumber");
        content.Add(new StringContent(messageBody), "messageBody");

        if (mediaUrls is not null && mediaUrls.Count > 0)
        {
            content.Add(new StringContent(JsonSerializer.Serialize(mediaUrls)), "mediaUrls");
        }

        if (statusCallbackUrl is not null)
        {
            content.Add(new StringContent(statusCallbackUrl), "statusCallbackUrl");
        }

        content.Add(new StringContent(enableShortlink.ToString().ToLowerInvariant()), "enableShortlink");
        content.Add(new StringContent(enableCompression.ToString().ToLowerInvariant()), "enableCompression");

        if (filterLandlinesAndInactiveNumbers)
            content.Add(new StringContent("true"), "filterLandlinesAndInactiveNumbers");

        return content;
    }
    /// <summary>
    /// Estimate Canadian or UK retail rates and total in microdollars; no charge or send. A Ready Canadian sender takes SMS or MMS
    /// to Canadian or US recipients; a UK (+44) virtual long code takes SMS only to GB mobile recipients (10-15 digits). The response
    /// region is "CA" or "GB". Unavailable unless that region is enabled for the environment. The sender may also be a READY
    /// alphanumeric sender ID (e.g. "ACME"; UK recipients, SMS only); its estimate counts the "\nOpt out: sihou.io/o/{code}"
    /// footer the server appends to every such message.
    /// </summary>
    public async Task<SignalHouseResponse> EstimateMessageAsync(string senderPhoneNumber, List<string> recipientPhoneNumbers, string messageBody, RequestOptions? options = null, string messageType = "SMS")
    {
        return await _client.RequestAsync("/message/estimate", HttpMethod.Post, new { senderPhoneNumber, recipientPhoneNumbers, messageBody, messageType }, options);
    }
}
