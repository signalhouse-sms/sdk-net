using System.Globalization;
using System.Net.Http.Headers;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Versioned message templates on the /template door. Every call is 404 while the API's RCS switch is
/// off. Template bodies are the raw RcsTemplateBody, sent verbatim.
/// </summary>
public sealed class TemplatesDomain
{
    private readonly HttpClientWrapper _client;

    internal TemplatesDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// Create a template with draft version 1. Returns { head, version } (HTTP 201).
    /// </summary>
    /// <param name="groupId">The owning group.</param>
    /// <param name="name">1-120 characters, unique per group and channel.</param>
    /// <param name="body">The raw RcsTemplateBody (a Dictionary, JsonElement or JsonNode), sent verbatim.</param>
    /// <param name="tag">Optional tag, 1-60 characters.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The new template head and draft version.</returns>
    public async Task<SignalHouseResponse> CreateAsync(
        string groupId,
        string name,
        object body,
        string? tag = null,
        RequestOptions? options = null)
    {
        var payload = TemplateRequests.CreateBody(groupId, name, body, tag);
        return await _client.RequestAsync("/template", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// List template summaries as { data, total, page, limit }.
    /// </summary>
    /// <param name="groupId">The owning group.</param>
    /// <param name="state">Optional DRAFT, PUBLISHED or ARCHIVED.</param>
    /// <param name="channel">Optional "rcs".</param>
    /// <param name="tag">Optional tag filter.</param>
    /// <param name="search">Optional name search.</param>
    /// <param name="sortBy">Optional name, createdAt or updatedAt.</param>
    /// <param name="sortOrder">Optional asc or desc.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page (default 25, max 100).</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>A page of template summaries.</returns>
    public async Task<SignalHouseResponse> ListAsync(
        string groupId,
        string? state = null,
        string? channel = null,
        string? tag = null,
        string? search = null,
        string? sortBy = null,
        string? sortOrder = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = TemplateRequests.ListQuery(groupId, state, channel, tag, search, sortBy, sortOrder, page, limit);
        return await _client.RequestAsync($"/template{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get { head, version } for one template version. An explicit version never falls back (404).
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="groupId">The owning group.</param>
    /// <param name="version">A positive integer, "draft" or "published"; null returns the published version, else the draft.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The template head and version.</returns>
    public async Task<SignalHouseResponse> GetAsync(
        string templateId,
        string groupId,
        string? version = null,
        RequestOptions? options = null)
    {
        var qs = TemplateRequests.VersionQuery(templateId, groupId, version);
        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/template/{safeTemplateId}{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Edit the draft (creating one from the published version if needed). Returns { head, version }. Writes apply
    /// directly (last write wins). An archived template is a 409 TEMPLATE_ARCHIVED; a name already in use is a 409
    /// TEMPLATE_NAME_TAKEN.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="groupId">The owning group.</param>
    /// <param name="name">Optional new name.</param>
    /// <param name="tag">Optional new tag; null leaves it unchanged unless clearTag is set.</param>
    /// <param name="clearTag">Send tag: null to clear it.</param>
    /// <param name="body">Optional full replacement RcsTemplateBody, sent verbatim.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The template head and draft version.</returns>
    public async Task<SignalHouseResponse> UpdateAsync(
        string templateId,
        string groupId,
        string? name = null,
        string? tag = null,
        bool clearTag = false,
        object? body = null,
        RequestOptions? options = null)
    {
        var payload = TemplateRequests.UpdateBody(templateId, groupId, name, tag, clearTag, body);
        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/template/{safeTemplateId}", HttpMethod.Put, payload, options);
    }

    /// <summary>
    /// Publish the current draft. Returns { head, version }. An incomplete draft is a 400 TEMPLATE_INVALID carrying errors
    /// and warnings; no draft is a 409 NO_DRAFT; a draft that changed while it was being published is a 409 DRAFT_CHANGED
    /// (publish again).
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="groupId">The owning group.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The template head and published version.</returns>
    public async Task<SignalHouseResponse> PublishAsync(
        string templateId,
        string groupId,
        RequestOptions? options = null)
    {
        var payload = TemplateRequests.PublishBody(templateId, groupId);
        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/template/{safeTemplateId}/publish", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// Archive a template. Returns its head.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="groupId">The owning group.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The archived template head.</returns>
    public async Task<SignalHouseResponse> ArchiveAsync(
        string templateId,
        string groupId,
        RequestOptions? options = null)
    {
        var payload = TemplateRequests.ArchiveBody(templateId, groupId);
        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/template/{safeTemplateId}/archive", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// List a template's version summaries (no bodies) as { data, total, page, limit }.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="groupId">The owning group.</param>
    /// <param name="page">Page number.</param>
    /// <param name="limit">Items per page.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>A page of version summaries.</returns>
    public async Task<SignalHouseResponse> VersionsAsync(
        string templateId,
        string groupId,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = TemplateRequests.VersionsQuery(templateId, groupId, page, limit);
        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/template/{safeTemplateId}/versions{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Validate exactly one of a body or a stored version without writing. Returns { valid, contentHash,
    /// validatorVersion, errors, warnings }.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="groupId">The owning group.</param>
    /// <param name="body">A raw RcsTemplateBody to validate.</param>
    /// <param name="version">A positive integer (sent as a JSON number), "draft" or "published".</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ValidationException">Neither or both of body and version are set.</exception>
    public async Task<SignalHouseResponse> ValidateAsync(
        string templateId,
        string groupId,
        object? body = null,
        string? version = null,
        RequestOptions? options = null)
    {
        var payload = TemplateRequests.ValidateBody(templateId, groupId, body, version);
        var safeTemplateId = Uri.EscapeDataString(templateId);
        return await _client.RequestAsync($"/template/{safeTemplateId}/validate", HttpMethod.Post, payload, options);
    }

    /// <summary>
    /// Get the block kinds, action types, limits and media rules the validator enforces.
    /// </summary>
    /// <param name="groupId">The calling group.</param>
    /// <param name="channel">Optional "rcs" (the server default).</param>
    /// <param name="provider">Optional "infobip" or "sinch"; adds provider limits and unsupported features.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The capabilities.</returns>
    public async Task<SignalHouseResponse> CapabilitiesAsync(
        string groupId,
        string? channel = null,
        string? provider = null,
        RequestOptions? options = null)
    {
        var qs = TemplateRequests.CapabilitiesQuery(groupId, channel, provider);
        return await _client.RequestAsync($"/template/capabilities{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Upload one template media file (multipart fields file + groupId). Returns the MediaAsset with its public URL (HTTP 201).
    /// </summary>
    /// <param name="groupId">The owning group.</param>
    /// <param name="file">The file content.</param>
    /// <param name="fileName">The file name sent with the upload.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The stored media asset.</returns>
    public async Task<SignalHouseResponse> UploadMediaAsync(
        string groupId,
        Stream file,
        string fileName,
        RequestOptions? options = null)
    {
        var content = TemplateRequests.MediaContent(groupId, file, fileName);
        return await _client.MultipartRequestAsync("/template/media", HttpMethod.Post, content, options);
    }
}

/// <summary>
/// Request builders for the /template door.
/// </summary>
internal static class TemplateRequests
{
    /// <summary>
    /// Validate the identifiers every per-template call needs.
    /// </summary>
    /// <exception cref="ValidationException">templateId or groupId is null.</exception>
    private static void RequireTemplate(string templateId, string groupId)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "templateId", templateId }, { "groupId", groupId } });
    }

    /// <summary>
    /// Build a create body.
    /// </summary>
    /// <exception cref="ValidationException">groupId, name or body is null.</exception>
    internal static Dictionary<string, object?> CreateBody(
        string groupId,
        string name,
        object body,
        string? tag)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "name", name }, { "body", body } });

        var payload = new Dictionary<string, object?> { { "groupId", groupId }, { "name", name }, { "body", body } };
        if (tag is not null)
        {
            payload["tag"] = tag;
        }
        return payload;
    }

    /// <summary>
    /// Build the list query string; null filters are dropped.
    /// </summary>
    /// <exception cref="ValidationException">groupId is null.</exception>
    internal static string ListQuery(
        string groupId,
        string? state,
        string? channel,
        string? tag,
        string? search,
        string? sortBy,
        string? sortOrder,
        int? page,
        int? limit)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });

        return HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "state", state },
            { "channel", channel },
            { "tag", tag },
            { "search", search },
            { "sortBy", sortBy },
            { "sortOrder", sortOrder },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });
    }

    /// <summary>
    /// Build the groupId + version selector query for a per-template read.
    /// </summary>
    /// <exception cref="ValidationException">templateId or groupId is null.</exception>
    internal static string VersionQuery(string templateId, string groupId, string? version)
    {
        RequireTemplate(templateId, groupId);
        return HttpClientWrapper.GetQueryString(new Dictionary<string, string?> { { "groupId", groupId }, { "version", version } });
    }

    /// <summary>
    /// Build the version-list query string.
    /// </summary>
    /// <exception cref="ValidationException">templateId or groupId is null.</exception>
    internal static string VersionsQuery(string templateId, string groupId, int? page, int? limit)
    {
        RequireTemplate(templateId, groupId);
        return HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });
    }

    /// <summary>
    /// Build the update body.
    /// </summary>
    /// <exception cref="ValidationException">templateId or groupId is null.</exception>
    internal static Dictionary<string, object?> UpdateBody(
        string templateId,
        string groupId,
        string? name,
        string? tag,
        bool clearTag,
        object? body)
    {
        RequireTemplate(templateId, groupId);

        var payload = new Dictionary<string, object?> { { "groupId", groupId } };
        if (name is not null)
        {
            payload["name"] = name;
        }
        if (clearTag)
        {
            payload["tag"] = null;
        }
        else if (tag is not null)
        {
            payload["tag"] = tag;
        }
        if (body is not null)
        {
            payload["body"] = body;
        }
        return payload;
    }

    /// <summary>
    /// Build the publish body.
    /// </summary>
    /// <exception cref="ValidationException">templateId or groupId is null.</exception>
    internal static Dictionary<string, object?> PublishBody(string templateId, string groupId)
    {
        RequireTemplate(templateId, groupId);
        return new Dictionary<string, object?> { { "groupId", groupId } };
    }

    /// <summary>
    /// Build the archive body.
    /// </summary>
    /// <exception cref="ValidationException">templateId or groupId is null.</exception>
    internal static Dictionary<string, object?> ArchiveBody(string templateId, string groupId)
    {
        RequireTemplate(templateId, groupId);
        return new Dictionary<string, object?> { { "groupId", groupId } };
    }

    /// <summary>
    /// Build the validate body from exactly one of a body or a version selector; a numeric selector is sent as a number.
    /// </summary>
    /// <exception cref="ValidationException">An identifier is null, or neither or both of body and version are set.</exception>
    internal static Dictionary<string, object?> ValidateBody(string templateId, string groupId, object? body, string? version)
    {
        RequireTemplate(templateId, groupId);
        if ((body is null) == string.IsNullOrEmpty(version))
        {
            throw new ValidationException("exactly one of body or version");
        }

        var payload = new Dictionary<string, object?> { { "groupId", groupId } };
        if (body is not null)
        {
            payload["body"] = body;
        }
        else if (long.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            payload["version"] = number;
        }
        else
        {
            payload["version"] = version;
        }
        return payload;
    }

    /// <summary>
    /// Build the capabilities query string.
    /// </summary>
    /// <exception cref="ValidationException">groupId is null.</exception>
    internal static string CapabilitiesQuery(string groupId, string? channel, string? provider)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId } });
        return HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "channel", channel },
            { "provider", provider },
        });
    }

    /// <summary>
    /// Build the multipart file + groupId upload form.
    /// </summary>
    /// <exception cref="ValidationException">groupId, file or fileName is null.</exception>
    internal static MultipartFormDataContent MediaContent(string groupId, Stream file, string fileName)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "groupId", groupId }, { "file", file }, { "fileName", fileName } });

        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(groupId), "groupId");
        return content;
    }
}
