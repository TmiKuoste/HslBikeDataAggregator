using HslBikeDataAggregator.Services.OpenData;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HslBikeDataAggregator.Functions;

public sealed class PollOpenDataFunction(OpenDataPollService openDataPollService, ILogger<PollOpenDataFunction> logger)
{
    [Function(nameof(PollOpenData))]
    public async Task PollOpenData([TimerTrigger("%OpenDataPollIntervalCron%")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        logger.LogInformation("PollOpenData trigger fired at {Timestamp}. Next schedule: {NextSchedule}.", DateTimeOffset.UtcNow, timerInfo.ScheduleStatus?.Next);
        var result = await openDataPollService.PollAsync(cancellationToken);

        logger.LogInformation(
            "PollOpenData completed at {Timestamp}. Polled {SourceCount} sources, {SuccessCount} succeeded.",
            result.Timestamp,
            result.SourceCount,
            result.SuccessCount);
    }
}
