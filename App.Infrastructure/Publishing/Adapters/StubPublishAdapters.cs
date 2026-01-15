using App.Domain.Enums;

namespace App.Infrastructure.Publishing.Adapters;

public sealed class TwitterPublishAdapter : IPublishAdapter
{
    public TargetType TargetType => TargetType.TwitterX;

    public Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct)
    {
        throw new NotSupportedException("Twitter/X publishing is not implemented in MVP.");
    }
}

public sealed class WordPressPublishAdapter : IPublishAdapter
{
    public TargetType TargetType => TargetType.WordPress;

    public Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct)
    {
        throw new NotSupportedException("WordPress publishing is not implemented in MVP.");
    }
}

public sealed class InstagramPublishAdapter : IPublishAdapter
{
    public TargetType TargetType => TargetType.Instagram;

    public Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct)
    {
        throw new NotSupportedException("Instagram publishing is not implemented in MVP.");
    }
}
