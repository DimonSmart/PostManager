using App.Domain.Enums;

namespace App.Domain.Services;

public static class PublishRollupCalculator
{
    public static PublishRollupStatus Calculate(IEnumerable<ChannelPublishStatus> statuses)
    {
        var list = statuses?.ToList() ?? new List<ChannelPublishStatus>();
        if (list.Count == 0 || list.All(status => status == ChannelPublishStatus.NotAttempted))
        {
            return PublishRollupStatus.None;
        }

        if (list.All(status => status == ChannelPublishStatus.Succeeded))
        {
            return PublishRollupStatus.Full;
        }

        return PublishRollupStatus.Partial;
    }
}

