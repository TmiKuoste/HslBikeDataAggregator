using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HslBikeDataAggregator.Functions;

public sealed class PollStationsFunction(ILogger<PollStationsFunction> logger)
{
    [Function(nameof(PollStations))]
    public Task PollStations([TimerTrigger("%PollIntervalCron%")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        logger.LogInformation("PollStations trigger fired at {Timestamp}. Next schedule: {NextSchedule}.", DateTimeOffset.UtcNow, timerInfo.ScheduleStatus?.Next);
        return Task.CompletedTask;
    }
}
