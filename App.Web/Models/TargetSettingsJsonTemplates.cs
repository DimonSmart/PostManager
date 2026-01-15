namespace App.Web.Models;

public static class TargetSettingsJsonTemplates
{
    public const string TelegramChannelTemplate = """
{
  "telegram": {
    "chatId": "@YourChannelOrChat",
    "botToken": "",

    "disableWebPagePreview": false,
    "disableNotification": false,
    "protectContent": false
  }
}
""";
}
