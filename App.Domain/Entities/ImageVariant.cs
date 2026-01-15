namespace App.Domain.Entities;

public sealed class ImageVariant
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public string ImageUri { get; set; } = string.Empty;
    public string? PromptUsed { get; set; }
    public string? GeneratorInfo { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Post? Post { get; set; }
}

