using App.Domain.Enums;
using App.Domain.Services;
using Xunit;

namespace App.Tests;

public class PublishRollupCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsNoneWhenAllNotAttempted()
    {
        var statuses = new[] { ChannelPublishStatus.NotAttempted, ChannelPublishStatus.NotAttempted };

        var result = PublishRollupCalculator.Calculate(statuses);

        Assert.Equal(PublishRollupStatus.None, result);
    }

    [Fact]
    public void Calculate_ReturnsFullWhenAllSucceeded()
    {
        var statuses = new[] { ChannelPublishStatus.Succeeded, ChannelPublishStatus.Succeeded };

        var result = PublishRollupCalculator.Calculate(statuses);

        Assert.Equal(PublishRollupStatus.Full, result);
    }

    [Fact]
    public void Calculate_ReturnsPartialWhenMixed()
    {
        var statuses = new[] { ChannelPublishStatus.Succeeded, ChannelPublishStatus.Failed };

        var result = PublishRollupCalculator.Calculate(statuses);

        Assert.Equal(PublishRollupStatus.Partial, result);
    }
}
