using App.Domain.Enums;

namespace App.Domain.Entities;

public sealed class Campaign
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresModeration { get; set; } = true;

    public int TextVariantsPerPost { get; set; } = 1;
    public string TextPrompt { get; set; } = string.Empty;
    public string TextEditorRulesPrompt { get; set; } = string.Empty;
    public LlmProvider TextLlmProvider { get; set; } = LlmProvider.Mock;
    public string TextLlmModel { get; set; } = string.Empty;
    public double TextLlmTemperature { get; set; } = 0.7;
    public int TextLlmMaxTokens { get; set; } = 512;

    public int ImageVariantsPerPost { get; set; } = 1;
    public ImageProvider ImageProvider { get; set; } = ImageProvider.StableDiffusionLocal;
    public string ImagePositivePrompt { get; set; } = string.Empty;
    public string? ImageNegativePrompt { get; set; }
    public string? ImageOptions { get; set; }

    public string ScheduleCron { get; set; } = "0 0 10 ? * * *";
    public string ScheduleTimezone { get; set; } = "UTC";
    public int MissedCatchUpWithinMinutes { get; set; }
    public MissedPublishPolicy MissedIfMissedLongerThanMinutes { get; set; } = MissedPublishPolicy.SkipAndNotify;

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public Tenant? Tenant { get; set; }
    public List<CampaignTarget> CampaignTargets { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<Post> Posts { get; set; } = new();
}

