using App.Domain.Entities;

namespace App.Infrastructure.Generation;

public sealed class MultiAgentTextGenerator
{
    private readonly ContentCreatorAgent _creatorAgent;
    private readonly EditorAgent _editorAgent;

    public MultiAgentTextGenerator(ContentCreatorAgent creatorAgent, EditorAgent editorAgent)
    {
        _creatorAgent = creatorAgent;
        _editorAgent = editorAgent;
    }

    public async Task<PostVariant> GenerateVariantAsync(Post post, Campaign campaign, Item item, LlmRuntimeConfig config, CancellationToken ct)
    {
        var draft = await _creatorAgent.GenerateDraftAsync(campaign, item, config, ct);
        var notes = await _editorAgent.ReviewAsync(campaign, draft, config, ct);

        return new PostVariant
        {
            Id = Guid.NewGuid(),
            TenantId = post.TenantId,
            PostId = post.Id,
            MarkdownText = draft,
            EditorNotes = notes,
            ModelInfo = $"{campaign.TextLlmProvider}/{campaign.TextLlmModel}",
            CreatedUtc = DateTime.UtcNow
        };
    }
}

