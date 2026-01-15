namespace App.Infrastructure.Generation;

public sealed class MockLlmClient : ILlmClient
{
    public Task<string> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        var text = $"[MOCK RESPONSE]\n\n{request.UserPrompt}";
        return Task.FromResult(text);
    }
}

