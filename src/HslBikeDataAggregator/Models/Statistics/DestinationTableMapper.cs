using HslBikeDataAggregator.Models.Shared;

namespace HslBikeDataAggregator.Models.Statistics;

public static class DestinationTableMapper
{
    private static readonly string[] DestinationFields =
    [
        "arrivalStationId",
        "tripCount",
        "averageDurationSeconds",
        "averageDistanceMetres"
    ];

    public static ColumnarTable ToDestinationTable(IReadOnlyList<DestinationRow> destinations)
    {
        ArgumentNullException.ThrowIfNull(destinations);

        return new ColumnarTable
        {
            Fields = [.. DestinationFields],
            Rows = destinations
                .Select(static destination => new object[]
                {
                    destination.ArrivalStationId,
                    destination.TripCount,
                    destination.AverageDurationSeconds,
                    destination.AverageDistanceMetres
                })
                .ToArray()
        };
    }
}
