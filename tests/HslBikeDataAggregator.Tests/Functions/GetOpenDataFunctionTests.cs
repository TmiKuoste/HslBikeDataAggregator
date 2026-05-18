using System.Net;
using System.Security.Claims;

using Azure.Core.Serialization;

using HslBikeDataAggregator.Functions;
using HslBikeDataAggregator.Models.OpenData;
using HslBikeDataAggregator.Services.OpenData;
using HslBikeDataAggregator.Storage;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace HslBikeDataAggregator.Tests.Functions;

public sealed class GetOpenDataFunctionTests
{
    [Fact]
    public async Task GetOpenData_ReturnsOkWithCacheHeadersAndTimeSeries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = CreateMockRequest("https://localhost/api/open-data");

        var source = new Mock<IOpenDataSource>();
        source.SetupGet(s => s.SourceId).Returns("uimastadion");

        var timeSeries = new OpenDataTimeSeries
        {
            SourceId = "uimastadion",
            DisplayName = "Uimastadion",
            Lat = 60.1857,
            Lon = 24.9282,
            AttributionUrl = "https://example.com",
            Timestamps = [new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero)],
            Values = [45.0]
        };

        var blobStorage = new Mock<IOpenDataBlobStorage>();
        blobStorage.Setup(s => s.GetOpenDataTimeSeriesAsync("uimastadion", cancellationToken)).ReturnsAsync(timeSeries);

        var function = new GetOpenDataFunction([source.Object], blobStorage.Object, NullLogger<GetOpenDataFunction>.Instance);

        var response = await function.GetOpenData(request.Object, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Cache-Control", out var cacheControl));
        Assert.Contains("public, max-age=120", cacheControl);

        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Assert.Contains("\"sourceId\":\"uimastadion\"", body, StringComparison.Ordinal);
        Assert.Contains("\"displayName\":\"Uimastadion\"", body, StringComparison.Ordinal);
        Assert.Contains("45", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetOpenData_FiltersNullTimeSeries_WhenBlobNotYetPopulated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = CreateMockRequest("https://localhost/api/open-data");

        var source = new Mock<IOpenDataSource>();
        source.SetupGet(s => s.SourceId).Returns("uimastadion");

        var blobStorage = new Mock<IOpenDataBlobStorage>();
        blobStorage.Setup(s => s.GetOpenDataTimeSeriesAsync("uimastadion", cancellationToken)).ReturnsAsync((OpenDataTimeSeries?)null);

        var function = new GetOpenDataFunction([source.Object], blobStorage.Object, NullLogger<GetOpenDataFunction>.Instance);

        var response = await function.GetOpenData(request.Object, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        Assert.Equal("[]", await reader.ReadToEndAsync(cancellationToken));
    }

    private static Mock<HttpRequestData> CreateMockRequest(string url)
    {
        var services = new ServiceCollection();
        services
            .AddOptions<WorkerOptions>()
            .Configure(options => options.Serializer = new JsonObjectSerializer());

        var serviceProvider = services.BuildServiceProvider();
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupProperty(c => c.InstanceServices, serviceProvider);

        var responseBody = new MemoryStream();
        var response = new Mock<HttpResponseData>(functionContext.Object);
        response.SetupProperty(r => r.StatusCode);
        response.SetupProperty(r => r.Body, responseBody);
        response.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        response.SetupGet(r => r.Cookies).Returns(Mock.Of<HttpCookies>());

        var request = new Mock<HttpRequestData>(functionContext.Object);
        request.Setup(r => r.CreateResponse()).Returns(response.Object);
        request.SetupGet(r => r.Body).Returns(Stream.Null);
        request.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        request.SetupGet(r => r.Identities).Returns(Array.Empty<ClaimsIdentity>());
        request.SetupGet(r => r.Method).Returns("GET");
        request.SetupGet(r => r.Url).Returns(new Uri(url));
        return request;
    }
}
