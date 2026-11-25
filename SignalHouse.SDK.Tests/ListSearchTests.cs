using System.Text;
using Xunit;

namespace SignalHouse.SDK.Tests;

public sealed class ListSearchTests
{
    private const string BaseUrl = "https://example.invalid";

    private static readonly string[] Search = ["apex", "displayName:dental"];

    [Fact]
    public async Task List_reads_send_each_search_term_as_its_own_key()
    {
        var (client, handler) = CreateClient();

        await client.Subgroups.GetSubgroupsAsync(groupId: "G0000001", search: Search);
        await client.Brands.GetBrandsAsync(groupId: "G0000001", search: Search);
        await client.Campaigns.GetCampaignsAsync(groupId: "G0000001", search: Search);
        await client.Numbers.GetPhoneNumbersAsync(groupId: "G0000001", search: Search);

        Assert.Equal(
            new[]
            {
                $"{BaseUrl}/subgroup?groupId=G0000001&search=apex&search=displayName%3Adental",
                $"{BaseUrl}/brand?groupId=G0000001&search=apex&search=displayName%3Adental",
                $"{BaseUrl}/campaign?groupId=G0000001&search=apex&search=displayName%3Adental",
                $"{BaseUrl}/number?groupId=G0000001&search=apex&search=displayName%3Adental",
            },
            handler.Urls);
    }

    [Fact]
    public async Task List_reads_without_search_are_unchanged()
    {
        var (client, handler) = CreateClient();

        await client.Subgroups.GetSubgroupsAsync(groupId: "G0000001", page: 2);
        await client.Brands.GetBrandsAsync(groupId: "G0000001", currentTollFreeCarrier: true);
        await client.Campaigns.GetCampaignsAsync(groupId: "G0000001", search: []);
        await client.Numbers.GetPhoneNumbersAsync();

        Assert.Equal(
            new[]
            {
                $"{BaseUrl}/subgroup?groupId=G0000001&page=2",
                $"{BaseUrl}/brand?groupId=G0000001&currentTollFreeCarrier=true",
                $"{BaseUrl}/campaign?groupId=G0000001",
                $"{BaseUrl}/number",
            },
            handler.Urls);
    }

    private static (SignalHouseClient Client, CaptureHandler Handler) CreateClient()
    {
        var handler = new CaptureHandler();
        return (new SignalHouseClient("default-key", BaseUrl, httpClient: new HttpClient(handler)), handler);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public List<string> Urls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri!.OriginalString);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            });
        }
    }
}
