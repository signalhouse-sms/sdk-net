using System.Net.Http.Headers;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;

namespace SignalHouse.SDK.Domains;

/// <summary>
/// Compliance cases on /compliance. Signal House opens a case when a carrier suspends one of your objects, a carrier
/// escalates a message, or Signal House needs something from you. You read your cases, answer their requirements and
/// upload files; only Signal House opens, works and closes them.
/// </summary>
public sealed class ComplianceDomain
{
    private readonly HttpClientWrapper _client;

    internal ComplianceDomain(HttpClientWrapper client)
    {
        _client = client;
    }

    /// <summary>
    /// List the compliance cases you can see, newest first. counts.waitingOnYou is the open cases with requirements
    /// still to answer. Null arguments are left out of the query. Roles: api, admin, developer, billing, user.
    /// </summary>
    /// <param name="groupId">A group you can view; null uses your active group.</param>
    /// <param name="status">OPEN, LIFT_REQUESTED or CLOSED.</param>
    /// <param name="stage">new, customer, ready, lift_requested or closed.</param>
    /// <param name="objectType">CAMPAIGN, BRAND, NUMBER, RCS_AGENT or SUBGROUP.</param>
    /// <param name="objectId">The object's ID.</param>
    /// <param name="subgroupId">Only cases in this subgroup.</param>
    /// <param name="suspended">true for cases with an open suspension or hold, false for the rest.</param>
    /// <param name="from">Opened on or after this ISO 8601 time.</param>
    /// <param name="to">Opened on or before this ISO 8601 time.</param>
    /// <param name="sort">createdAt (the default), fixBy or severity.</param>
    /// <param name="page">Page number, from 1.</param>
    /// <param name="limit">Cases per page, 1-200.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ data, total, page, limit, totalPages, counts: { open, waitingOnYou } }.</returns>
    public async Task<SignalHouseResponse> GetCasesAsync(
        string? groupId = null,
        string? status = null,
        string? stage = null,
        string? objectType = null,
        string? objectId = null,
        string? subgroupId = null,
        bool? suspended = null,
        string? from = null,
        string? to = null,
        string? sort = null,
        int? page = null,
        int? limit = null,
        RequestOptions? options = null)
    {
        var qs = HttpClientWrapper.GetQueryString(new Dictionary<string, string?>
        {
            { "groupId", groupId },
            { "status", status },
            { "stage", stage },
            { "objectType", objectType },
            { "objectId", objectId },
            { "subgroupId", subgroupId },
            { "suspended", suspended?.ToString().ToLowerInvariant() },
            { "from", from },
            { "to", to },
            { "sort", sort },
            { "page", page?.ToString() },
            { "limit", limit?.ToString() },
        });
        return await _client.RequestAsync($"/compliance/cases{qs}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Get one compliance case: its stage, suspensions, tickets and requirements.
    /// Roles: api, admin, developer, billing, user.
    /// </summary>
    /// <param name="caseId">The case ID (e.g. "CASE-…").</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The case.</returns>
    /// <exception cref="ValidationException">caseId is null.</exception>
    public async Task<SignalHouseResponse> GetCaseAsync(
        string caseId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "caseId", caseId } });

        var safeCaseId = Uri.EscapeDataString(caseId);
        return await _client.RequestAsync($"/compliance/cases/{safeCaseId}", HttpMethod.Get, options: options);
    }

    /// <summary>
    /// Answer one requirement with a note, files, or both. A RECORDS requirement needs a file or a note. Null
    /// arguments are left out of the body. Roles: api, admin, developer, user.
    /// </summary>
    /// <param name="caseId">The case ID (e.g. "CASE-…").</param>
    /// <param name="requirementId">The requirement ID (e.g. "CASE-…-n").</param>
    /// <param name="answer">What you did, up to 2,000 characters.</param>
    /// <param name="fileIds">Up to 10 fileIds from <see cref="UploadFileAsync"/>, on this case.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>The case.</returns>
    /// <exception cref="ValidationException">caseId or requirementId is null.</exception>
    public async Task<SignalHouseResponse> AnswerRequirementAsync(
        string caseId,
        string requirementId,
        string? answer = null,
        List<string>? fileIds = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "caseId", caseId },
            { "requirementId", requirementId },
        });

        var body = new Dictionary<string, object?>();
        if (answer is not null)
        {
            body["answer"] = answer;
        }
        if (fileIds is not null)
        {
            body["fileIds"] = fileIds;
        }

        var safeCaseId = Uri.EscapeDataString(caseId);
        var safeRequirementId = Uri.EscapeDataString(requirementId);
        return await _client.RequestAsync($"/compliance/cases/{safeCaseId}/requirements/{safeRequirementId}", HttpMethod.Put, body, options);
    }

    /// <summary>
    /// Upload a file to a compliance case (multipart/form-data): CSV, XLSX, PDF, PNG, JPG or TXT, up to 10 MB. Pass
    /// the returned fileId in fileIds when you answer the requirement. Roles: api, admin, developer, user.
    /// </summary>
    /// <param name="caseId">The case the file answers.</param>
    /// <param name="file">The file stream to upload.</param>
    /// <param name="fileName">Filename for the upload. Defaults to the name of a FileStream, or "file" for any other stream. The API recognizes CSV and TXT files by the ".csv" or ".txt" extension.</param>
    /// <param name="requirementId">The requirement the file answers; null leaves it out.</param>
    /// <param name="options">Additional request options.</param>
    /// <returns>{ fileId, fileName, mimeType, size }.</returns>
    /// <exception cref="ValidationException">caseId or file is null.</exception>
    public async Task<SignalHouseResponse> UploadFileAsync(
        string caseId,
        Stream file,
        string? fileName = null,
        string? requirementId = null,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?>
        {
            { "caseId", caseId },
            { "file", file },
        });

        var content = new MultipartFormDataContent();
        content.Add(new StringContent(caseId), "caseId");
        if (!string.IsNullOrEmpty(requirementId))
        {
            content.Add(new StringContent(requirementId), "requirementId");
        }
        var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName ?? (file is FileStream fileStream ? Path.GetFileName(fileStream.Name) : "file"));

        return await _client.MultipartRequestAsync("/compliance/files", HttpMethod.Post, content, options);
    }

    /// <summary>
    /// Download a compliance file. The API sends the file's bytes with its MIME type.
    /// Roles: api, admin, developer, billing, user.
    /// </summary>
    /// <param name="fileId">The fileId <see cref="UploadFileAsync"/> returned.</param>
    /// <param name="options">Additional request options.</param>
    /// <exception cref="ValidationException">fileId is null.</exception>
    /// <returns>The file's exact bytes in Data, or a normalized API error.</returns>
    public async Task<SignalHouseFileResponse> GetFileAsync(
        string fileId,
        RequestOptions? options = null)
    {
        HttpClientWrapper.Require(new Dictionary<string, object?> { { "fileId", fileId } });

        var safeFileId = Uri.EscapeDataString(fileId);
        return await _client.FileRequestAsync($"/compliance/files/{safeFileId}", options);
    }
}
