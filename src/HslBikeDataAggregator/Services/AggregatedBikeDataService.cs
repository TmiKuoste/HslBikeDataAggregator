using HslBikeDataAggregator.Models;

namespace HslBikeDataAggregator.Services;

public sealed class AggregatedBikeDataService
{
    public Task<IReadOnlyList<BikeStation>> GetStationsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<BikeStation>>([]);

    public Task<IReadOnlyList<StationSnapshot>> GetSnapshotsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<StationSnapshot>>([]);

    public Task<IReadOnlyList<HourlyAvailability>> GetAvailabilityAsync(string stationId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<HourlyAvailability>>([]);

    public Task<IReadOnlyList<StationHistory>> GetDestinationsAsync(string stationId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<StationHistory>>([]);
}
