using System.Reflection;

using HslBikeDataAggregator.Functions;

using Microsoft.Azure.Functions.Worker;

namespace HslBikeDataAggregator.Tests.Functions;

public sealed class PollStationsFunctionTests
{
    [Fact]
    public void PollStations_UsesConfiguredCronExpression()
    {
        var method = typeof(PollStationsFunction).GetMethod(nameof(PollStationsFunction.PollStations), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);

        var timerParameter = method!.GetParameters()[0];
        var timerTriggerAttribute = timerParameter.GetCustomAttribute<TimerTriggerAttribute>();

        Assert.NotNull(timerTriggerAttribute);
        Assert.Equal("%PollIntervalCron%", timerTriggerAttribute!.Schedule);
    }
}
