namespace HslBikeDataAggregator.Models.Snapshots;

public sealed record StationCountSeries
{
    public required string StationId { get; init; }

    public required IReadOnlyList<int> Counts { get; init; }
}
