using App.Domain.Enums;

namespace App.Domain.Entities;

public sealed class PostChannelPublish
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public Guid? TargetId { get; set; }
    public ChannelPublishStatus Status { get; set; } = ChannelPublishStatus.NotAttempted;
    public string? PlatformMessageId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public Post? Post { get; set; }
    public Target? Target { get; set; }
}

