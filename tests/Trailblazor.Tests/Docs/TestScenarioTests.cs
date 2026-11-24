using System.Text.RegularExpressions;
using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class TestScenarioTests : DocsTest
{
    // The pages still without a test of their own to show (#696). A page leaves this list when it gets its scenario;
    // a new page ships with one instead of joining it.
    private static readonly string[] Owed =
    [
        "text", "avatar", "timeline", "image", "card", "icon", "button-group", "back-to-top", "app-shell", "progress",
        "label", "mention-input", "time-picker", "query-builder", "focus-scope", "tour", "table", "table-of-contents",
        "empty-state", "speech-to-text", "highlight", "skeleton", "badge",
    ];

    // The pages with nothing for a user to do or for a test to check, such as a spacer's, which need no scenario.
    private static readonly string[] NotOwed =
    [
        "kbd", "qr-code", "watermark", "divider", "surface", "stack", "spacer", "field-row", "grid", "masonry",
        "container",
    ];

    public static TheoryData<string> WithScenario =>
        new(ExampleCatalog.All.Where(example => example.Scenario is not null).Select(example => example.Name));

    [Fact]
    public void Scenario_ShouldBeOnEveryPageThatIsntOwedOne_WhenTheCatalogIsRead()
    {
        var without = ExampleCatalog.All.Where(example => example.Scenario is null).Select(example => example.Name);

        Assert.Equal(Owed.Concat(NotOwed).Order(), without.Order());
    }

    [Theory, MemberData(nameof(WithScenario))]
    public void Scenario_ShouldNameASectionOfItsPageAndHaveATestToShow_WhenAPageHasOne(string name)
    {
        var example = ExampleCatalog.Find(name)!;
        var scenario = example.Scenario!;

        var section = Assert.Single(example.Sections, section => section.Id == scenario.Section);
        // The sentence follows "A whole test of the example under ...:" on the page.
        Assert.Matches(@"^[a-z].*\.$", scenario.Checks);
        // The page says the tests drive that section's example, so each has to.
        Assert.Contains($"Render<{section.Component.Name}>()", TestScenario.Bunit(example));
        Assert.Contains($"OpenAsync(browser, \"/examples/{name}/{scenario.Section}\")",
            TestScenario.Playwright(example));
        Assert.All([TestScenario.Bunit(example), TestScenario.Playwright(example)],
            test => Assert.Contains(".Tb().", test));
    }

    [Fact]
    public void Scenario_ShouldBelongToAPage_WhenItsFileIsEmbedded()
    {
        var files = typeof(Example).Assembly.GetManifestResourceNames()
            .Where(resource => resource.StartsWith("Scenarios/", StringComparison.Ordinal));
        var pages = ExampleCatalog.All.Where(example => example.Scenario is not null)
            .Select(example => $"{TestScenario.TypeOf(example)}.cs");

        Assert.Equal(pages.SelectMany(page => new[] { $"Scenarios/Bunit/{page}", $"Scenarios/Playwright/{page}" })
            .Order(), files.Order());
    }

    [Fact]
    public void Scenario_ShouldHoldOneTest_WhenItsFileIsRead()
    {
        // The page shows the first test in the file, so a second one would never show.
        var assembly = typeof(Example).Assembly;
        var counts = assembly.GetManifestResourceNames()
            .Where(resource => resource.StartsWith("Scenarios/", StringComparison.Ordinal)).Select(resource =>
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
                return (resource,
                    Tests: Regex.Count(reader.ReadToEnd(), @"^\s*\[(Fact|Theory)", RegexOptions.Multiline));
            });

        Assert.All(counts, file => Assert.Equal(1, file.Tests));
    }

    [Fact]
    public void ComponentPage_ShouldShowTheTestInBothPackages_WhenThePageHasAScenario()
    {
        var cut = ComponentPage("button");

        var bunit = cut.Find("[aria-label='The test in bUnit']").TextContent;
        var playwright = cut.Find("[aria-label='The test in Playwright']").TextContent;
        Assert.StartsWith("var cut = Render<ButtonSubmitExample>();", bunit);
        Assert.Contains("var send = cut.Tb().Button(\"Send report\");", bunit);
        Assert.EndsWith("Assert.Equal(\"Reports sent: 1\", cut.Find(\"output\").TextContent);", bunit.TrimEnd());
        Assert.StartsWith("var page = await OpenAsync(browser, \"/examples/button/submit\");", playwright);
        Assert.Contains("page.Tb().Button(\"Send report\")", playwright);
        // The sentence above the tests names the example they drive and links to it.
        var lead = cut.FindAll("p").Single(text => text.TextContent.Contains("A whole test of the"));
        Assert.Equal(("Submit", "components/button#submit"),
            (lead.QuerySelector("a")!.TextContent, lead.QuerySelector("a")!.GetAttribute("href")));
    }

    [Fact]
    public void ComponentPage_ShouldShowNoTest_WhenThePageHasNoScenario()
    {
        // Nothing is left to check once every page has its scenario.
        if (ExampleCatalog.All.FirstOrDefault(example => example.Scenario is null) is { } example)
        {
            var cut = ComponentPage(example.Name);

            Assert.Empty(cut.FindAll("[aria-label='The test in bUnit'], [aria-label='The test in Playwright']"));
        }
    }
}
