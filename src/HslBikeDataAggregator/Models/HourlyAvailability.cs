using System.Text.Json.Serialization;

namespace HslBikeDataAggregator.Models;

public sealed record HourlyAvailability
{
    [JsonPropertyName("hour")]
    public required int Hour { get; init; }

    [JsonPropertyName("averageBikesAvailable")]
    public required double AverageBikesAvailable { get; init; }
}
