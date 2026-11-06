using System.Text.RegularExpressions;
using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class AccessibilityStatusTests : DocsTest
{
    [Fact]
    public void AccessibilityStatus_ShouldListEveryComponent_WhenBuilt()
    {
        Assert.Equal(ExampleCatalog.All.Select(example => example.Name),
            AccessibilityStatusCatalog.All.Select(status => status.Component));
    }

    [Fact]
    public void AccessibilityStatus_ShouldNotCallMotionNotApplicable_WhenTheStylesheetAnimatesTheComponent()
    {
        var css = File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot", "trailblazor.css"));
        var moving = Regex.Matches(css, @"([^{}]+)\{[^{}]*\b(?:transition|animation)(?:-[a-z]+)?:[^{}]*\}")
            .Select(rule => rule.Groups[1].Value).ToList();

        var wrong = AccessibilityStatusCatalog.All
            .Where(status => status.ReducedMotion == CheckState.NotApplicable)
            .Where(status => ExampleCatalog.Find(status.Component)!.Blocks.Any(block =>
                moving.Any(selector => Regex.IsMatch(selector, $@"\.{block}(?:--|__|[^\w-]|$)"))))
            .Select(status => status.Component);

        Assert.Empty(wrong);
    }

    [Fact]
    public void AccessibilityGuide_ShouldShowEachComponentsStatus_WhenOpened()
    {
        var cut = PageAt("guides/accessibility");

        var rows = cut.FindAll("[aria-label='Accessibility status of each component'] tbody tr");

        Assert.Equal(ExampleCatalog.All.Select(example => example.Title), Texts(rows.Select(row => row.Children[0])));
        var stack = Texts(rows.Single(row => row.Children[0].TextContent.Trim() == "Stack").Children);
        Assert.Equal(["Stack", "Tested", "n/a", "n/a", "n/a", "Not yet"], stack);
        var treeView = Texts(rows.Single(row => row.Children[0].TextContent.Trim() == "Tree view").Children);
        Assert.Equal(["Tree view", "Tested", "Tested", "Tested", "Tested", "Not yet"], treeView);
    }

    [Fact]
    public void Home_ShouldLinkTheAccessibilityGuide_WhenOpened()
    {
        var cut = PageAt("");

        Assert.Contains(cut.FindAll("a"), link => link.GetAttribute("href") == "guides/accessibility"
            && link.TextContent == "See what's tested and what isn't");
    }

    [Theory]
    [InlineData("button")]
    [InlineData("text")]
    public void ComponentPage_ShouldLinkItsAccessibilityStatus_WhenOpened(string name)
    {
        var cut = ComponentPage(name);

        Assert.Contains(cut.FindAll("a"), link => link.GetAttribute("href") == "guides/accessibility#status");
    }
}
