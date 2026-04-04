namespace HslBikeDataAggregator.Services;

public sealed record PollStationsResult(DateTimeOffset Timestamp, int StationCount, int SnapshotCount);
