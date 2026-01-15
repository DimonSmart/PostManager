using System.Net.Http.Json;
using System.Text.Json;
using App.Domain.Enums;
using App.Infrastructure.Models;

namespace App.Infrastructure.Publishing.Adapters;

public sealed class TelegramPublishAdapter : IPublishAdapter
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TelegramPublishAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public TargetType TargetType => TargetType.TelegramChannel;

    public async Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct)
    {
        var settings = ParseSettings(request.TargetSettingsJson);
        var token = ResolveToken(settings);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Telegram bot token is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ChatId))
        {
            throw new InvalidOperationException("Telegram chat id is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.ImagePath))
        {
            return await SendMessageAsync(token, settings, request.Text, ct);
        }

        return await SendPhotoAsync(token, settings, request.Text, request.ImagePath, ct);
    }

    private sealed class TelegramSettingsEnvelope
    {
        public TelegramTargetSettings? Telegram { get; set; }
    }

    private static TelegramTargetSettings ParseSettings(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return new TelegramTargetSettings();
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<TelegramSettingsEnvelope>(settingsJson, JsonOptions);
            if (envelope?.Telegram is not null)
            {
                return envelope.Telegram;
            }

            return JsonSerializer.Deserialize<TelegramTargetSettings>(settingsJson, JsonOptions)
                   ?? new TelegramTargetSettings();
        }
        catch
        {
            return new TelegramTargetSettings();
        }
    }

    private static string? ResolveToken(TelegramTargetSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.BotToken))
        {
            return settings.BotToken;
        }

        if (!string.IsNullOrWhiteSpace(settings.BotTokenEnv))
        {
            return Environment.GetEnvironmentVariable(settings.BotTokenEnv);
        }

        return null;
    }

    private async Task<PublishResult> SendMessageAsync(string token, TelegramTargetSettings settings, string text, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{token}/sendMessage";
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = settings.ChatId,
            ["text"] = text,
            ["disable_web_page_preview"] = settings.DisableWebPagePreview,
            ["disable_notification"] = settings.DisableNotification,
            ["protect_content"] = settings.ProtectContent
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Telegram sendMessage failed: {response.StatusCode} {content}");
        }

        return new PublishResult(ExtractMessageId(content));
    }

    private async Task<PublishResult> SendPhotoAsync(string token, TelegramTargetSettings settings, string caption, string imagePath, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{token}/sendPhoto";
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(settings.ChatId!), "chat_id");
        content.Add(new StringContent(caption), "caption");

        if (settings.DisableNotification)
        {
            content.Add(new StringContent("true"), "disable_notification");
        }

        if (settings.ProtectContent)
        {
            content.Add(new StringContent("true"), "protect_content");
        }

        await using var fileStream = File.OpenRead(imagePath);
        content.Add(new StreamContent(fileStream), "photo", Path.GetFileName(imagePath));

        using var response = await _httpClient.PostAsync(url, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Telegram sendPhoto failed: {response.StatusCode} {responseBody}");
        }

        return new PublishResult(ExtractMessageId(responseBody));
    }

    private static string? ExtractMessageId(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("result", out var result)
                && result.TryGetProperty("message_id", out var messageId))
            {
                return messageId.GetRawText();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
