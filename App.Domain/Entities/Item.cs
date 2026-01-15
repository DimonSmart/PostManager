namespace App.Domain.Entities;

public sealed class Item
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid CampaignId { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Campaign? Campaign { get; set; }
    public List<Post> Posts { get; set; } = new();
}

