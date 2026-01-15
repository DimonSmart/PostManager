using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace App.Infrastructure.Generation;

public sealed class OpenAiCompatibleLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;

    public OpenAiCompatibleLlmClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            throw new InvalidOperationException("LLM endpoint is not configured.");
        }

        var url = BuildChatCompletionUrl(request.Endpoint);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
        }

        var messages = new List<Dictionary<string, string>>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new Dictionary<string, string>
            {
                ["role"] = "system",
                ["content"] = request.SystemPrompt
            });
        }

        messages.Add(new Dictionary<string, string>
        {
            ["role"] = "user",
            ["content"] = request.UserPrompt
        });

        var payload = new
        {
            model = request.Model,
            messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        var json = JsonSerializer.Serialize(payload);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LLM call failed: {response.StatusCode} {responseContent}");
        }

        using var document = JsonDocument.Parse(responseContent);
        if (document.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }

            if (firstChoice.TryGetProperty("text", out var text))
            {
                return text.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string BuildChatCompletionUrl(string endpoint)
    {
        var trimmed = endpoint.TrimEnd('/');
        if (trimmed.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return $"{trimmed}/v1/chat/completions";
    }
}

