using App.Domain.Entities;
using App.Domain.Services;

namespace App.Infrastructure.Generation;

public sealed class ContentCreatorAgent
{
    private readonly LlmClientFactory _factory;

    public ContentCreatorAgent(LlmClientFactory factory)
    {
        _factory = factory;
    }

    public Task<string> GenerateDraftAsync(Campaign campaign, Item item, LlmRuntimeConfig config, CancellationToken ct)
    {
        var prompt = PromptBuilder.BuildTextPrompt(campaign, item);
        var request = new LlmRequest(
            SystemPrompt: "You are Content Creator Agent. Generate one variant of a short post.",
            UserPrompt: prompt,
            Model: campaign.TextLlmModel,
            Temperature: campaign.TextLlmTemperature,
            MaxTokens: campaign.TextLlmMaxTokens,
            Endpoint: config.Endpoint,
            ApiKey: config.ApiKey);

        var client = _factory.Resolve(campaign.TextLlmProvider);
        return client.GenerateAsync(request, ct);
    }
}

