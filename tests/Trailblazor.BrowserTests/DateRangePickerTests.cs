using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DateRangePickerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Picker(IPage page, string label = "Trip dates") =>
        page.GetByRole(AriaRole.Group, new() { Name = label });

    private static ILocator Box(IPage page, string box, string label = "Trip dates") =>
        Picker(page, label).GetByRole(AriaRole.Textbox, new() { Name = $"{label} {box}" });

    private static ILocator Calendar(IPage page) => page.GetByRole(AriaRole.Dialog, new() { Name = "Choose dates" });

    private static ILocator Day(IPage page, string name) =>
        Calendar(page).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<IPage> OpenWithCalendarAsync(string browser, string label = "Trip dates",
        BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/date-range", options);
        var trigger = Picker(page, label).GetByRole(AriaRole.Button, new() { Name = "Choose dates" });
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        await trigger.ClickAsync();
        await Expect(Calendar(page).Locator("[data-part=day][tabindex='0']")).ToBeFocusedAsync();
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldOpenOnTheStartAndCommitTheRange_WhenTwoDaysArePicked(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Expect(Day(page, "Wednesday, June 3, 2026")).ToBeFocusedAsync();
        await Expect(Calendar(page).GetByRole(AriaRole.Grid)).ToHaveCountAsync(2);

        await Day(page, "Friday, June 12, 2026").ClickAsync();
        await Day(page, "Monday, June 15, 2026").ClickAsync();

        await Expect(Calendar(page)).ToBeHiddenAsync();
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-06-12 to 2026-06-15");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("1");
        await Expect(Box(page, "Start date")).ToHaveValueAsync("6/12/2026");
        await Expect(Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldMoveByDayWeekAndMonthAndShowNewMonths_WhenKeysArePressed(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(page, "Thursday, June 4, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Day(page, "Thursday, June 11, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Day(page, "Sunday, June 7, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Day(page, "Saturday, June 13, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(Day(page, "Monday, July 13, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(Day(page, "Thursday, August 13, 2026")).ToBeFocusedAsync();
        await Expect(Calendar(page).GetByRole(AriaRole.Grid).First).ToHaveAccessibleNameAsync("July 2026");
        await page.Keyboard.PressAsync("Shift+PageUp");
        await Expect(Day(page, "Wednesday, August 13, 2025")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+PageDown");
        await Expect(Day(page, "Thursday, August 13, 2026")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Space");

        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-08-13 to 2026-08-20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldCloseAndKeepTheRange_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Day(page, "Friday, June 12, 2026").ClickAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(Calendar(page)).ToBeHiddenAsync();
        await Expect(Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" })).ToBeFocusedAsync();
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-06-03 to 2026-06-10");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldMarkTheRangeAndLeaveClosedDaysUnpicked_WhenOpen(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        var selected = Calendar(page).Locator("td[aria-selected=true]");

        await Expect(selected).ToHaveCountAsync(8);
        await Expect(Day(page, "Thursday, June 25, 2026")).ToHaveAttributeAsync("aria-disabled", "true");
        await Day(page, "Monday, June 22, 2026").ClickAsync();
        await Day(page, "Thursday, June 25, 2026").ClickAsync(new() { Force = true });

        await Expect(Calendar(page)).ToBeVisibleAsync();
        await Expect(selected).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldPreviewTheRange_WhenThePointerRestsOnALaterDay(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Day(page, "Monday, June 22, 2026").ClickAsync();

        await Day(page, "Friday, June 26, 2026").HoverAsync();

        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(4);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldDropThePreview_WhenClosedAndOpenedAgain(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        var trigger = Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" });
        await Day(page, "Monday, June 22, 2026").ClickAsync();
        await Day(page, "Friday, June 26, 2026").HoverAsync();
        await Expect(Calendar(page).Locator("[data-preview]")).Not.ToHaveCountAsync(0);

        await page.Keyboard.PressAsync("Escape");
        await trigger.ClickAsync();

        await Expect(Day(page, "Wednesday, June 3, 2026")).ToBeFocusedAsync();
        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(0);
    }

    // Kiritimati and Pago Pago are 25 hours apart, so at least one of them is never on the server's date.
    [Theory]
    [InlineData("chromium", "Pacific/Kiritimati")]
    [InlineData("chromium", "Pacific/Pago_Pago")]
    [InlineData("firefox", "Pacific/Kiritimati")]
    [InlineData("firefox", "Pacific/Pago_Pago")]
    [InlineData("webkit", "Pacific/Kiritimati")]
    [InlineData("webkit", "Pacific/Pago_Pago")]
    public async Task Calendar_ShouldMarkTheBrowsersToday_WhenItsTimeZoneDiffersFromTheServers(string browser,
        string timeZone)
    {
        var page = await OpenWithCalendarAsync(browser, "Next trip", new() { TimezoneId = timeZone });

        var today = await page.EvaluateAsync<string>("() => new Date().toLocaleDateString('sv-SE')");

        await Expect(Calendar(page).Locator("[aria-current=date]")).ToHaveAttributeAsync("data-date", today);
        await Expect(Calendar(page).Locator("[aria-current=date]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Preset_ShouldCommitItsRangeAndClose_WhenChosen(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);

        await Calendar(page).GetByRole(AriaRole.Button, new() { Name = "All of August" }).ClickAsync();

        await Expect(Calendar(page)).ToBeHiddenAsync();
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-08-01 to 2026-08-31");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldCommitBothDates_WhenTheyReadInOrder(string browser)
    {
        var page = await OpenAsync(browser, "/date-range");

        await Box(page, "Start date").FillAsync("6/20/2026");
        await Box(page, "End date").FillAsync("6/27/2026");
        await Box(page, "End date").PressAsync("Enter");

        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-06-20 to 2026-06-27");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldExplainAndMarkTheBox_WhenADateIsWrong(string browser)
    {
        var page = await OpenAsync(browser, "/date-range");
        var start = Box(page, "Start date");
        var end = Box(page, "End date");

        await start.FillAsync("June");
        await start.PressAsync("Enter");
        await Expect(start).ToHaveAccessibleDescriptionAsync("Enter the start date as MM/DD/YYYY.");
        await Expect(start).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(end).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await start.FillAsync("6/20/2026");
        await start.PressAsync("Enter");
        await Expect(end).ToHaveAccessibleDescriptionAsync("The end date is before the start date.");
        await Expect(end).ToHaveAttributeAsync("aria-invalid", "true");
        await end.FillAsync("6/25/2026");
        await end.PressAsync("Enter");
        await Expect(end).ToHaveAccessibleDescriptionAsync("6/25/2026 can't be chosen.");
        await end.FillAsync("6/26/2026");
        await end.PressAsync("Enter");

        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-06-20 to 2026-06-26");
        await Expect(start).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(end).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Clear_ShouldEmptyBothDatesAndFocusTheStart_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/date-range");

        await Picker(page).GetByRole(AriaRole.Button, new() { Name = "Clear" }).ClickAsync();

        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("none to none");
        await Expect(Box(page, "Start date")).ToBeFocusedAsync();
        await Expect(Box(page, "End date")).ToHaveValueAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Value_ShouldShowWithoutRaisingAChange_WhenTheParentSetsIt(string browser)
    {
        var page = await OpenAsync(browser, "/date-range");

        await page.GetByRole(AriaRole.Button, new() { Name = "Set from parent" }).ClickAsync();

        await Expect(Box(page, "Start date")).ToHaveValueAsync("9/1/2026");
        await Expect(Box(page, "End date")).ToHaveValueAsync("9/5/2026");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Typing_ShouldKeepTheTimeOfTheDateLeftAlone_WhenTheOtherChanges(string browser)
    {
        var page = await OpenAsync(browser, "/date-range");
        var end = Box(page, "End date", "Created");

        await end.FillAsync("03/30/2026");
        await end.PressAsync("Enter");

        await Expect(page.GetByTestId("created"))
            .ToHaveTextAsync("2026-03-01T08:30:00.0000000 to 2026-03-30T00:00:00.0000000");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldTypeTheDatesInTheFieldsFormat_WhenChoosing(string browser)
    {
        var page = await OpenAsync(browser, "/date-range");

        await page.Tb().DateRangePicker("Trip dates").ChooseAsync(new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 9));
        await page.Tb().DateRangePicker("Created").ChooseAsync(new DateOnly(2026, 4, 1), null);

        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-07-02 to 2026-07-09");
        await Expect(page.GetByTestId("created")).ToHaveTextAsync("2026-04-01T00:00:00.0000000 to ");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldWaitForTheInteractiveField_WhenThePageIsStillPrerendered(string browser)
    {
        var page = await OpenStaticAsync(browser, "/date-range");

        await page.Tb().DateRangePicker("Trip dates").ChooseAsync(new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 9));

        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-07-02 to 2026-07-09");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldPostTheBrowsersDateBoxes_WhenUsedAsAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-date-range");

        await page.Tb().DateRangePicker("Trip dates").ChooseAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 4));
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("2026-08-01 to 2026-08-04");
        await Expect(Box(page, "End date")).ToHaveValueAsync("2026-08-04");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldHaveNoAccessibilityViolations_WhenOpen(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Day(page, "Monday, June 22, 2026").ClickAsync();

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }
}
