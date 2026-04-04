using System.Net;

using HslBikeDataAggregator.Models;
using HslBikeDataAggregator.Services;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HslBikeDataAggregator.Functions;

public sealed class StationsFunctions(
    AggregatedBikeDataService bikeDataService,
    ILogger<StationsFunctions> logger)
{
    private const string AllowedOrigin = "https://kuoste.github.io";

    [Function(nameof(GetStations))]
    public Task<HttpResponseData> GetStations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stations")] HttpRequestData request,
        CancellationToken cancellationToken)
        => CreateJsonResponseAsync(request, bikeDataService.GetStationsAsync(cancellationToken), cancellationToken);

    [Function(nameof(GetSnapshots))]
    public Task<HttpResponseData> GetSnapshots(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "snapshots")] HttpRequestData request,
        CancellationToken cancellationToken)
        => CreateJsonResponseAsync(request, bikeDataService.GetSnapshotsAsync(cancellationToken), cancellationToken);

    [Function(nameof(GetStationAvailability))]
    public Task<HttpResponseData> GetStationAvailability(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stations/{stationId}/availability")] HttpRequestData request,
        string stationId,
        CancellationToken cancellationToken)
        => CreateJsonResponseAsync(request, bikeDataService.GetAvailabilityAsync(stationId, cancellationToken), cancellationToken);

    [Function(nameof(GetStationDestinations))]
    public Task<HttpResponseData> GetStationDestinations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stations/{stationId}/destinations")] HttpRequestData request,
        string stationId,
        CancellationToken cancellationToken)
        => CreateJsonResponseAsync(request, bikeDataService.GetDestinationsAsync(stationId, cancellationToken), cancellationToken);

    private async Task<HttpResponseData> CreateJsonResponseAsync<T>(
        HttpRequestData request,
        Task<IReadOnlyList<T>> payloadTask,
        CancellationToken cancellationToken)
    {
        var payload = await payloadTask;
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", AllowedOrigin);
        response.Headers.Add("Vary", "Origin");
        await response.WriteAsJsonAsync(payload, cancellationToken);
        logger.LogInformation("Served {PayloadType} response with {Count} items.", typeof(T).Name, payload.Count);
        return response;
    }
}
