using System.Reflection;

using HslBikeDataAggregator.Functions;

using Microsoft.Azure.Functions.Worker;

namespace HslBikeDataAggregator.Tests.Functions;

public sealed class PollOpenDataFunctionTests
{
    [Fact]
    public void PollOpenData_UsesConfiguredCronExpression()
    {
        var method = typeof(PollOpenDataFunction).GetMethod(nameof(PollOpenDataFunction.PollOpenData), BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);

        var timerParameter = method!.GetParameters()[0];
        var timerTriggerAttribute = timerParameter.GetCustomAttribute<TimerTriggerAttribute>();

        Assert.NotNull(timerTriggerAttribute);
        Assert.Equal("%OpenDataPollIntervalCron%", timerTriggerAttribute!.Schedule);
    }
}
