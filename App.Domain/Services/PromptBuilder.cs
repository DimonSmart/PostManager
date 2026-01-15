using App.Domain.Entities;

namespace App.Domain.Services;

public static class PromptBuilder
{
    public static string BuildTextPrompt(Campaign campaign, Item item)
    {
        return InjectItem(campaign.TextPrompt, item.SourceText, "Item: ");
    }

    public static string BuildEditorPrompt(Campaign campaign, string draftText)
    {
        var rules = campaign.TextEditorRulesPrompt;
        if (string.IsNullOrWhiteSpace(rules))
        {
            return $"Review the draft and provide notes.\n\nDraft:\n{draftText}";
        }

        return $"{rules}\n\nDraft:\n{draftText}";
    }

    public static string BuildImagePrompt(Campaign campaign, Item item)
    {
        return InjectItem(campaign.ImagePositivePrompt, item.SourceText, "Subject: ");
    }

    private static string InjectItem(string template, string itemText, string fallbackPrefix)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return $"{fallbackPrefix}{itemText}";
        }

        var withBrace = template.Replace("{{item}}", itemText, StringComparison.OrdinalIgnoreCase)
            .Replace("{item}", itemText, StringComparison.OrdinalIgnoreCase);

        if (!ReferenceEquals(withBrace, template) && !string.Equals(withBrace, template, StringComparison.Ordinal))
        {
            return withBrace;
        }

        return $"{template}\n\n{fallbackPrefix}{itemText}";
    }
}

