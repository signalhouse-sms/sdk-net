using System.Net;
using System.Text;
using System.Text.Json;
using SignalHouse.SDK.Models;
using Xunit;

namespace SignalHouse.SDK.Tests;

/// <summary>
/// Runs the shared RCS corpus (sdks/fixtures/rcs/requests.json) against client.Rcs: each case's request must match its
/// method, path, query and body. Bodies and queries compare as JSON values, because key order differs by language.
/// </summary>
public class RcsFixtureTests
{
    private static readonly string CorpusPath = Path.Combine(AppContext.BaseDirectory, "fixtures", "rcs", "requests.json");

    [Fact]
    public async Task EveryCaseInTheSharedCorpusProducesItsRequest()
    {
        if (!File.Exists(CorpusPath))
        {
            return; // The corpus lives in the monorepo.
        }

        using var corpus = JsonDocument.Parse(File.ReadAllText(CorpusPath));
        var failures = new List<string>();
        foreach (var testCase in corpus.RootElement.GetProperty("cases").EnumerateArray())
        {
            var name = testCase.GetProperty("name").GetString();
            var expected = testCase.GetProperty("request");
            var handler = new CaptureHandler();
            var client = new SignalHouseClient("test", "https://example.invalid", httpClient: new HttpClient(handler));

            await Run(client, testCase.GetProperty("operation").GetString()!, new Input(testCase.GetProperty("input")));

            if (handler.Requests.Count != 1)
            {
                failures.Add($"{name}: sent {handler.Requests.Count} requests");
                continue;
            }
            var sent = handler.Requests[0];
            Compare(failures, name, "method", expected.GetProperty("method").GetString(), sent.Method);
            Compare(failures, name, "path", expected.GetProperty("path").GetString(), sent.Uri.AbsolutePath);
            Compare(failures, name, "query", Canonical(Field(expected, "query")), Canonical(JsonSerializer.SerializeToElement(QueryPairs(sent.Uri))));
            Compare(failures, name, "body", Canonical(Field(expected, "body")), Canonical(sent.Body is null ? null : JsonSerializer.Deserialize<JsonElement>(sent.Body)));
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Call the method a corpus operation names, with its input under the JavaScript SDK's parameter names.
    /// </summary>
    private static Task<SignalHouseResponse> Run(SignalHouseClient client, string operation, Input input)
    {
        var agents = client.Rcs.Agents;
        var messages = client.Rcs.Messages;
        var agentId = input.Text("agentId")!;
        return operation switch
        {
            "messages.send" => messages.SendAsync(agentId, input.Json("recipients")!, templateId: input.Text("templateId"),
                templateVersion: input.Int("templateVersion"), content: input.Json("content"), contentFormat: input.Text("contentFormat"),
                trafficType: input.Text("trafficType"), ttlSeconds: input.Int("ttlSeconds"), expireTime: input.Text("expireTime"),
                fallback: input.Json("fallback"), statusCallbackUrl: input.Text("statusCallbackUrl"), enableShortlink: input.Bool("enableShortlink"),
                isTest: input.Bool("isTest"), simulate: input.Text("simulate")),
            "messages.sendBatch" => messages.SendBatchAsync(agentId, input.Json("recipients")!, templateId: input.Text("templateId"),
                templateVersion: input.Int("templateVersion"), content: input.Json("content"), contentFormat: input.Text("contentFormat"),
                trafficType: input.Text("trafficType"), ttlSeconds: input.Int("ttlSeconds"), expireTime: input.Text("expireTime"),
                fallback: input.Json("fallback"), statusCallbackUrl: input.Text("statusCallbackUrl"), enableShortlink: input.Bool("enableShortlink"),
                isTest: input.Bool("isTest"), simulate: input.Text("simulate")),
            "messages.validate" => messages.ValidateAsync(agentId, input.Json("recipients")!, templateId: input.Text("templateId"),
                templateVersion: input.Int("templateVersion"), content: input.Json("content"), contentFormat: input.Text("contentFormat"),
                trafficType: input.Text("trafficType"), ttlSeconds: input.Int("ttlSeconds"), expireTime: input.Text("expireTime"),
                fallback: input.Json("fallback"), statusCallbackUrl: input.Text("statusCallbackUrl"), enableShortlink: input.Bool("enableShortlink"),
                isTest: input.Bool("isTest"), simulate: input.Text("simulate")),
            "messages.estimate" => messages.EstimateAsync(agentId, recipients: input.Json("recipients"), recipientCount: input.Int("recipientCount"),
                templateId: input.Text("templateId"), templateVersion: input.Int("templateVersion"), content: input.Json("content"),
                contentFormat: input.Text("contentFormat"), trafficType: input.Text("trafficType"), ttlSeconds: input.Int("ttlSeconds"),
                expireTime: input.Text("expireTime"), fallback: input.Json("fallback")),
            "messages.capabilities" => messages.CapabilitiesAsync(agentId, input.Strings("phoneNumbers")!),
            "messages.get" => messages.GetAsync(input.Text("messageId")!),
            "messages.list" => messages.ListAsync(input.Text("groupId")!, input.Text("from")!, input.Text("to")!, agentId: input.Text("agentId"),
                status: input.Text("status"), page: input.Int("page"), limit: input.Int("limit"), templateId: input.Text("templateId"),
                templateVersion: input.Int("templateVersion"), bulkId: input.Text("bulkId"), fallbackReason: input.Text("fallbackReason"),
                billingClass: input.Text("billingClass"), deliveredChannel: input.Text("deliveredChannel"), isTest: input.Bool("isTest")),
            "agents.create" => agents.CreateAsync(input.Text("brandId")!, autofill: input.Json("autofill"), campaignId: input.Text("campaignId"), agent: input.Json("agent"),
                consent: input.Json("consent"), contact: input.Json("contact"), useCase: input.Text("useCase"), billingCategory: input.Text("billingCategory"),
                defaultFallback: input.Json("defaultFallback"), excludedCarriers: input.Strings("excludedCarriers"), testers: input.Strings("testers")),
            "agents.list" => agents.ListAsync(groupId: input.Text("groupId"), subgroupId: input.Text("subgroupId"), status: input.Text("status"),
                brandId: input.Text("brandId"), campaignId: input.Text("campaignId"), page: input.Int("page"), limit: input.Int("limit")),
            "agents.get" => agents.GetAsync(agentId),
            "agents.update" => agents.UpdateAsync(agentId, autofill: input.Json("autofill"), brandId: input.Text("brandId"), campaignId: input.Text("campaignId"), agent: input.Json("agent"),
                consent: input.Json("consent"), contact: input.Json("contact"), useCase: input.Text("useCase"), billingCategory: input.Text("billingCategory"),
                defaultFallback: input.Json("defaultFallback"), excludedCarriers: input.Strings("excludedCarriers"), testers: input.Strings("testers")),
            "agents.validate" => agents.ValidateAsync(agentId),
            "agents.submit" => agents.SubmitAsync(agentId),
            "agents.launch" => agents.LaunchAsync(agentId, acknowledgeFees: input.Bool("acknowledgeFees"), acknowledgePhi: input.Bool("acknowledgePhi")),
            "agents.requestCarrierUpdate" => agents.RequestCarrierUpdateAsync(agentId),
            "agents.archive" => agents.ArchiveAsync(agentId),
            "agents.createEntryPoints" => agents.CreateEntryPointsAsync(agentId, body: input.Text("body")),
            "agents.resendTesterInvite" => agents.ResendTesterInviteAsync(agentId, input.Text("phoneNumber")!),
            "agents.removeTester" => agents.RemoveTesterAsync(agentId, input.Text("phoneNumber")!),
            _ => throw new InvalidOperationException($"no adapter for {operation}"),
        };
    }

    /// <summary>
    /// Record a mismatch between the corpus and the request the SDK sent.
    /// </summary>
    private static void Compare(List<string> failures, string? name, string part, string? expected, string? actual)
    {
        if (expected != actual)
        {
            failures.Add($"{name}: {part}{Environment.NewLine}  expected {expected}{Environment.NewLine}  actual   {actual}");
        }
    }

    /// <summary>
    /// Read an optional property of a corpus request.
    /// </summary>
    private static JsonElement? Field(JsonElement request, string name) =>
        request.TryGetProperty(name, out var value) ? value : null;

    /// <summary>
    /// Read a query string as name and value pairs.
    /// </summary>
    private static Dictionary<string, string> QueryPairs(Uri uri)
    {
        var pairs = new Dictionary<string, string>();
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            pairs[Uri.UnescapeDataString(parts[0])] = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
        }
        return pairs;
    }

    /// <summary>
    /// Write a JSON value with every object's keys sorted, so two equal values give the same text. A missing value reads as {}.
    /// </summary>
    private static string Canonical(JsonElement? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            if (value is { } element)
            {
                Write(writer, element);
            }
            else
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Write one JSON value with its object keys in ordinal order.
    /// </summary>
    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    Write(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    /// <summary>
    /// One corpus case's input, read as .NET argument values.
    /// </summary>
    private sealed class Input(JsonElement input)
    {
        private JsonElement? Get(string key) => input.TryGetProperty(key, out var value) ? value : null;

        public string? Text(string key) => Get(key)?.GetString();

        public int? Int(string key) => Get(key)?.GetInt32();

        public bool? Bool(string key) => Get(key)?.GetBoolean();

        public object? Json(string key) => Get(key);

        public List<string>? Strings(string key) => Get(key)?.EnumerateArray().Select(item => item.GetString()!).ToList();
    }

    private sealed record CapturedRequest(string Method, Uri Uri, string? Body);

    /// <summary>
    /// Record each request and answer 200 with an empty JSON object.
    /// </summary>
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method.Method, request.RequestUri!, body));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }
}
