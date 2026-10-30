using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK;

/// <summary>
/// Internal HTTP transport layer used by all domain classes.
/// Handles authentication, serialization, and response normalization.
/// </summary>
public sealed class HttpClientWrapper
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    internal HttpClientWrapper(string apiKey, string baseUrl, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// The API base URL this client was configured with, without a trailing slash.
    /// </summary>
    internal string BaseUrl => _baseUrl;

    /// <summary>
    /// Send a JSON-body request and return a standardized response.
    /// </summary>
    public async Task<SignalHouseResponse> RequestAsync(
        string path,
        HttpMethod method,
        object? body = null,
        RequestOptions? options = null)
    {
        var url = $"{_baseUrl}{path}";
        using var request = new HttpRequestMessage(method, url);

        ApplyHeaders(request, options);

        if (body is not null && method != HttpMethod.Get)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await SendAsync(request);
    }

    /// <summary>
    /// Send a multipart/form-data request and return a standardized response.
    /// </summary>
    public async Task<SignalHouseResponse> MultipartRequestAsync(
        string path,
        HttpMethod method,
        MultipartFormDataContent content,
        RequestOptions? options = null)
    {
        var url = $"{_baseUrl}{path}";
        using var request = new HttpRequestMessage(method, url);

        ApplyHeaders(request, options);
        request.Content = content;

        return await SendAsync(request);
    }

    /// <summary>
    /// Build a query string from a dictionary of parameters, skipping null values.
    /// </summary>
    public static string GetQueryString(Dictionary<string, string?> parameters)
    {
        var pairs = new List<string>();
        foreach (var kvp in parameters)
        {
            if (kvp.Value is not null)
            {
                pairs.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
        }
        return pairs.Count > 0 ? $"?{string.Join("&", pairs)}" : string.Empty;
    }

    /// <summary>
    /// Validate that required fields are not null. Throws <see cref="ValidationException"/> on the first missing field.
    /// </summary>
    public static void Require(Dictionary<string, object?> fields)
    {
        foreach (var kvp in fields)
        {
            if (kvp.Value is null)
            {
                throw new ValidationException(kvp.Key);
            }
        }
    }

    private void ApplyHeaders(HttpRequestMessage request, RequestOptions? options)
    {
        var token = options?.Token ?? _apiKey;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (options?.Headers is not null)
        {
            foreach (var header in options.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!string.IsNullOrEmpty(options?.IdempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", options.IdempotencyKey);
        }
    }

    private async Task<SignalHouseResponse> SendAsync(HttpRequestMessage request)
    {
        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new SignalHouseException($"HTTP request failed: {ex.Message}", ex);
        }

        using (httpResponse)
        {
            var response = new SignalHouseResponse
            {
                Status = (int)httpResponse.StatusCode,
                Success = httpResponse.IsSuccessStatusCode,
            };

            var responseBody = await httpResponse.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement.Clone();

                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var errorProp))
                    {
                        response.Error = errorProp.ValueKind == JsonValueKind.String
                            ? errorProp.GetString()
                            : errorProp.GetRawText();
                    }

                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var msgProp) && response.Error is null)
                    {
                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            response.Error = msgProp.ValueKind == JsonValueKind.String
                                ? msgProp.GetString()
                                : msgProp.GetRawText();
                        }
                    }

                    response.Data = root;
                }
                catch (JsonException)
                {
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        response.Error = responseBody;
                    }
                }
            }

            if (!httpResponse.IsSuccessStatusCode && response.Error is null)
            {
                response.Error = httpResponse.ReasonPhrase;
            }

            return response;
        }
    }
}
