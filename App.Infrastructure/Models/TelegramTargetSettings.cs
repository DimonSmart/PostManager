namespace App.Infrastructure.Models;

public sealed class TelegramTargetSettings
{
    public string? BotTokenEnv { get; set; }
    public string? BotToken { get; set; }
    public string? ChatId { get; set; }
}

