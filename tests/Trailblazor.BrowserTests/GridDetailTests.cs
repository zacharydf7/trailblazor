namespace Trailblazor.BrowserTests;

public class GridDetailTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static async Task<ILocator> OpenTrailsAsync(IPage page)
    {
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trails and their stages" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }).First)
            .ToHaveAttributeAsync("data-ready", "");
        return grid;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Details_ShouldOpenAndLetFocusMoveInAndOutOfANestedGrid_WhenUsedFromTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = await OpenTrailsAsync(page);
        var toggle = grid.GetByRole(AriaRole.Button, new() { Name = "Details for Cedar Flats" });
        var details = grid.Locator("[data-part=detail-cell]");

        await toggle.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(details).ToBeFocusedAsync();

        var stages = page.GetByRole(AriaRole.Grid, new() { Name = "Stages of Cedar Flats" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = stages }).Last)
            .ToHaveAttributeAsync("data-ready", "");
        await page.Keyboard.PressAsync("Enter");
        await Expect(stages.GetByRole(AriaRole.Columnheader, new() { Name = "Stage" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(stages.GetByRole(AriaRole.Gridcell, new() { Name = "River path" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(details).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(toggle).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(details).ToHaveCountAsync(0);
        await Expect(page.GetByLabel("Open details")).ToHaveTextAsync("No details open.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NestedGrid_ShouldSelectOnlyItsOwnRows_WhenCtrlAIsPressedInIt(string browser)
    {
        var page = await OpenAsync(browser, "/grid-detail");
        var stages = page.GetByRole(AriaRole.Grid, new() { Name = "Stages of Pine Hollow loop" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = stages }).Last)
            .ToHaveAttributeAsync("data-ready", "");

        await stages.GetByRole(AriaRole.Gridcell, new() { Name = "Falls loop", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+a");

        await Expect(stages.Locator("tr[aria-selected=true]")).ToHaveCountAsync(2);
        await Expect(page.GetByLabel("Trails chosen")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Details_ShouldKeepTheTabStopOfTheirOnlyButton_WhenFocusHasMovedOn(string browser)
    {
        var page = await OpenAsync(browser, "/grid-detail");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trail links" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }).Last)
            .ToHaveAttributeAsync("data-ready", "");
        var map = grid.GetByRole(AriaRole.Button, new() { Name = "Map of Cedar Flats" });

        await map.FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(map).ToBeFocusedAsync();
        await grid.GetByRole(AriaRole.Gridcell, new() { Name = "Cedar Flats", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(map).ToBeFocusedAsync();
        await Expect(map).Not.ToHaveAttributeAsync("tabindex", "-1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Details_ShouldOpenUnderTheirRow_WhenTheButtonIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");
        var grid = await OpenTrailsAsync(page);

        await grid.GetByRole(AriaRole.Button, new() { Name = "Details for Pine Hollow loop" }).ClickAsync();
        await grid.GetByRole(AriaRole.Button, new() { Name = "Details for Eagle Ridge traverse" }).ClickAsync();

        await Expect(page.GetByLabel("Open details"))
            .ToHaveTextAsync("Open: Pine Hollow loop, Eagle Ridge traverse.");
        await Expect(grid.Locator(":scope > tbody > tr")).ToHaveCountAsync(5);
        await Expect(grid.Locator(":scope > tbody > tr").Nth(1)).ToHaveAttributeAsync("data-part", "detail");
        await Expect(page.GetByRole(AriaRole.Grid, new() { Name = "Stages of Eagle Ridge traverse" })
            .GetByRole(AriaRole.Row)).ToHaveCountAsync(4);
    }
}
