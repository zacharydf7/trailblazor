using System.Globalization;
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

    // A duration written as a motion token drops to 0ms with reduced motion; any other duration needs a rule of its own
    // in a reduced-motion block. Checked here rather than in a browser, since popups and busy states move too.
    [Fact]
    public void Stylesheet_ShouldOverrideEveryOtherDuration_WhenMotionIsReduced()
    {
        var css = Regex.Replace(File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot",
            "trailblazor.css")), @"/\*.*?\*/", "", RegexOptions.Singleline);
        var reduced = Regex.Matches(css,
            @"@media\s*\(prefers-reduced-motion:\s*reduce\)\s*\{((?:[^{}]|\{[^{}]*\})*)\}");
        var overridden = reduced
            .SelectMany(block => Regex.Matches(block.Groups[1].Value, @"([^{}]+)\{"))
            .SelectMany(rule => Selectors(rule.Groups[1].Value)).ToHashSet();

        var unstilled = Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}")
            .Where(rule => MovesForAWhile(rule.Groups[2].Value))
            .SelectMany(rule => Selectors(rule.Groups[1].Value))
            .Where(selector => !overridden.Contains(selector));

        Assert.Empty(unstilled);
    }

    private static IEnumerable<string> Selectors(string list) =>
        list.Split(',').Select(selector => Regex.Replace(selector.Trim(), @"\s+", " "));

    // A transition or animation with a duration above zero that isn't a token, such as 800ms but not visibility 0s.
    private static bool MovesForAWhile(string declarations) =>
        Regex.Matches(declarations, @"(?<![\w-])(?:transition|animation)(?:-duration)?\s*:([^;]*)")
            .Select(declaration => Regex.Replace(declaration.Groups[1].Value, @"var\([^)]*\)", ""))
            .Any(value => Regex.Matches(value, @"(?<![\w.-])(\d*\.?\d+)(?:ms|s)\b")
                .Any(time => double.Parse(time.Groups[1].Value, CultureInfo.InvariantCulture) > 0));

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
