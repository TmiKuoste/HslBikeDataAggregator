using System.Text.Json.Serialization;

namespace HslBikeDataAggregator.Models.OpenData;

public sealed record OpenDataTimeSeries
{
    [JsonPropertyName("sourceId")]
    public required string SourceId { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("lat")]
    public required double Lat { get; init; }

    [JsonPropertyName("lon")]
    public required double Lon { get; init; }

    [JsonPropertyName("attributionUrl")]
    public required string AttributionUrl { get; init; }

    [JsonPropertyName("unit")]
    public string? Unit { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("timestamps")]
    public required IReadOnlyList<DateTimeOffset> Timestamps { get; init; }

    [JsonPropertyName("values")]
    public required IReadOnlyList<double> Values { get; init; }

    public static OpenDataTimeSeries CreateEmpty(
        string sourceId,
        string displayName,
        double lat,
        double lon,
        string attributionUrl,
        string? unit = null,
        string? description = null) =>
        new()
        {
            SourceId = sourceId,
            DisplayName = displayName,
            Lat = lat,
            Lon = lon,
            AttributionUrl = attributionUrl,
            Unit = unit,
            Description = description,
            Timestamps = [],
            Values = []
        };
}
