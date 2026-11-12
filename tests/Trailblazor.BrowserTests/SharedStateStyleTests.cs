namespace Trailblazor.BrowserTests;

// Hover, pressed and disabled states follow the shared rules; a list box filter leaves its focus ring to the field.
public class SharedStateStyleTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // An example and a row in it that hovers.
    private static readonly (string Example, string Selector)[] HoverRows =
    [
        ("app-shell", ".tb-nav-menu__link:not(.tb-nav-menu__link--current)"),
        ("segmented-control", ".tb-radio-group--segmented .tb-radio:not(:has(:checked, :disabled))"),
        ("stepper", "button.tb-stepper__header"),
    ];

    public static TheoryData<string, string> HoverRowsInEveryBrowser => new(
        from browser in AllBrowsers.Names from row in HoverRows select (browser, row.Example));

    [Theory, MemberData(nameof(HoverRowsInEveryBrowser))]
    public async Task Row_ShouldTakeTheSharedHoverLayer_WhenHovered(string browser, string example)
    {
        var page = await OpenAsync(browser, $"/examples/{example}");
        var row = page.Locator(HoverRows.Single(entry => entry.Example == example).Selector).First;

        await row.HoverAsync();

        var (shown, expected) = await ColorsAsync(row, "background-color", "var(--tb-color-hover)");
        Assert.Equal(expected, shown);
    }

    [Theory]
    [MemberData(nameof(PressedRowsInEveryBrowser))]
    public async Task Row_ShouldTakeTheSharedPressedLayer_WhenPressed(string browser, string example)
    {
        var page = await OpenAsync(browser, $"/examples/{example}");
        var row = page.Locator(HoverRows.Single(entry => entry.Example == example).Selector).First;

        await row.HoverAsync();
        await page.Mouse.DownAsync();

        var (shown, expected) = await ColorsAsync(row, "background-color", "var(--tb-color-pressed)");
        await page.Mouse.UpAsync();
        Assert.Equal(expected, shown);
    }

    public static TheoryData<string, string> PressedRowsInEveryBrowser => new(
        from browser in AllBrowsers.Names from example in new[] { "app-shell", "stepper" } select (browser, example));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SmallSegment_ShouldKeepA24pxTarget_WhenThePageTurnsCompact(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control");
        var segment = page.Locator(".tb-radio-group--segmented.tb-radio-group--small .tb-radio").First;
        await Expect(segment).ToHaveCSSAsync("min-height", "26px");

        await page.EvaluateAsync("() => document.documentElement.dataset.tbDensity = 'compact'");

        await Expect(segment).ToHaveCSSAsync("min-height", "24px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MenuItem_ShouldTakeTheDisabledTextColor_WhenDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Actions", Exact = true }).First.ClickAsync();
        var archive = page.GetByRole(AriaRole.Menuitem, new() { Name = "Archive" }).First;

        var (shown, expected) = await ColorsAsync(archive, "color", "var(--tb-color-text-disabled)");

        Assert.Equal(expected, shown);
        await Expect(archive).ToHaveCSSAsync("opacity", "1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MenuItem_ShouldTakeGrayText_WhenDisabledInForcedColors(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu", new() { ForcedColors = ForcedColors.Active });
        await page.GetByRole(AriaRole.Button, new() { Name = "Actions", Exact = true }).First.ClickAsync();

        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Archive" }).First)
            .ToHaveCSSAsync("color", await SystemColorAsync(page, "GrayText"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeRow_ShouldTakeNoHoverFill_WhenDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var row = page.GetByRole(AriaRole.Tree, new() { Name = "Open and closed regions" })
            .Locator(".tb-tree__item[aria-disabled=true]").First;

        await row.HoverAsync();

        await Expect(row).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OptionBox_ShouldDim_WhenTheOptionIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox");
        var option = page.GetByRole(AriaRole.Listbox, new() { Name = "Gear to pack" }).GetByRole(AriaRole.Option).First;

        await option.EvaluateAsync("option => option.setAttribute('aria-disabled', 'true')");

        await Expect(option.Locator(".tb-select__box")).Not.ToHaveCSSAsync("opacity", "1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBoxFilter_ShouldTakeTheListsTextSizeAndLeaveTheRingToIt_WhenFocused(string browser)
    {
        var page = await OpenAsync(browser, "/listbox-filter");
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Big trail", Exact = true });
        var filter = list.Locator("xpath=..").GetByRole(AriaRole.Textbox);

        await filter.FocusAsync();

        await Expect(filter).ToHaveCSSAsync("font-size", await list.EvaluateAsync<string>(
            "list => getComputedStyle(list).fontSize"));
        await Expect(filter).ToHaveCSSAsync("outline-style", "none");
    }

    // The element's computed color for the property, and the color a token gives in the same place.
    private static async Task<(string Shown, string Expected)> ColorsAsync(ILocator element, string property,
        string token)
    {
        var colors = await element.EvaluateAsync<string[]>("""
            (element, [property, token]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, token);
                element.parentElement.append(probe);
                const expected = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return [getComputedStyle(element).getPropertyValue(property), expected];
            }
            """, new[] { property, token });
        return (colors[0], colors[1]);
    }
}
