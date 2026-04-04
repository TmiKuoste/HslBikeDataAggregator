using System.Text.Json.Serialization;

namespace HslBikeDataAggregator.Models;

public sealed record StationSnapshot
{
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("bikeCounts")]
    public required IReadOnlyDictionary<string, int> BikeCounts { get; init; }
}
