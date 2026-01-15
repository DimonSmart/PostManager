using App.Domain.Services;
using Xunit;

namespace App.Tests;

public class ItemParserTests
{
    [Fact]
    public void ParseItems_IgnoresBlanksAndComments()
    {
        var input = "\n  First item  \n# Comment\n\nSecond item\n";
        var items = ItemParser.ParseItems(input);

        Assert.Equal(2, items.Count);
        Assert.Equal("First item", items[0]);
        Assert.Equal("Second item", items[1]);
    }
}
