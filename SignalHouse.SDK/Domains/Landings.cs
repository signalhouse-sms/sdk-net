using System.Net.Http.Headers;
using System.Text.Json;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Landing page management operations (create and update use multipart/form-data).
/// </summary>
public sealed class LandingsDomain
{
    private readonly HttpClientWrapper _client;

    internal LandingsDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Get details of a landing page by its ID.
    /// </summary>
    public async Task<SignalHouseResponse> GetLandingsAsync(string landingId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "landingId", landingId } });

        var safeLandingId = Uri.EscapeDataString(landingId);
        return await _client.RequestAsync($"/landing/{safeLandingId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a public (published) landing page by its ID. This endpoint is public and does not require authentication.
    /// </summary>
    public async Task<SignalHouseResponse> GetPublicLandingAsync(string landingId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "landingId", landingId } });

        var safeLandingId = Uri.EscapeDataString(landingId);
        return await _client.RequestAsync($"/landing/public/{safeLandingId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create a new landing page (multipart upload).
    /// </summary>
    /// <param name="landingData">Key/value pairs for the landing page fields (brandId, description, colors, etc.).
    /// Optionally set registrationType to "TOLL_FREE" and supply tollFree
    /// {useCases, channels, consentDisclosure, consentBoxes[{useCase, channel, text}]} to build a Toll-Free page,
    /// which renders one consent checkbox per use case per channel. Set useBrandTemplate to true (Toll-Free
    /// only) to build the page from the brand's landing page template, taking its colors, logo and webhook
    /// URL from there; no logo file is then required.</param>
    /// <param name="file">Optional logo image stream.</param>
    /// <param name="fileName">Filename for the logo image (required when file is provided).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> CreateLandingAsync(
        Dictionary<string, object> landingData,
        Stream? file = null,
        string? fileName = null,
        RequestOptions? options = null)
    {
        var content = BuildMultipartContent(landingData, file, fileName);
        return await _client.MultipartRequestAsync("/landing", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Update an existing landing page (multipart upload). Set useBrandTemplate to true in landingData
    /// (Toll-Free only) to re-copy the brand's landing page template into this page rather than creating a
    /// second one.
    /// </summary>
    public async Task<SignalHouseResponse> UpdateLandingAsync(
        string landingId,
        Dictionary<string, object> landingData,
        Stream? file = null,
        string? fileName = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "landingId", landingId } });

        var safeLandingId = Uri.EscapeDataString(landingId);
        var content = BuildMultipartContent(landingData, file, fileName);
        return await _client.MultipartRequestAsync($"/landing/{safeLandingId}", HttpMethod.Put, content, options);
    }

    /// <summary>
    /// Get a landing page by its associated brand ID.
    /// </summary>
    public async Task<SignalHouseResponse> GetLandingByBrandIdAsync(string brandId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        return await _client.RequestAsync($"/landing/brand/{safeBrandId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get a brand's Toll-Free landing page template — the reusable look every page the brand generates
    /// snapshots. Returns an empty array when the brand has no template.
    /// </summary>
    public async Task<SignalHouseResponse> GetLandingTemplateAsync(string brandId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        return await _client.RequestAsync($"/landing/template/{safeBrandId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Create or replace a brand's Toll-Free landing page template (multipart upload). Editing a template
    /// never alters a page that already exists, so a URL already given to a carrier keeps rendering what
    /// was declared.
    /// </summary>
    /// <param name="brandId">The brand the template belongs to.</param>
    /// <param name="templateData">userDescription (the customer's own sentence, not the finished About
    /// paragraph — the services clause is added per campaign), the four colors, webhookUrl.</param>
    /// <param name="file">Logo image stream. Required the first time only; omit it on a later save to keep
    /// the stored logo.</param>
    /// <param name="fileName">Filename for the logo image (required when file is provided).</param>
    /// <param name="options">Optional per-request overrides.</param>
    public async Task<SignalHouseResponse> UpsertLandingTemplateAsync(
        string brandId,
        Dictionary<string, object> templateData,
        Stream? file = null,
        string? fileName = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "brandId", brandId } });

        var safeBrandId = Uri.EscapeDataString(brandId);
        var content = BuildMultipartContent(templateData, file, fileName);
        return await _client.MultipartRequestAsync($"/landing/template/{safeBrandId}", HttpMethod.Put, content, options);
    }

    /// <summary>
    /// Delete a landing page by its ID.
    /// </summary>
    public async Task<SignalHouseResponse> DeleteLandingAsync(string landingId, RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "landingId", landingId } });

        var safeLandingId = Uri.EscapeDataString(landingId);
        return await _client.RequestAsync($"/landing/{safeLandingId}", HttpMethod.Delete, options: options);
    }

    private static MultipartFormDataContent BuildMultipartContent(
        Dictionary<string, object> data,
        Stream? file,
        string? fileName)
    {
        var content = new MultipartFormDataContent();

        if (file is not null)
        {
            var streamContent = new StreamContent(file);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName ?? "file");
        }

        foreach (var kvp in data)
        {
            if (kvp.Value is null) continue;

            var value = kvp.Value is string s
                ? s
                : JsonSerializer.Serialize(kvp.Value);

            content.Add(new StringContent(value), kvp.Key);
        }

        return content;
    }
}
