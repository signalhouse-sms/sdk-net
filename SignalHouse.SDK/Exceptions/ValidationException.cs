namespace SignalHouse.SDK.Exceptions;

/// <summary>
/// Thrown when a required parameter is null or missing before a request is sent.
/// </summary>
public class ValidationException : SignalHouseException
{
    /// <summary>
    /// The name of the missing or invalid parameter.
    /// </summary>
    public string ParameterName { get; }

    public ValidationException(string parameterName)
        : base($"Missing required parameter: {parameterName}", statusCode: 400)
    {
        ParameterName = parameterName;
    }
}
