namespace HslBikeDataAggregator.Services.OpenData;

public interface IOpenDataSource
{
    string SourceId { get; }
    string DisplayName { get; }
    double Lat { get; }
    double Lon { get; }
    string AttributionUrl { get; }

    /// Returns null when data is unavailable (e.g. out of season) — caller records -1 sentinel.
    Task<double?> FetchAsync(CancellationToken cancellationToken);
}
