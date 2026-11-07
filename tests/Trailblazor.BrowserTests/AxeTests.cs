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

    public static TheoryData<string, string> SelectsInBothThemes => new(
        from name in new[]
        {
            "Notify me about", "Trails to hike", "Route", "Time zone", "Guide", "Permit number", "Huts", "Campsite",
            "Saved trail", "Saved routes", "Start day", "Permit type", "Map style", "Season",
        }
        from theme in new[] { "light", "dark" }
        select (name, theme));

    // Each section whose field opens is checked open, in chromium alone since colors don't differ.
    [Theory, MemberData(nameof(SelectsInBothThemes))]
    public async Task Select_ShouldHaveNoAccessibilityViolations_WhenItsPopupIsOpen(string name, string theme)
    {
        var page = await OpenAsync("chromium", "/examples/select");
        await SetThemeAsync(page, theme);
        var field = page.GetByRole(AriaRole.Combobox, new() { Name = name, Exact = true });

        await ClickWhenReadyAsync(field);
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");
        if (name == "Guide")
        {
            // Its options load as it opens, so axe waits for them rather than checking a list that's about to change.
            await Expect(page.GetByRole(AriaRole.Option, new() { Name = "Katherine" })).ToBeVisibleAsync();
        }

        AssertNoViolations(await page.RunAxe(WcagAA));
    }

    public static TheoryData<string, string, string, bool, string> AutocompleteStatesInBothThemes => new(
        from state in new[]
        {
            ("Region", "north", "North ridge", false),
            ("Region", "zzz", "No matches", false),
            ("Town", "a", "Type to search", false),
            ("Campsite", "alder", "Couldn't load suggestions.", true),
            ("Lake", "mir", "Mirror", false),
            ("Ranger", "ada", "Ada", false),
            ("Hut", "ald", "Alder", false),
            ("Pack weight (kg)", "7", "7.5", false),
            ("Shelter", "zzz", "No shelter has that name", false),
            ("Trail number", "300", "Trail 300", false),
            ("Guide", "ada", "Ada", false),
            ("Map style", "sat", "Satellite", false),
            ("Peak", "eag", "Eagle", false),
            ("River", "wil", "Willow", false),
        }
        from theme in new[] { "light", "dark" }
        select (state.Item1, state.Item2, state.Item3, state.Item4, theme));

    // Each section whose field opens is checked open, and each message its list settles on, in chromium alone.
    [Theory, MemberData(nameof(AutocompleteStatesInBothThemes))]
    public async Task Autocomplete_ShouldHaveNoAccessibilityViolations_WhenItsListShowsEachState(string name,
        string typed, string shown, bool fail, string theme)
    {
        var page = await OpenAsync("chromium", "/examples/autocomplete");
        await SetThemeAsync(page, theme);

        await CheckOpenAsync(page, new(new("listbox", name), async opened =>
        {
            var field = opened.GetByRole(AriaRole.Combobox, new() { Name = name, Exact = true });
            await Expect(field).ToHaveAttributeAsync("data-ready", "");
            if (fail)
            {
                await opened.GetByRole(AriaRole.Checkbox, new() { Name = "Make the next search fail" }).CheckAsync();
            }

            // Some of these fields start with a value, which would otherwise stay in front of what's typed.
            await field.ClearAsync();
            await field.PressSequentiallyAsync(typed);
            await Expect(opened.GetByRole(AriaRole.Listbox, new() { Name = name }).GetByText(shown)).ToBeVisibleAsync();

            // Axe skips contrast on anything aria-disabled, and these messages are information, so they're measured.
            await opened.EvaluateAsync(
                "() => document.querySelectorAll('[data-part=status]').forEach(status => status.removeAttribute('aria-disabled'))");
        }));
    }

    public static TheoryData<string, string> ColorPickersInBothThemes => new(
        from name in new[]
        {
            "Map overlay", "Tent color", "Highlight", "Club color", "Sign color", "Boundary color", "Group color",
            "Line color", "Warning color", "Shelter color",
        }
        from theme in new[] { "light", "dark" }
        select (name, theme));

    // Each section whose picker opens is checked open, in chromium alone.
    [Theory, MemberData(nameof(ColorPickersInBothThemes))]
    public async Task ColorPicker_ShouldHaveNoAccessibilityViolations_WhenItsPopupIsOpen(string name, string theme)
    {
        var page = await OpenAsync("chromium", "/examples/color-picker");
        await SetThemeAsync(page, theme);
        var open = name == "Shelter color" ? "Open the shelter colors" : "Choose a color";

        await CheckOpenAsync(page, new(new("dialog", open), opened => ClickWhenReadyAsync(
            ColorPickerTests.Field(opened, name).GetByRole(AriaRole.Button, new() { Name = open }))));
    }

    public static TheoryData<string, string> DateRangePickersInBothThemes => new(
        from name in new[]
        {
            "Report period", "Site visit", "Vacation in Germany", "Closure dates", "Created between", "Booking dates",
            "Survey dates", "Permit dates", "Travel window", "Filled dates", "Hut stay", "Shuttle",
        }
        from theme in new[] { "light", "dark" }
        select (name, theme));

    // Each section whose picker opens is checked open, in chromium alone.
    [Theory, MemberData(nameof(DateRangePickersInBothThemes))]
    public async Task DateRangePicker_ShouldHaveNoAccessibilityViolations_WhenItsCalendarIsOpen(string name, string theme)
    {
        var page = await OpenAsync("chromium", "/examples/date-range-picker");
        await SetThemeAsync(page, theme);
        var open = name == "Shuttle" ? "Open the ride calendar" : "Choose dates";

        await CheckOpenAsync(page, new(new("dialog", open), opened => ClickWhenReadyAsync(
            opened.GetByRole(AriaRole.Group, new() { Name = name, Exact = true })
                .GetByRole(AriaRole.Button, new() { Name = open }))));
    }

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

    // A failed save shows an alert in the dialog, which the opened dialog's own row can't reach.
    [Theory]
    [InlineData("light"), InlineData("dark")]
    public async Task Dialog_ShouldHaveNoAccessibilityViolations_WhenSavingHasFailed(string theme)
    {
        var page = await OpenAsync("chromium", "/examples/dialog");
        await SetThemeAsync(page, theme);
        await Button(page, "Book the hut").ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Book the hut" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Book", Exact = true }).ClickAsync();
        await Expect(dialog.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Try again" })).ToBeVisibleAsync();
        await WaitForFadesAsync(page);

        AssertNoViolations(await page.RunAxe(WcagAA));
    }

    private static readonly (string Example, string Opener, string Role, string Shown)[] OverlaySections =
    [
        ("tooltip", "Below", "tooltip", "Shown below the button"),
        ("tooltip", "Beside", "tooltip", "Shown at the end"),
        ("tooltip", "Save", "tooltip", "Saves a copy of the trail"),
        ("tooltip", "Long description", "tooltip", "This description is long"),
        ("tooltip", "Slow to appear", "tooltip", "Appears after a second"),
        ("tooltip", "Slow to hide", "tooltip", "Stays for two seconds"),
        ("tooltip", "Delete", "tooltip", "Delete the selection"),
        ("popover", "Trip details", "dialog", "Trip details"),
        ("popover", "Notifications", "dialog", "Notifications"),
        ("popover", "Packing list", "dialog", "Packing list"),
        ("popover", "Share", "dialog", "Share this trail"),
        ("popover", "Keyboard shortcuts", "dialog", "Keyboard shortcuts"),
        ("popover", "Opens beside", "dialog", "Opens beside"),
        ("popover", "Ada Lovelace", "dialog", "Ada Lovelace"),
        ("menu", "More options", "menu", "More options"),
        ("menu", "View", "menu", "View"),
        ("menu", "Filters", "menu", "Filters"),
        ("menu", "Guides", "menu", "Guides"),
        ("menu", "RB account", "menu", "RB account"),
        ("menu", "Trail tools", "menu", "Trail tools"),
        ("menu", "Opens above", "menu", "Opens above"),
        ("menu", "Full width", "menu", "Full width"),
        ("menu", "Saved searches", "menu", "Saved searches"),
        ("dialog", "Delete report", "alertdialog", "Delete report?"),
        ("dialog", "Check the backup", "alertdialog", "Backup finished"),
        ("dialog", "Share", "dialog", "Share report"),
        ("dialog", "Full", "dialog", "A dialog at Full"),
        ("dialog", "Top", "dialog", "A dialog at Top"),
        ("dialog", "Without a header", "dialog", "Trail closed"),
        ("dialog", "With an icon in the title", "dialog", "Trail folder"),
        ("dialog", "Start at the safe choice", "dialog", "Discard the draft?"),
        ("dialog", "Require an answer", "dialog", "Accept the trail rules"),
        ("dialog", "Read the trail rules", "dialog", "Trail rules"),
        ("dialog", "Book the hut", "dialog", "Book the hut"),
        ("dialog", "Edit beside the page", "dialog", "Edit name"),
        ("dialog", "Take notes", "dialog", "Notes on the report"),
        ("dialog", "Open on the page", "dialog", "A dialog on the page"),
        ("dialog", "Open a form", "menu", "Open a form"),
        ("focus-scope", "Edit the note", "group", "Note"),
        ("focus-scope", "Focus the marked field", "group", "Your name"),
        ("focus-scope", "Leave focus where it is", "group", "Your town"),
        ("focus-scope", "Start the checklist", "group", "Checklist"),
    ];

    public static TheoryData<string, string, string, string, string> OverlaySectionsInBothThemes => new(
        from section in OverlaySections
        from theme in new[] { "light", "dark" }
        select (section.Example, section.Opener, section.Role, section.Shown, theme));

    // Each listed popup is checked open, in chromium alone. The export dialog closes itself too soon to be listed.
    [Theory, MemberData(nameof(OverlaySectionsInBothThemes))]
    public async Task OverlaySection_ShouldHaveNoAccessibilityViolations_WhenOpen(string example, string opener,
        string role, string shown, string theme)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}");
        await SetThemeAsync(page, theme);
        var byHover = role == "tooltip";

        await CheckOpenAsync(page, new(new(role, shown),
            opened => byHover ? Button(opened, opener).HoverAsync() : Button(opened, opener).ClickAsync(), byHover));
    }

    // Errors only show once a form is submitted, so this submits each of the form page's forms that starts out empty.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldHaveNoAccessibilityViolations_WhenShowingItsErrorsInEitherTheme(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");
        foreach (var submit in new[] { "Sign up", "Save limits", "Save contact", "Reserve", "Request permit",
            "Send report" })
        {
            await Button(page, submit).ClickAsync();
        }

        await Expect(page.Locator("[data-part=error]")).ToHaveCountAsync(10);
        await CheckBothThemesAsync(page);
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
