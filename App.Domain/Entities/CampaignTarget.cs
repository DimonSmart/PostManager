namespace App.Domain.Entities;

public sealed class CampaignTarget
{
    public string TenantId { get; set; } = string.Empty;
    public Guid CampaignId { get; set; }
    public Guid TargetId { get; set; }

    public Campaign? Campaign { get; set; }
    public Target? Target { get; set; }
}

