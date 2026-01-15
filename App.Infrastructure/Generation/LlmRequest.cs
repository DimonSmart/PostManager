namespace App.Infrastructure.Generation;

public sealed record LlmRequest(
    string? SystemPrompt,
    string UserPrompt,
    string Model,
    double Temperature,
    int MaxTokens,
    string? Endpoint,
    string? ApiKey);

