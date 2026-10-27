namespace SignalHouse.SDK.Exceptions;

/// <summary>
/// Base exception for all errors raised by the SignalHouse SDK.
/// </summary>
public class SignalHouseException : Exception
{
    /// <summary>
    /// The HTTP status code associated with the error, if available.
    /// </summary>
    public int? StatusCode { get; }

    public SignalHouseException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public SignalHouseException(string message, Exception innerException, int? statusCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
