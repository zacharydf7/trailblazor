using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class CalendarTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Calendar(IPage page, string label = "Hike date") =>
        page.GetByRole(AriaRole.Group, new() { Name = label, Exact = true });

    private static ILocator Grid(IPage page, string label = "Hike date") => Calendar(page, label).GetByRole(AriaRole.Grid);

    private static ILocator Cell(IPage page, string name, string label = "Hike date") =>
        Grid(page, label).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator Title(IPage page) => Calendar(page).Locator("[data-part=title]");

    private async Task<IPage> OpenOnTheValueAsync(string browser, string label = "Hike date")
    {
        var page = await OpenAsync(browser, "/calendar");
        await Expect(Grid(page, label)).ToHaveAttributeAsync("data-ready", "");
        await Grid(page, label).Locator("[tabindex='0']").FocusAsync();
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldMoveByDayWeekAndMonth_WhenTheArrowAndPageKeysArePressed(string browser)
    {
        var page = await OpenOnTheValueAsync(browser);

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Cell(page, "Thursday, June 4, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Cell(page, "Thursday, June 11, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Cell(page, "Saturday, June 13, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(Cell(page, "Monday, July 13, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+PageUp");

        await Expect(Cell(page, "Sunday, July 13, 2025")).ToBeFocusedAsync();
        await Expect(Title(page)).ToHaveTextAsync("July 2025");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldChooseTheDayTheKeysMovedTo_WhenEnterFollowsKeysThatRanAheadOfTheServer(
        string browser)
    {
        var page = await OpenOnTheValueAsync(browser);

        foreach (var key in new[] { "PageDown", "PageDown", "PageDown", "ArrowRight", "Enter" })
        {
            await page.Keyboard.PressAsync(key);
        }

        await Expect(page.GetByTestId("hike")).ToHaveTextAsync("2026-09-04");
        await Expect(Cell(page, "Friday, September 4, 2026")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldReachADayThroughTheYearsAndMonths_WhenOnlyTheKeyboardIsUsed(string browser)
    {
        var page = await OpenOnTheValueAsync(browser);
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Title(page)).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(Title(page)).ToHaveAttributeAsync("aria-label", "2026, choose a year");
        await page.Keyboard.PressAsync("Enter");
        await Expect(Title(page)).ToHaveTextAsync("2020–2029");
        await Expect(Title(page)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Tab");
        await Expect(Cell(page, "2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Cell(page, "2031")).ToBeFocusedAsync();
        await Expect(Title(page)).ToHaveTextAsync("2030–2039");
        await page.Keyboard.PressAsync("Enter");
        await Expect(Cell(page, "June 2031")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Cell(page, "Monday, February 3, 2031")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("hike")).ToHaveTextAsync("2026-06-03");
        await page.Keyboard.PressAsync("Space");
        await Expect(page.GetByTestId("hike")).ToHaveTextAsync("2031-02-03");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldFocusTheSameDayOfThePickedMonth_WhenTheTitleThenAMonthAreClicked(string browser)
    {
        var page = await OpenOnTheValueAsync(browser);

        await Title(page).ClickAsync();

        await Expect(Grid(page)).ToHaveAttributeAsync("data-view", "months");
        await Cell(page, "September 2026").ClickAsync();
        await Expect(Cell(page, "Thursday, September 3, 2026")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldMoveAlongRowsAndPages_WhenKeysArePressedAmongTheMonthsAndYears(string browser)
    {
        var page = await OpenOnTheValueAsync(browser);
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Enter");
        await Expect(Grid(page)).ToHaveAttributeAsync("data-view", "months");
        await Grid(page).Locator("[tabindex='0']").FocusAsync();

        await page.Keyboard.PressAsync("End");
        await Expect(Cell(page, "August 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Cell(page, "May 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(Cell(page, "May 2027")).ToBeFocusedAsync();
        await Title(page).ClickAsync();
        await Expect(Grid(page)).ToHaveAttributeAsync("data-view", "years");
        await Grid(page).Locator("[tabindex='0']").FocusAsync();
        await Expect(Cell(page, "2027")).ToBeFocusedAsync();
        // The decade's last row holds only 2028 and 2029, so End stops at 2029.
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("End");
        await Expect(Cell(page, "2029")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("PageUp");

        await Expect(Cell(page, "2019")).ToBeFocusedAsync();
        await Expect(Title(page)).ToHaveTextAsync("2010–2019");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldStayWithinTheYears1To9999_WhenKeysMoveNearEitherEnd(string browser)
    {
        var page = await OpenOnTheValueAsync(browser, "Last year");
        await page.Keyboard.PressAsync("PageDown");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Grid(page, "Last year").Locator("[data-date='9999-12-19']")).ToBeFocusedAsync();

        await Grid(page, "First years").Locator("[tabindex='0']").FocusAsync();
        var title = Calendar(page, "First years").Locator("[data-part=title]");
        await title.ClickAsync();
        await title.ClickAsync();
        await Expect(Grid(page, "First years")).ToHaveAttributeAsync("data-view", "years");
        await Grid(page, "First years").Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Grid(page, "First years").Locator("[data-date='0006-01-01']")).ToBeFocusedAsync();
        await Expect(title).ToHaveTextAsync("0–9");
        await page.Keyboard.PressAsync("PageUp");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Grid(page, "First years").Locator("[data-date='0007-01-01']")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldLeaveTheDateAlone_WhenAClosedDayIsPicked(string browser)
    {
        var page = await OpenOnTheValueAsync(browser);

        foreach (var key in new[] { "ArrowDown", "ArrowDown", "ArrowDown", "ArrowRight" })
        {
            await page.Keyboard.PressAsync(key);
        }

        await Expect(Cell(page, "Thursday, June 25, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(Cell(page, "Thursday, June 25, 2026")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(page.GetByTestId("hike")).ToHaveTextAsync("2026-06-03");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldMoveToTheNextDayWithTheLeftArrow_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenOnTheValueAsync(browser, "Right to left");

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Cell(page, "Thursday, June 4, 2026", "Right to left")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldDrawTheChosenDayInTheSystemHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/calendar", new() { ForcedColors = ForcedColors.Active });

        var colors = await Grid(page).EvaluateAsync<string[]>("grid => ['2026-06-03', '2026-06-04'].map(date => "
            + "getComputedStyle(grid.querySelector(`[data-date='${date}']`)).backgroundColor)");

        Assert.Equal(await SystemColorAsync(page, "Highlight"), colors[0]);
        Assert.NotEqual(colors[0], colors[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldPickADateInAnotherDecade_WhenUsedFromTheTestingHelpers(string browser)
    {
        var page = await OpenAsync(browser, "/calendar");

        await page.Tb().Calendar("Hike date").ChooseAsync(new DateOnly(2041, 3, 9));

        await Expect(page.GetByTestId("hike")).ToHaveTextAsync("2041-03-09");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldPostTheDate_WhenTheCalendarIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-calendar");

        await page.Tb().Calendar("Hike date").ChooseAsync(new DateOnly(2026, 9, 14));
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("2026-09-14");
    }
}
