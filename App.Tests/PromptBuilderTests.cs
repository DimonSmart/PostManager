using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Services;
using Xunit;

namespace App.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void BuildTextPrompt_ReplacesItemPlaceholder()
    {
        var campaign = new Campaign { TextPrompt = "Write about {item}", TextEditorRulesPrompt = "" };
        var item = new Item { SourceText = "coffee" };

        var prompt = PromptBuilder.BuildTextPrompt(campaign, item);

        Assert.Equal("Write about coffee", prompt);
    }

    [Fact]
    public void BuildTextPrompt_AppendsItemWhenNoPlaceholder()
    {
        var campaign = new Campaign { TextPrompt = "Write a short post", TextEditorRulesPrompt = "" };
        var item = new Item { SourceText = "tea" };

        var prompt = PromptBuilder.BuildTextPrompt(campaign, item);

        Assert.Contains("Write a short post", prompt);
        Assert.Contains("Item: tea", prompt);
    }
}
