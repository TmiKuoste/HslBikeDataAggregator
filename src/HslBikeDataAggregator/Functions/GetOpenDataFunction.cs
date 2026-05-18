using System.Net;

using HslBikeDataAggregator.Models.OpenData;
using HslBikeDataAggregator.Services.OpenData;
using HslBikeDataAggregator.Storage;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HslBikeDataAggregator.Functions;

public sealed class GetOpenDataFunction(
    IReadOnlyList<IOpenDataSource> sources,
    IOpenDataBlobStorage openDataBlobStorage,
    ILogger<GetOpenDataFunction> logger)
{
    private const string CacheControl = "public, max-age=120";

    [Function(nameof(GetOpenData))]
    public async Task<HttpResponseData> GetOpenData(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "open-data")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var timeSeries = await Task.WhenAll(
            sources.Select(s => openDataBlobStorage.GetOpenDataTimeSeriesAsync(s.SourceId, cancellationToken)));

        var result = timeSeries.OfType<OpenDataTimeSeries>().ToArray();

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Cache-Control", CacheControl);
        await response.WriteAsJsonAsync(result, cancellationToken);
        logger.LogInformation("Served {Count} open data time series.", result.Length);
        return response;
    }
}
