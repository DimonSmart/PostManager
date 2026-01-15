using App.Domain.Entities;
using App.Domain.Services;

namespace App.Infrastructure.Generation;

public sealed class EditorAgent
{
    private readonly LlmClientFactory _factory;

    public EditorAgent(LlmClientFactory factory)
    {
        _factory = factory;
    }

    public Task<string> ReviewAsync(Campaign campaign, string draftText, LlmRuntimeConfig config, CancellationToken ct)
    {
        var prompt = PromptBuilder.BuildEditorPrompt(campaign, draftText);
        var request = new LlmRequest(
            SystemPrompt: "You are Editor Agent. Review the draft and provide concise notes.",
            UserPrompt: prompt,
            Model: campaign.TextLlmModel,
            Temperature: Math.Min(0.3, campaign.TextLlmTemperature),
            MaxTokens: Math.Min(256, campaign.TextLlmMaxTokens),
            Endpoint: config.Endpoint,
            ApiKey: config.ApiKey);

        var client = _factory.Resolve(campaign.TextLlmProvider);
        return client.GenerateAsync(request, ct);
    }
}

