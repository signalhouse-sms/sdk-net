namespace SignalHouse.SDK.Models;

/// <summary>
/// Per-request overrides that can be passed to any SDK method.
/// </summary>
public class RequestOptions
{
    /// <summary>
    /// Override the default Bearer token for this single request.
    /// Useful for impersonation or login flows that return a new token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Additional HTTP headers to include in the request.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Sent as the Idempotency-Key header; a retry with the same key and request replays the first response
    /// instead of repeating it.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}
