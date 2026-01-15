using App.Domain.Enums;

namespace App.Web.Models;

public sealed class TargetFormModel
{
    public string DisplayName { get; set; } = string.Empty;
    public TargetType TargetType { get; set; } = TargetType.TelegramChannel;
    public string? SettingsJson { get; set; }
}
