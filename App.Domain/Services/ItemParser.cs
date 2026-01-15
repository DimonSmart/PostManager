namespace App.Domain.Services;

public static class ItemParser
{
    public static IReadOnlyList<string> ParseItems(string? rawText, bool ignoreComments = true)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Array.Empty<string>();
        }

        var items = new List<string>();
        var lines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (ignoreComments && trimmed.StartsWith('#'))
            {
                continue;
            }

            items.Add(trimmed);
        }

        return items;
    }
}

