using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// In-app notification management operations.
/// </summary>
public sealed class NotificationsDomain
{
    private readonly HttpClientWrapper _client;

    internal NotificationsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get notifications by ID or by group ID with optional filters.
    /// At least one of <paramref name="id"/> or <paramref name="groupId"/> is required.
    /// </summary>
    /// <param name="id">Notification ID.</param>
    /// <param name="groupId">Group ID to fetch notifications for.</param>
    /// <param name="page">Page number (min 1, default 1).</param>
    /// <param name="limit">Results per page (min 1, max 100, default 10).</param>
    /// <param name="status">Filter by status: "READ" or "UNREAD".</param>
    /// <param name="eventTypes">Event types to filter by (comma-separated string or JSON array).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> GetNotificationsAsync(
        string? id = null,
        string? groupId = null,
        int? page = null,
        int? limit = null,
        string? status = null,
        object? eventTypes = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id or groupId", (object?)id ?? groupId } });

        var eventTypesStr = eventTypes switch
        {
            string s => s,
            IEnumerable<string> list => string.Join(",", list),
            _ => null,
        };

        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "id", id },
            { "groupId", groupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
            { "status", status },
            { "eventTypes", eventTypesStr },
        });

        return await _client.RequestAsync($"/notification{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Update the status of one or more notifications.
    /// </summary>
    /// <param name="ids">A single notification ID string or a list of notification IDs.</param>
    /// <param name="status">New status: "READ" or "UNREAD".</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> UpdateNotificationStatusAsync(
        object ids,
        string status,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "ids", ids }, { "status", status } });

        return await _client.RequestAsync("/notification/status", HttpMethod.Put, new { ids, status }, options);
    }

    /// <summary>
    /// Delete a notification by its ID.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteNotificationAsync(string id, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "id", id } });

        var safeId = Uri.EscapeDataString(id);
        return await _client.RequestAsync($"/notification/{safeId}", HttpMethod.Delete, options: options);
    }
}
