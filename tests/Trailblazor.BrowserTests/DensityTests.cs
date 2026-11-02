namespace Trailblazor.BrowserTests;

public class DensityTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // An example, the part measured and how, its comfortable and compact sizes, and the button that shows it, if any.
    private static readonly (string Example, string Part, string Property, string Comfortable, string Compact,
        string? Opener)[] Parts =
    [
        ("select", ".tb-field__control", "height", "40px", "32px", null),
        ("tabs", ".tb-tabs__tab", "min-height", "40px", "32px", null),
        ("accordion", ".tb-accordion__trigger", "min-height", "44px", "32px", null),
        ("list", ".tb-list__item", "min-height", "44px", "32px", null),
        ("chip", ".tb-chip:not(.tb-chip--small, .tb-chip--large)", "height", "32px", "24px", null),
        ("table", ".tb-table--small td", "height", "32px", "24px", null),
        ("tree-view", ".tb-tree__item", "min-height", "32px", "28px", null),
        ("listbox", "[role=option]", "min-height", "32px", "28px", null),
        ("menu", "[role=menuitem]", "min-height", "32px", "28px", "Actions"),
        ("alert", ".tb-alert", "padding-top", "12px", "6px", null),
    ];

    public static TheoryData<string, string> PartsInEveryBrowser => new(
        from browser in AllBrowsers.Names from part in Parts select (browser, part.Example));

    [Theory, MemberData(nameof(PartsInEveryBrowser))]
    public async Task Density_ShouldResizeEachPart_WhenThePageTurnsCompact(string browser, string example)
    {
        var (_, selector, property, comfortable, compact, opener) = Parts.Single(part => part.Example == example);
        var page = await OpenAsync(browser, $"/examples/{example}");
        if (opener is not null)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = opener, Exact = true }).ClickAsync();
        }

        var part = page.Locator(selector).First;
        await Expect(part).ToHaveCSSAsync(property, comfortable);

        await page.EvaluateAsync("() => document.documentElement.dataset.tbDensity = 'compact'");

        await Expect(part).ToHaveCSSAsync(property, compact);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Density_ShouldApplyToARegionOnly_WhenAnElementSetsIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/list");

        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Campsites" }).GetByRole(AriaRole.Listitem).First)
            .ToHaveCSSAsync("min-height", "44px");
        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Trail conditions" })
            .GetByRole(AriaRole.Listitem).First).ToHaveCSSAsync("min-height", "32px");
    }
}
