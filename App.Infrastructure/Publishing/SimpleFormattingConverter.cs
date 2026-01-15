using System.Text.RegularExpressions;
using App.Domain.Enums;
using Markdig;

namespace App.Infrastructure.Publishing;

public sealed class SimpleFormattingConverter : IFormattingConverter
{
    private static readonly Regex MarkdownDecorations = new(@"[*_`>#\-]", RegexOptions.Compiled);

    public string Convert(string markdown, TargetType targetType)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return targetType switch
        {
            TargetType.WordPress => Markdown.ToHtml(markdown),
            TargetType.TwitterX => TrimToLength(StripMarkdown(markdown), 280),
            _ => markdown
        };
    }

    private static string StripMarkdown(string markdown)
    {
        var stripped = MarkdownDecorations.Replace(markdown, string.Empty);
        return stripped.Replace("[", string.Empty)
            .Replace("]", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty);
    }

    private static string TrimToLength(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        if (maxLength <= 3)
        {
            return text.Substring(0, maxLength);
        }

        return text.Substring(0, maxLength - 3) + "...";
    }
}

