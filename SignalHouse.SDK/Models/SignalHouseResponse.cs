using System.Text.Json;

namespace SignalHouse.SDK.Models;

/// <summary>
/// Standardized response wrapper returned by all SDK methods.
/// </summary>
public class SignalHouseResponse
{
    /// <summary>
    /// Whether the request completed successfully (HTTP 2xx).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The parsed JSON payload from the response body. Null when the response has no body.
    /// </summary>
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Error message extracted from the response, or the HTTP reason phrase on failure.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The HTTP status code returned by the server.
    /// </summary>
    public int Status { get; set; }
}
