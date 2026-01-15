namespace App.Domain.Entities;

public sealed class PostVariant
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public string MarkdownText { get; set; } = string.Empty;
    public string? ModelInfo { get; set; }
    public string? EditorNotes { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Post? Post { get; set; }
}

