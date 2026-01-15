using App.Domain.Enums;

namespace App.Infrastructure.Generation;

public sealed class LlmClientFactory
{
    private readonly OpenAiCompatibleLlmClient _openAiClient;
    private readonly MockLlmClient _mockClient;

    public LlmClientFactory(OpenAiCompatibleLlmClient openAiClient, MockLlmClient mockClient)
    {
        _openAiClient = openAiClient;
        _mockClient = mockClient;
    }

    public ILlmClient Resolve(LlmProvider provider)
    {
        return provider == LlmProvider.OpenAiCompatible ? _openAiClient : _mockClient;
    }
}

