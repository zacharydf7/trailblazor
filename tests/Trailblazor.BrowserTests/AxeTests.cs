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

    // Examples full of states (selection, fills, tones), checked in every palette in chromium alone, since colors don't
    // differ between engines. Graphite, the host's default, is included so the list follows PaletteCatalog. Examples
    // with a popup are also checked open, where their chosen and selected states show.
    private static readonly string[] StatefulExamples =
    [
        "button", "toggle-group", "chip", "alert", "badge", "checkbox", "select", "listbox", "tabs", "data-grid",
        "tree-view", "date-range-picker",
    ];

    public static TheoryData<string, string, string> StatefulExamplesInEachPaletteAndTheme => new(
        from example in StatefulExamples
        from palette in PaletteCatalog.All
        from theme in new[] { "light", "dark" }
        select (example, palette.Name, theme));

    [Theory, MemberData(nameof(StatefulExamplesInEachPaletteAndTheme))]
    public async Task Example_ShouldHaveNoAccessibilityViolations_WhenShownInEachPalette(string example, string palette,
        string theme)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}");
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);

        AssertNoViolations(await page.RunAxe(WcagAA));
        if (OpeningOf(ExampleCatalog.Find(example)!) is { } opening)
        {
            await CheckOpenAsync(page, opening);
        }
    }

    public static TheoryData<string> AllExamples => new(ExampleCatalog.All.Select(example => example.Name));

    // Compact density shrinks controls toward the 24px target size, so every example is checked in it in chromium, and
    // those with a popup are also checked open.
    [Theory, MemberData(nameof(AllExamples))]
    public async Task Example_ShouldHaveNoAccessibilityViolations_WhenTheDensityIsCompact(string example)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}");
        await page.EvaluateAsync("() => document.documentElement.dataset.tbDensity = 'compact'");
        await WaitForFadesAsync(page);

        AssertNoViolations(await page.RunAxe(WcagAA));
        if (OpeningOf(ExampleCatalog.Find(example)!) is { } opening)
        {
            await CheckOpenAsync(page, opening);
        }
    }

    // Popups aren't on the page until they open, so examples with one are also checked while open: those whose
    // target has an opener, and those listed here, whose target is the control or field the popup opens from.
    private sealed record Opening(TestTarget Shown, Func<IPage, Task> OpenAsync, bool ByHover = false);

    private static readonly Dictionary<string, Opening> Opened = new()
    {
        ["menu"] = new(new("menu", "Actions"), page => Button(page, "Actions").ClickAsync()),
        ["label"] = new(new("dialog", "Information about Project code"),
            page => Button(page, "Information about Project code").ClickAsync()),
        ["select"] = new(new("listbox", "Shipping"),
            page => ClickWhenReadyAsync(page.GetByRole(AriaRole.Combobox, new() { Name = "Shipping" }))),
        ["autocomplete"] = new(new("listbox", "Trailhead"), async page =>
        {
            var field = page.GetByRole(AriaRole.Combobox, new() { Name = "Trailhead" });
            await Expect(field).ToHaveAttributeAsync("data-ready", "");
            await field.PressSequentiallyAsync("ridge");
            await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Trailhead" }).GetByRole(AriaRole.Option))
                .ToHaveCountAsync(2);
        }),
        ["date-range-picker"] = new(new("dialog", "Choose dates"),
            page => ClickWhenReadyAsync(page.GetByRole(AriaRole.Group, new() { Name = "Trip dates" })
                .GetByRole(AriaRole.Button, new() { Name = "Choose dates" }))),
        ["color-picker"] = new(new("dialog", "Choose a color"),
            page => ClickWhenReadyAsync(ColorPickerTests.Field(page, "Trail marker")
                .GetByRole(AriaRole.Button, new() { Name = "Choose a color" }))),
    };

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // A control whose script opens its popup is clicked once the script has attached.
    private static async Task ClickWhenReadyAsync(ILocator control)
    {
        await Expect(control).ToHaveAttributeAsync("data-ready", "");
        await control.ClickAsync();
    }

    private static Opening? OpeningOf(Example example) =>
        Opened.GetValueOrDefault(example.Name) ?? (example.Target is { OpenedBy: { } opener } target
            ? new(target, page => target.OpenOnHover ? ExampleTargetTests.Find(page, opener).HoverAsync()
                : ExampleTargetTests.Find(page, opener).ClickAsync(), target.OpenOnHover)
            : null);

    public static TheoryData<string, string, string> OpenedExamplesInBothThemes => new(
        from browser in AllBrowsers.Names
        from example in ExampleCatalog.All
        where OpeningOf(example) is not null
        from theme in new[] { "light", "dark" }
        select (browser, example.Name, theme));

    [Theory, MemberData(nameof(OpenedExamplesInBothThemes))]
    public async Task Example_ShouldHaveNoAccessibilityViolations_WhenOpenInTheme(string browser, string example,
        string theme)
    {
        var page = await OpenAsync(browser, $"/examples/{example}");
        await SetThemeAsync(page, theme);

        await CheckOpenAsync(page, OpeningOf(ExampleCatalog.Find(example)!)!);
    }

    private static async Task CheckOpenAsync(IPage page, Opening opening)
    {
        await opening.OpenAsync(page);
        var shown = ExampleTargetTests.Find(page, opening.Shown);
        await Expect(shown).ToBeVisibleAsync();
        // Hovering the shown component keeps a toast from timing out while axe runs.
        if (!opening.ByHover)
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

    private static void AssertNoViolations(AxeResult result) => Assert.Empty(result.Violations.Select(violation =>
        $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
}
