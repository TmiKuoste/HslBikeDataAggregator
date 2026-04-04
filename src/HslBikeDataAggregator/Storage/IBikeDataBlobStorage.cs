using HslBikeDataAggregator.Models;

namespace HslBikeDataAggregator.Storage;

public interface IBikeDataBlobStorage
{
    Task<IReadOnlyList<BikeStation>> GetLatestStationsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<StationSnapshot>> GetRecentSnapshotsAsync(CancellationToken cancellationToken);

    Task WriteLatestStationsAsync(IReadOnlyList<BikeStation> stations, CancellationToken cancellationToken);

    Task WriteRecentSnapshotsAsync(IReadOnlyList<StationSnapshot> snapshots, CancellationToken cancellationToken);
}
