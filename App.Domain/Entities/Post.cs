using App.Domain.Enums;

namespace App.Domain.Entities;

public sealed class Post
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid CampaignId { get; set; }
    public Guid ItemId { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Created;
    public bool IsTextReady { get; set; }
    public bool IsImageReady { get; set; }
    public Guid? SelectedTextVariantId { get; set; }
    public Guid? SelectedImageVariantId { get; set; }
    public string? UserEditedMarkdown { get; set; }
    public DateTime? PublishAtUtc { get; set; }
    public PublishRollupStatus PublishRollupStatus { get; set; } = PublishRollupStatus.None;
    public string? PublicationLog { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public Campaign? Campaign { get; set; }
    public Item? Item { get; set; }
    public List<PostVariant> Variants { get; set; } = new();
    public List<ImageVariant> ImageVariants { get; set; } = new();
    public List<PostChannelPublish> ChannelPublishes { get; set; } = new();
}

