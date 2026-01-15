using App.Domain.Enums;

namespace App.Domain.Entities;

public sealed class Target
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public TargetType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SettingsJson { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Tenant? Tenant { get; set; }
    public List<CampaignTarget> CampaignTargets { get; set; } = new();
}

