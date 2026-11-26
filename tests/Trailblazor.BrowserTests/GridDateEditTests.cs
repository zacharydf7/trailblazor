namespace Trailblazor.BrowserTests;

public class GridDateEditTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DateCell_ShouldKeepItsEditOpenAndSaveThePickedDay_WhenItsCalendarIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/grid-date-edit");
        var (cell, editor) = await StartEditAsync(page);
        await Expect(editor.Locator("[data-part=input]")).ToHaveValueAsync("2026-06-03");

        await editor.GetByRole(AriaRole.Button, new() { Name = "Choose a date" }).ClickAsync();
        await page.Locator("[data-part=day][data-date='2026-06-20']").ClickAsync();
        await Expect(editor.Locator("[data-part=input]")).ToHaveAttributeAsync("data-value", "2026-06-20");
        await Expect(editor).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button, new() { Name = "Elsewhere" }).ClickAsync();

        await Expect(editor).ToHaveCountAsync(0);
        await Expect(cell).ToHaveTextAsync("2026-06-20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DateCell_ShouldKeepItsEditOpen_WhenItsCalendarPicksWithEnterAndClosesWithEscape(string browser)
    {
        var page = await OpenAsync(browser, "/grid-date-edit");
        var (cell, editor) = await StartEditAsync(page);
        var button = editor.GetByRole(AriaRole.Button, new() { Name = "Choose a date" });
        await Expect(button).ToHaveAttributeAsync("data-ready", "");
        await page.Keyboard.PressAsync("Tab");
        await Expect(button).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.Locator("[data-part=day][data-date='2026-06-03']")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("Enter");
        await Expect(editor.Locator("[data-part=input]")).ToHaveAttributeAsync("data-value", "2026-06-04");
        await Expect(button).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("Escape");

        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(editor).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button, new() { Name = "Elsewhere" }).ClickAsync();
        await Expect(cell).ToHaveTextAsync("2026-06-04");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TimeCell_ShouldOpenItsEditKeepingTheTime_WhenADigitIsTypedOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/grid-date-edit");
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trips" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        var cell = grid.Locator("tr[data-key='1'] td[data-column=Leaves]");
        await cell.ClickAsync();

        await page.Keyboard.PressAsync("9");

        var box = cell.Locator("[data-part=editor] input[type=time]");
        await Expect(box).ToBeFocusedAsync();
        await Expect(box).ToHaveValueAsync("07:30");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DateAndTimeCell_ShouldTabThroughItsBoxesAndSaveTheTime_WhenATimeIsTypedInIt(string browser)
    {
        // The WebKit build Playwright uses on Windows shows a text box for a time box, whose edits never reach the app.
        Assert.SkipWhen(browser == "webkit", "WebKit on Windows has no time box.");
        var page = await OpenAsync(browser, "/grid-date-edit");
        var (cell, editor) = await StartEditAsync(page, "Booked");
        await Expect(editor.Locator("[data-part=date-input]")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(editor.GetByRole(AriaRole.Button, new() { Name = "Choose a date" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        var time = editor.Locator("[data-part=time-input]");
        await Expect(time).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(editor.GetByRole(AriaRole.Button, new() { Name = "Choose a date" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await time.FillAsync("18:45");
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.Locator("td[data-column=Leaves] [data-part=editor] input")).ToBeFocusedAsync();
        await Expect(cell).ToHaveTextAsync("2026-05-01 18:45");
    }

    private static async Task<(ILocator Cell, ILocator Editor)> StartEditAsync(IPage page, string column = "Starts")
    {
        var grid = page.GetByRole(AriaRole.Grid, new() { Name = "Trips" });
        await Expect(page.Locator("[data-part=grid]").Filter(new() { Has = grid }))
            .ToHaveAttributeAsync("data-ready", "");
        var cell = grid.Locator($"tr[data-key='1'] td[data-column={column}]");
        await cell.ClickAsync();
        await page.Keyboard.PressAsync("F2");
        var editor = cell.Locator("[data-part=editor]");
        await Expect(editor).ToBeVisibleAsync();
        return (cell, editor);
    }
}
