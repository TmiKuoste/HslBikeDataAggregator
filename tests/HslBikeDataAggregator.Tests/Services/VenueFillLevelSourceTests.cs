using System.Net;
using System.Text;

using HslBikeDataAggregator.Configuration;
using HslBikeDataAggregator.Services.OpenData;

namespace HslBikeDataAggregator.Tests.Services;

public sealed class VenueFillLevelSourceTests
{
    [Fact]
    public async Task FetchAsync_ReturnsFillLevel_WhenResponseContainsResultObject()
    {
        var source = CreateSource("""
            {
              "result": {
                "location_name": "Uimastadion",
                "fill_level": 185,
                "location_id": 180,
                "capacity": 4000
              },
              "isError": false,
              "message": "Operation successful"
            }
            """);

        var value = await source.FetchAsync(TestContext.Current.CancellationToken);

        Assert.Equal(185.0, value);
    }

    [Fact]
    public async Task FetchAsync_ReturnsNull_WhenIsErrorTrue()
    {
        var source = CreateSource("""
            {
              "result": null,
              "isError": true,
              "message": "Some error"
            }
            """);

        var value = await source.FetchAsync(TestContext.Current.CancellationToken);

        Assert.Null(value);
    }

    [Fact]
    public async Task FetchAsync_ReturnsNull_WhenFillLevelFieldAbsent()
    {
        var source = CreateSource("""
            {
              "result": {
                "location_name": "Uimastadion"
              },
              "isError": false
            }
            """);

        var value = await source.FetchAsync(TestContext.Current.CancellationToken);

        Assert.Null(value);
    }

    private static VenueFillLevelSource CreateSource(string responseBody)
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        }));

        var config = new VenueFillLevelConfig
        {
            SourceId = "uimastadion",
            DisplayName = "Uimastadion",
            Lat = 60.1857,
            Lon = 24.9282,
            AttributionUrl = "https://example.com",
            LocationId = "180",
            LocationUrlName = "uimastadion"
        };

        return new VenueFillLevelSource(config, new HttpClient(handler));
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request);
    }
}
