using System.Net.Http.Json;
using System.Text.Json;

using HslBikeDataAggregator.Configuration;
using HslBikeDataAggregator.Models;

using Microsoft.Extensions.Options;

namespace HslBikeDataAggregator.Services;

public sealed class DigitransitStationClient(HttpClient httpClient, IOptions<PollStationsOptions> options)
{
    private const string GraphQlUrl = "https://api.digitransit.fi/routing/v2/hsl/gtfs/v1";

    private const string Query = """
        {
          vehicleRentalStations {
            stationId
            name
            lat
            lon
            allowPickup
            allowDropoff
            capacity
            availableVehicles { byType { count } }
            availableSpaces { byType { count } }
          }
        }
        """;

    public async Task<IReadOnlyList<BikeStation>> FetchStationsAsync(CancellationToken cancellationToken)
    {
        var subscriptionKey = options.Value.DigitransitSubscriptionKey;
        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("DigitransitSubscriptionKey setting is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlUrl)
        {
            Content = JsonContent.Create(new GraphQlRequest(Query))
        };

        request.Headers.Add("digitransit-subscription-key", subscriptionKey);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

        var stations = new List<BikeStation>();
        var rentalStations = document.RootElement
            .GetProperty("data")
            .GetProperty("vehicleRentalStations");

        foreach (var station in rentalStations.EnumerateArray())
        {
            stations.Add(new BikeStation
            {
                Id = station.GetProperty("stationId").GetString() ?? string.Empty,
                Name = station.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                Lat = station.GetProperty("lat").GetDouble(),
                Lon = station.GetProperty("lon").GetDouble(),
                Capacity = station.TryGetProperty("capacity", out var capacity) ? capacity.GetInt32() : 0,
                BikesAvailable = SumByType(station, "availableVehicles"),
                SpacesAvailable = SumByType(station, "availableSpaces"),
                IsActive = station.TryGetProperty("allowPickup", out var allowPickup) && allowPickup.GetBoolean()
            });
        }

        return stations;
    }

    private static int SumByType(JsonElement station, string propertyName)
    {
        if (!station.TryGetProperty(propertyName, out var property))
        {
            return 0;
        }

        if (!property.TryGetProperty("byType", out var byType))
        {
            return 0;
        }

        var total = 0;
        foreach (var entry in byType.EnumerateArray())
        {
            if (entry.TryGetProperty("count", out var count))
            {
                total += count.GetInt32();
            }
        }

        return total;
    }

    private sealed record GraphQlRequest(string Query);
}
