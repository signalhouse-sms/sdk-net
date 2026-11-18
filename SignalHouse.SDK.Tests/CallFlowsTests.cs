using System.Net;
using SignalHouse.SDK;
using SignalHouse.SDK.Exceptions;
using SignalHouse.SDK.Models;
using Xunit;

namespace SignalHouse.SDK.Tests;

public sealed class CallFlowsTests
{
    private const string Id = "0088947b-7821-4853-bfe6-744fce926de2";
    private const string Base = "https://example.invalid/voice/api/v1/call-flows";

    [Fact]
    public async Task Reads_use_the_expected_paths()
    {
        var (client, handler) = CreateClient();

        await client.Voice.CallFlows.ListAsync();
        await client.Voice.CallFlows.CatalogAsync();
        await client.Voice.CallFlows.GetAsync(Id);

        Assert.Collection(handler.Requests,
            r => Assert.Equal(("GET", Base, (string?)null), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("GET", $"{Base}/catalog", (string?)null), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("GET", $"{Base}/{Id}", (string?)null), (r.Method, r.Url, r.Body)));
    }

    [Fact]
    public async Task List_sends_set_filters_with_lowercase_booleans()
    {
        var (client, handler) = CreateClient();

        await client.Voice.CallFlows.ListAsync(new Dictionary<string, object?>
        {
            ["page"] = 2,
            ["subgroupId"] = "S0000001",
            ["includeDeleted"] = true,
            ["q"] = null,
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"{Base}?page=2&subgroupId=S0000001&includeDeleted=true", request.Url);
    }

    [Fact]
    public async Task Writes_use_the_expected_methods_and_bodies()
    {
        var (client, handler) = CreateClient();
        var flows = client.Voice.CallFlows;

        await flows.CreateAsync(new Dictionary<string, object?> { ["name"] = "Main line", ["subgroupId"] = "S0000001" });
        await flows.UpdateAsync(Id, new Dictionary<string, object?> { ["status"] = "DISABLED" });
        await flows.SaveDraftAsync(Id, new Dictionary<string, object?> { ["nodes"] = Array.Empty<object>(), ["edges"] = Array.Empty<object>() });
        await flows.DeleteAsync(Id);

        Assert.Collection(handler.Requests,
            r => Assert.Equal(("POST", Base, "{\"name\":\"Main line\",\"subgroupId\":\"S0000001\"}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("PATCH", $"{Base}/{Id}", "{\"status\":\"DISABLED\"}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("PUT", $"{Base}/{Id}/draft", "{\"nodes\":[],\"edges\":[]}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("DELETE", $"{Base}/{Id}", (string?)null), (r.Method, r.Url, r.Body)));
    }

    [Fact]
    public async Task Input_less_posts_send_an_empty_object()
    {
        var (client, handler) = CreateClient();
        var flows = client.Voice.CallFlows;

        await flows.PublishAsync(Id);
        await flows.RestoreAsync(Id);
        await flows.DuplicateAsync(Id);
        await flows.DuplicateAsync(Id, "Weekend");

        Assert.Collection(handler.Requests,
            r => Assert.Equal(("POST", $"{Base}/{Id}/publish", "{}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("POST", $"{Base}/{Id}/restore", "{}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("POST", $"{Base}/{Id}/duplicate", "{}"), (r.Method, r.Url, r.Body)),
            r => Assert.Equal(("POST", $"{Base}/{Id}/duplicate", "{\"name\":\"Weekend\"}"), (r.Method, r.Url, r.Body)));
    }

    [Fact]
    public async Task Request_options_and_id_encoding_apply()
    {
        var (client, handler) = CreateClient();

        await client.Voice.CallFlows.GetAsync("a/b", new RequestOptions { Token = "override-token" });

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"{Base}/a%2Fb", request.Url);
        Assert.Equal("override-token", request.Token);
    }

    [Fact]
    public async Task Missing_ids_are_rejected_before_sending()
    {
        var (client, handler) = CreateClient();

        await Assert.ThrowsAsync<ValidationException>(() => client.Voice.CallFlows.GetAsync(null!));
        await Assert.ThrowsAsync<ValidationException>(() => client.Voice.CallFlows.SaveDraftAsync(Id, null!));
        Assert.Empty(handler.Requests);
    }

    private static (SignalHouseClient Client, CaptureHandler Handler) CreateClient()
    {
        var handler = new CaptureHandler();
        return (new SignalHouseClient("default-key", "https://example.invalid", httpClient: new HttpClient(handler)), handler);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Method.Method,
                request.RequestUri!.AbsoluteUri,
                request.Headers.Authorization?.Parameter,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }

    private sealed record CapturedRequest(string Method, string Url, string? Token, string? Body);
}
