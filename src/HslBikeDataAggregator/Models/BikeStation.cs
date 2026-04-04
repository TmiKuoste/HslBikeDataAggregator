using System.Text.Json.Serialization;

namespace HslBikeDataAggregator.Models;

public sealed record BikeStation
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("lat")]
    public required double Lat { get; init; }

    [JsonPropertyName("lon")]
    public required double Lon { get; init; }

    [JsonPropertyName("capacity")]
    public required int Capacity { get; init; }

    [JsonPropertyName("bikesAvailable")]
    public required int BikesAvailable { get; init; }

    [JsonPropertyName("spacesAvailable")]
    public required int SpacesAvailable { get; init; }

    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; }
}
