using HslBikeDataAggregator.Services;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HslBikeDataAggregator.Functions;

public sealed class PollStationsFunction(IPollStationsService pollStationsService, ILogger<PollStationsFunction> logger)
{
    [Function(nameof(PollStations))]
    public async Task PollStations([TimerTrigger("%PollIntervalCron%")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        logger.LogInformation("PollStations trigger fired at {Timestamp}. Next schedule: {NextSchedule}.", DateTimeOffset.UtcNow, timerInfo.ScheduleStatus?.Next);
        var result = await pollStationsService.PollAsync(cancellationToken);

        logger.LogInformation(
            "PollStations completed at {Timestamp}. Stored {StationCount} stations and retained {SnapshotCount} snapshots.",
            result.Timestamp,
            result.StationCount,
            result.SnapshotCount);
    }
}
