using HslBikeDataAggregator.Configuration;
using HslBikeDataAggregator.Models.OpenData;
using HslBikeDataAggregator.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HslBikeDataAggregator.Services.OpenData;

public sealed class OpenDataPollService(
    IReadOnlyList<IOpenDataSource> sources,
    IOpenDataBlobStorage blobStorage,
    IOptions<OpenDataOptions> options,
    TimeProvider timeProvider,
    ILogger<OpenDataPollService> logger)
{
    public async Task<PollOpenDataResult> PollAsync(CancellationToken cancellationToken)
    {
        var timestamp = timeProvider.GetUtcNow();
        var historyLimit = Math.Max(1, options.Value.HistoryLimit);
        var successCount = 0;

        foreach (var source in sources)
        {
            double value;
            try
            {
                var fetched = await source.FetchAsync(cancellationToken);
                value = fetched ?? -1;
                if (fetched.HasValue) successCount++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Source {SourceId} fetch failed; recording sentinel.", source.SourceId);
                value = -1;
            }

            var existing = await blobStorage.GetOpenDataTimeSeriesAsync(source.SourceId, cancellationToken)
                ?? OpenDataTimeSeries.CreateEmpty(source.SourceId, source.DisplayName, source.Lat, source.Lon, source.AttributionUrl);

            var updated = AppendValue(existing, timestamp, value, historyLimit, source);
            await blobStorage.WriteOpenDataTimeSeriesAsync(updated, cancellationToken);

            logger.LogInformation(
                "Stored open data sample for {SourceId}: value={Value}, retained {SnapshotCount} snapshots.",
                source.SourceId,
                value,
                updated.Timestamps.Count);
        }

        return new PollOpenDataResult(timestamp, sources.Count, successCount);
    }

    private static OpenDataTimeSeries AppendValue(
        OpenDataTimeSeries existing,
        DateTimeOffset timestamp,
        double value,
        int historyLimit,
        IOpenDataSource source)
    {
        var timestamps = existing.Timestamps
            .Append(timestamp)
            .OrderBy(static t => t)
            .TakeLast(historyLimit)
            .ToArray();

        var values = existing.Values
            .Append(value)
            .TakeLast(historyLimit)
            .ToArray();

        return existing with
        {
            DisplayName = source.DisplayName,
            Lat = source.Lat,
            Lon = source.Lon,
            AttributionUrl = source.AttributionUrl,
            Timestamps = timestamps,
            Values = values
        };
    }
}

public sealed record PollOpenDataResult(DateTimeOffset Timestamp, int SourceCount, int SuccessCount);
