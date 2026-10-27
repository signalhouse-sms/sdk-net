using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains.Voice;

/// <summary>
/// Voice Analytics — aggregated call metrics for the portal Voice Analytics view
/// (Voice service <c>GET /voice/stats/voice-analytics</c>). Returns per-direction
/// summary tiles (both directions) plus byDate / byNumber / byCarrier status
/// breakdowns for charts. Distinct from <c>client.Voice.CallLogs</c>, which is the
/// raw call history. Accessed via <c>client.Voice.Analytics</c>.
/// </summary>
public sealed class AnalyticsDomain
{
    private readonly HttpClientWrapper _client;

    internal AnalyticsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get aggregated voice analytics for the current account.
    /// Optional filter keys (camelCase), all omitted when unset:
    /// <c>dateFrom</c> (ISO-8601 lower bound on call start), <c>dateTo</c> (ISO-8601
    /// upper bound), <c>groupBy</c> (<c>day</c>|<c>week</c>|<c>month</c>, byDate bucket
    /// size), <c>direction</c> (<c>INBOUND</c>|<c>OUTBOUND</c>; summary tiles always
    /// cover both, breakdowns honor it), <c>callSource</c> (comma-separated CallLog
    /// callSource list), <c>subgroupId</c>, <c>number</c> (substring match against
    /// either leg), <c>carrier</c>.
    /// Returns <c>{ success, range: { dateFrom, dateTo, days }, outbound: &lt;Summary&gt;,
    /// inbound: &lt;Summary&gt;, byDate: [&lt;Bucket&gt;], byNumber: [&lt;Bucket&gt;],
    /// byCarrier: [&lt;Bucket&gt;], byChannel: [&lt;Bucket&gt;] }</c> (byChannel = per-callSource breakdown), where each Summary carries <c>totalCalls</c>,
    /// <c>successfulCalls</c>, <c>successRate</c>, <c>avgCallsPerDay</c>,
    /// <c>avgCallDurationSeconds</c>, <c>totalCallMinutes</c>, <c>answerRate</c>,
    /// <c>abandonmentRate</c>, <c>failedCalls</c>, <c>failureRate</c>, <c>pctUnder6s</c>,
    /// <c>avgRingDurationSeconds</c>, <c>sentimentScore</c>, <c>sentimentLabel</c>, and each
    /// Bucket carries <c>key</c>, <c>total</c>, <c>completed</c>, <c>failed</c>,
    /// <c>noAnswer</c>, <c>busy</c>, <c>canceled</c>, <c>inProgress</c>,
    /// <c>totalDurationSeconds</c>, <c>avgSentimentScore</c>.
    /// <c>GET /voice/stats/voice-analytics</c>.
    /// </summary>
    /// <param name="filters">Optional camelCase filter keys (see summary). Null values are skipped.</param>
    /// <param name="options">Additional options for the request.</param>
    /// <returns>The API response.</returns>
    public async Task<SignalHouseResponse> GetAsync(IDictionary<string, object?>? filters = null, RequestOptions? options = null)
    {
        var parameters = new Dictionary<string, string?>();
        if (filters is not null)
        {
            foreach (var kvp in filters)
            {
                if (kvp.Value is not null)
                {
                    var str = kvp.Value.ToString();
                    if (!string.IsNullOrEmpty(str))
                    {
                        parameters[kvp.Key] = str;
                    }
                }
            }
        }

        var qs = HttpClientWrapper.GetQueryString(parameters);
        return await _client.RequestAsync($"/voice/stats/voice-analytics{qs}", HttpMethod.Get, options: options);
    }
}
