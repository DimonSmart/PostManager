namespace App.Infrastructure.Models;

public sealed class TelegramTargetSettings
{
    public string? BotTokenEnv { get; set; }
    public string? BotToken { get; set; }
    public string? ChatId { get; set; }

    public bool DisableWebPagePreview { get; set; }
    public bool DisableNotification { get; set; }
    public bool ProtectContent { get; set; }
}

