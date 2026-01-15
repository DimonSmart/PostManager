namespace App.Domain.Entities;

public sealed class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
    public DateTime CreatedUtc { get; set; }

    public List<Target> Targets { get; set; } = new();
    public List<Campaign> Campaigns { get; set; } = new();
}

