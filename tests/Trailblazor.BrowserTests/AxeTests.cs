using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

// Every docs example must have zero WCAG 2.2 AA violations, in both themes and every engine.
public class AxeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    internal static readonly AxeRunOptions WcagAA = new()
    {
        RunOnly = new RunOnlyOptions
        {
            Type = "tag",
            Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"],
        },
    };

    public static TheoryData<string, string, string> ExamplesInBothThemes => new(
        from browser in AllBrowsers.Names
        from example in ExampleCatalog.All
        from theme in new[] { "light", "dark" }
        select (browser, example.Name, theme));

    [Theory, MemberData(nameof(ExamplesInBothThemes))]
    public async Task Example_ShouldHaveNoAccessibilityViolations_WhenRenderedInTheme(string browser, string example,
        string theme)
    {
        var page = await OpenAsync(browser, $"/examples/{example}");
        await SetThemeAsync(page, theme);

        AssertNoViolations(await page.RunAxe(WcagAA));
    }

    // Overlays aren't on the page until they open, so examples with an opener are also checked while open. A menu's
    // and a toggletip's example targets are their buttons, so the popups those buttons open are listed here.
    private static readonly Dictionary<string, TestTarget> Opened = new()
    {
        ["menu"] = new("menu", "Actions") { OpenedBy = new("button", "Actions") },
        ["label"] = new("dialog", "Information about Project code")
        {
            OpenedBy = new("button", "Information about Project code"),
        },
    };

    private static TestTarget? OpenedTarget(Example example) =>
        Opened.GetValueOrDefault(example.Name) ?? (example.Target?.OpenedBy is null ? null : example.Target);

    public static TheoryData<string, string, string> OpenedExamplesInBothThemes => new(
        from browser in AllBrowsers.Names
        from example in ExampleCatalog.All
        where OpenedTarget(example) is not null
        from theme in new[] { "light", "dark" }
        select (browser, example.Name, theme));

    [Theory, MemberData(nameof(OpenedExamplesInBothThemes))]
    public async Task Example_ShouldHaveNoAccessibilityViolations_WhenOpenInTheme(string browser, string example,
        string theme)
    {
        var target = OpenedTarget(ExampleCatalog.Find(example)!)!;
        var page = await OpenAsync(browser, $"/examples/{example}");
        await SetThemeAsync(page, theme);
        var opener = ExampleTargetTests.Find(page, target.OpenedBy!);
        await (target.OpenOnHover ? opener.HoverAsync() : opener.ClickAsync());
        var shown = ExampleTargetTests.Find(page, target);
        await Expect(shown).ToBeVisibleAsync();
        // Hovering the shown component keeps a toast from timing out while axe runs.
        if (!target.OpenOnHover)
        {
            await shown.HoverAsync();
        }

        await WaitForFadesAsync(page);

        AssertNoViolations(await page.RunAxe(WcagAA));
        await Expect(shown).ToBeVisibleAsync();
    }

    // The overlay example has no role to target, so this clicks its buttons to show both overlays: report, then page.
    // The page overlay blocks everything behind it, which axe then skips, so the report's overlay is checked first.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Overlay_ShouldHaveNoAccessibilityViolations_WhenShownInEitherTheme(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay");
        var overlays = page.Locator("[data-part=overlay]");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load the report" }).ClickAsync();
        await Expect(overlays).ToHaveCountAsync(1);
        await CheckBothThemesAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Show page overlay" }).ClickAsync();
        await Expect(overlays).ToHaveCountAsync(2);
        await CheckBothThemesAsync(page);
    }

    private static async Task CheckBothThemesAsync(IPage page)
    {
        foreach (var theme in new[] { "light", "dark" })
        {
            await SetThemeAsync(page, theme);
            await WaitForFadesAsync(page);

            AssertNoViolations(await page.RunAxe(WcagAA));
        }
    }

    // Waits out fade-ins so axe doesn't read half-faded colors, but not infinite animations like the loading spinner.
    private static Task WaitForFadesAsync(IPage page) => page.WaitForFunctionAsync("""
        () => document.getAnimations().every(animation => animation.playState !== 'running'
            || animation.effect?.getComputedTiming().iterations === Infinity)
        """);

    private static void AssertNoViolations(AxeResult result) => Assert.Empty(result.Violations.Select(violation =>
        $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
}
