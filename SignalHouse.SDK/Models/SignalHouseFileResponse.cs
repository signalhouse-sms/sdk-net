namespace SignalHouse.SDK.Models;

/// <summary>Response from a file download, without decoding the file as text or JSON.</summary>
public sealed class SignalHouseFileResponse
{
    /// <summary>Whether the server returned HTTP 2xx.</summary>
    public bool Success { get; set; }

    /// <summary>The exact file bytes on success; null on failure.</summary>
    public byte[]? Data { get; set; }

    /// <summary>The API error message or HTTP reason phrase on failure.</summary>
    public string? Error { get; set; }

    /// <summary>The HTTP status code.</summary>
    public int Status { get; set; }
}
