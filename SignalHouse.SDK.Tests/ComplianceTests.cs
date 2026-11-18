using System.Net;
using System.Text;
using System.Text.Json;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;
using Xunit;

namespace SignalHouse.SDK.Tests;

public sealed class ComplianceTests
{
    private const string BaseUrl = "https://example.invalid";

    [Fact]
    public async Task GetCasesAsync_sends_only_the_given_filters()
    {
        var (client, handler) = CreateClient();

        await client.Compliance.GetCasesAsync(status: "OPEN", objectType: "CAMPAIGN", suspended: false, from: "2026-11-01T00:00:00Z", page: 2, limit: 50);
        await client.Compliance.GetCasesAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal("GET", request.Method));
        Assert.Equal(
            $"{BaseUrl}/compliance/cases?status=OPEN&objectType=CAMPAIGN&suspended=false&from=2026-11-01T00%3A00%3A00Z&page=2&limit=50",
            handler.Requests[0].Url);
        Assert.Equal($"{BaseUrl}/compliance/cases", handler.Requests[1].Url);
    }

    [Fact]
    public async Task GetCaseAsync_escapes_the_case_id()
    {
        var (client, handler) = CreateClient();

        await client.Compliance.GetCaseAsync("CASE-1/x");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("GET", request.Method);
        Assert.Equal($"{BaseUrl}/compliance/cases/CASE-1%2Fx", request.Url);
    }

    [Fact]
    public async Task AnswerRequirementAsync_sends_only_the_given_keys()
    {
        var (client, handler) = CreateClient();

        await client.Compliance.AnswerRequirementAsync("CASE-1", "CASE-1-2", answer: "Removed the number");
        await client.Compliance.AnswerRequirementAsync("CASE-1", "CASE-1-2", fileIds: ["CF1", "CF2"]);
        await client.Compliance.AnswerRequirementAsync("CASE-1", "CASE-1-2");

        Assert.Equal(3, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("PUT", request.Method);
            Assert.Equal($"{BaseUrl}/compliance/cases/CASE-1/requirements/CASE-1-2", request.Url);
        });
        Assert.Equal("{\"answer\":\"Removed the number\"}", handler.Requests[0].Body);
        Assert.Equal("{\"fileIds\":[\"CF1\",\"CF2\"]}", handler.Requests[1].Body);
        Assert.Equal("{}", handler.Requests[2].Body);
    }

    [Fact]
    public async Task UploadFileAsync_sends_the_form_fields_and_the_file()
    {
        var (client, handler) = CreateClient();

        await client.Compliance.UploadFileAsync("CASE-1", new MemoryStream(Encoding.UTF8.GetBytes("a,b\n1,2")), "records.csv", requirementId: "CASE-1-2");
        await client.Compliance.UploadFileAsync("CASE-1", new MemoryStream(new byte[] { 1, 2, 3 }));

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("POST", request.Method);
            Assert.Equal($"{BaseUrl}/compliance/files", request.Url);
        });
        Assert.Equal(
            new[] { new FormPart("caseId", null, "CASE-1"), new FormPart("requirementId", null, "CASE-1-2"), new FormPart("file", "records.csv", "a,b\n1,2") },
            handler.Requests[0].Parts);
        Assert.Equal(new[] { "caseId", "file" }, handler.Requests[1].Parts!.Select(part => part.Name));
        Assert.Equal("file", handler.Requests[1].Parts![1].FileName);
    }

    [Fact]
    public async Task UploadFileAsync_names_a_FileStream_upload_after_its_file()
    {
        var (client, handler) = CreateClient();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-optins.csv");
        await File.WriteAllTextAsync(path, "a,b\n1,2");
        try
        {
            await using var stream = File.OpenRead(path);
            await client.Compliance.UploadFileAsync("CASE-1", stream);
        }
        finally
        {
            File.Delete(path);
        }

        Assert.Equal(Path.GetFileName(path), Assert.Single(handler.Requests).Parts![1].FileName);
    }

    [Fact]
    public async Task GetFileAsync_escapes_the_file_id()
    {
        var (client, handler) = CreateClient();

        await client.Compliance.GetFileAsync("CF 1");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("GET", request.Method);
        Assert.Equal($"{BaseUrl}/compliance/files/CF%201", request.Url);
    }

    [Theory]
    [InlineData("application/pdf", new byte[] { 37, 80, 68, 70, 0, 255, 128 })]
    [InlineData("image/png", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 255 })]
    [InlineData("text/csv", new byte[] { 97, 44, 98, 10 })]
    [InlineData("text/plain", new byte[] { })]
    public async Task GetFileAsync_preserves_exact_bytes(string contentType, byte[] bytes)
    {
        var (client, handler) = CreateClient();
        handler.ResponseBody = bytes;
        handler.ContentType = contentType;
        var response = await client.Compliance.GetFileAsync("CF1", new RequestOptions { Token = "customer-token" });
        Assert.True(response.Success);
        Assert.Equal(200, response.Status);
        Assert.Equal(bytes, response.Data);
        Assert.Null(response.Error);
        Assert.Equal("Bearer customer-token", handler.Authorization);
    }

    [Theory]
    [InlineData("{\"message\":\"Forbidden\"}", "Forbidden")]
    [InlineData("{\"error\":\"Not found\"}", "Not found")]
    [InlineData("Unavailable", "Unavailable")]
    public async Task GetFileAsync_preserves_the_normal_error_decoder(string body, string expected)
    {
        var (client, handler) = CreateClient();
        handler.Status = HttpStatusCode.Forbidden;
        handler.ResponseBody = Encoding.UTF8.GetBytes(body);
        var response = await client.Compliance.GetFileAsync("CF1");
        Assert.False(response.Success);
        Assert.Equal(403, response.Status);
        Assert.Null(response.Data);
        Assert.Equal(expected, response.Error);
    }

    [Fact]
    public async Task Required_arguments_throw_before_any_request()
    {
        var (client, handler) = CreateClient();

        Assert.Equal("caseId", (await Assert.ThrowsAsync<ValidationException>(() => client.Compliance.GetCaseAsync(null!))).ParameterName);
        Assert.Equal("requirementId", (await Assert.ThrowsAsync<ValidationException>(() => client.Compliance.AnswerRequirementAsync("CASE-1", null!))).ParameterName);
        Assert.Equal("file", (await Assert.ThrowsAsync<ValidationException>(() => client.Compliance.UploadFileAsync("CASE-1", null!))).ParameterName);
        Assert.Equal("fileId", (await Assert.ThrowsAsync<ValidationException>(() => client.Compliance.GetFileAsync(null!))).ParameterName);
        Assert.Empty(handler.Requests);
    }

    private static (SignalHouseClient Client, CaptureHandler Handler) CreateClient()
    {
        var handler = new CaptureHandler();
        return (new SignalHouseClient("default-key", BaseUrl, httpClient: new HttpClient(handler)), handler);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];
        public byte[] ResponseBody { get; set; } = Encoding.UTF8.GetBytes("{}");
        public string ContentType { get; set; } = "application/json";
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            List<FormPart>? parts = null;
            string? body = null;
            if (request.Content is MultipartFormDataContent multipart)
            {
                parts = [];
                foreach (var part in multipart)
                {
                    var disposition = part.Headers.ContentDisposition!;
                    parts.Add(new FormPart(disposition.Name!.Trim('"'), disposition.FileName?.Trim('"'), await part.ReadAsStringAsync(cancellationToken)));
                }
            }
            else if (request.Content is not null)
            {
                body = await request.Content.ReadAsStringAsync(cancellationToken);
                JsonDocument.Parse(body).Dispose();
            }

            Requests.Add(new CapturedRequest(request.Method.Method, request.RequestUri!.OriginalString, body, parts));
            Authorization = request.Headers.Authorization?.ToString();
            var content = new ByteArrayContent(ResponseBody);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType);
            return new HttpResponseMessage(Status) { Content = content };
        }
    }

    private sealed record CapturedRequest(string Method, string Url, string? Body, List<FormPart>? Parts);

    private sealed record FormPart(string Name, string? FileName, string Value);
}
