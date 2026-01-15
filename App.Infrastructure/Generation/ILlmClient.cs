namespace App.Infrastructure.Generation;

public interface ILlmClient
{
    Task<string> GenerateAsync(LlmRequest request, CancellationToken ct);
}

