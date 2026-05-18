using HslBikeDataAggregator.Models.OpenData;

namespace HslBikeDataAggregator.Storage;

public interface IOpenDataBlobStorage
{
    Task<OpenDataTimeSeries?> GetOpenDataTimeSeriesAsync(string sourceId, CancellationToken cancellationToken);
    Task WriteOpenDataTimeSeriesAsync(OpenDataTimeSeries timeSeries, CancellationToken cancellationToken);
}
