using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// URL shortlink operations.
/// </summary>
public sealed class ShortlinksDomain
{
    private readonly HttpClientWrapper _client;

    internal ShortlinksDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get the redirect URL for a shortlink by its ID.
    /// </summary>
    public async Task<SignalHouseResponse> GetShortlinkRedirectAsync(string shortlinkId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "shortlinkId", shortlinkId } });

        var safeShortlinkId = Uri.EscapeDataString(shortlinkId);
        return await _client.RequestAsync($"/shortlink/redirect/{safeShortlinkId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get the recipient opt-out details ({code, senderName, maskedRecipient, optedOut}) behind an
    /// alphanumeric-sender opt-out link (sihou.io/o/{code}). Public; no auth required.
    /// </summary>
    public async Task<SignalHouseResponse> GetOptOutAsync(string code, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "code", code } });

        var safeCode = Uri.EscapeDataString(code);
        return await _client.RequestAsync($"/shortlink/optout/{safeCode}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Confirm the recipient opt-out behind an alphanumeric-sender opt-out link; idempotent.
    /// Public; no auth required.
    /// </summary>
    public async Task<SignalHouseResponse> ConfirmOptOutAsync(string code, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "code", code } });

        var safeCode = Uri.EscapeDataString(code);
        return await _client.RequestAsync($"/shortlink/optout/{safeCode}", HttpMethod.Post, options: options);
    }

    /// <summary>
    /// Get shortlink details with optional filters and pagination.
    /// </summary>
    public async Task<SignalHouseResponse> GetShortlinkAsync(
        string? shortlinkId = null,
        string? messageId = null,
        string? phoneNumber = null,
        string? campaignId = null,
        string? brandId = null,
        string? subgroupId = null,
        string? groupId = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "shortlinkId", shortlinkId },
            { "messageId", messageId },
            { "phoneNumber", phoneNumber },
            { "campaignId", campaignId },
            { "brandId", brandId },
            { "subgroupId", subgroupId },
            { "groupId", groupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });

        return await _client.RequestAsync($"/shortlink{qs}", HttpMethod.Get, options: options);
    }
}
