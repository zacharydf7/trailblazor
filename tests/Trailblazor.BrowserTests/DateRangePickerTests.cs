using System.Text.RegularExpressions;
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
    public async Task Calendar_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeld(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Expect(Day(page, "Wednesday, June 3, 2026, Start date")).ToBeFocusedAsync();

        foreach (var key in new[] { "Alt+ArrowRight", "Control+ArrowRight", "Meta+ArrowRight", "Control+Home" })
        {
            await page.Keyboard.PressAsync(key);
            await Expect(Day(page, "Wednesday, June 3, 2026, Start date")).ToBeFocusedAsync();
        }

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(page, "Thursday, June 4, 2026")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldOpenOnTheStartAndCommitTheRange_WhenTwoDaysArePicked(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Expect(Day(page, "Wednesday, June 3, 2026, Start date")).ToBeFocusedAsync();
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
    public async Task Calendar_ShouldMoveToTheNextDayWithTheLeftArrow_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Day(page, "Thursday, June 4, 2026")).ToBeFocusedAsync();
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
    public async Task Calendar_ShouldOutlineTheEndsAndDashThePreview_WhenColorsAreForced(string browser)
    {
        var page = await OpenWithCalendarAsync(browser, options: new() { ForcedColors = ForcedColors.Active });
        await Day(page, "Monday, June 22, 2026").ClickAsync();
        var start = Day(page, "Monday, June 22, 2026, Start date");

        await Day(page, "Friday, June 26, 2026").HoverAsync();

        await Expect(start).ToHaveCSSAsync("outline-style", "solid");
        await Expect(Calendar(page).Locator("[data-preview]").Last).ToHaveCSSAsync("outline-style", "dashed");
        // A day the arrows move to keeps its focus ring inside the preview.
        await page.Keyboard.PressAsync("ArrowRight");
        var focused = Day(page, "Tuesday, June 23, 2026");
        await Expect(focused).ToBeFocusedAsync();
        await Expect(focused).ToHaveAttributeAsync("data-preview", "");
        await Expect(focused).ToHaveCSSAsync("outline-style", "solid");
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
    public async Task Calendar_ShouldPreviewTheRange_WhenTheKeysMoveFocusToALaterDay(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Day(page, "Monday, June 22, 2026").ClickAsync();
        await Expect(Calendar(page)).ToHaveAttributeAsync("data-anchor", "2026-06-22");

        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(2);
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldDropThePreview_WhenAPressBecomesAScroll(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Day(page, "Monday, June 22, 2026").ClickAsync();
        await Day(page, "Friday, June 26, 2026").HoverAsync();
        await Expect(Calendar(page).Locator("[data-preview]")).Not.ToHaveCountAsync(0);

        await Day(page, "Friday, June 26, 2026").EvaluateAsync(
            "day => day.dispatchEvent(new PointerEvent('pointercancel', { bubbles: true }))");

        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldKeepItsKeys_WhenABlankPartIsPressed(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        var focused = Calendar(page).Locator("[data-part=day][tabindex='0']");
        var trigger = Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" });

        await Calendar(page).ClickAsync(new() { Position = new() { X = 3, Y = 3 } });

        await Expect(focused).ToBeFocusedAsync();
        var from = await focused.GetAttributeAsync("data-date");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Calendar(page).Locator("[data-part=day]:focus")).Not.ToHaveAttributeAsync("data-date", from!);
        await page.Keyboard.PressAsync("Escape");
        await Expect(Calendar(page)).Not.ToBeVisibleAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldCloseAndKeepTheRange_WhenItsButtonIsClickedAgain(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Day(page, "Monday, June 22, 2026").ClickAsync();

        await Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" }).ClickAsync();

        await Expect(Calendar(page)).Not.ToBeVisibleAsync();
        await Expect(Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" }))
            .ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-06-03 to 2026-06-10");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("0");
    }

    [Fact]
    public async Task Calendar_ShouldCloseAndKeepTheRange_WhenItsButtonIsTappedAgain()
    {
        var page = await OpenWithCalendarAsync("chromium", options: new() { HasTouch = true });

        await Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" }).TapAsync();

        await Expect(Calendar(page)).Not.ToBeVisibleAsync();
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-06-03 to 2026-06-10");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldPreviewTheWholeRange_WhenPagingKeepsFocusOnTheSameButton(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        // Back to January, then forward into February as the second month shown, whose days line up with March's.
        var focused = Calendar(page).Locator("[data-part=day]:focus");
        foreach (var (key, month) in new[] { ("PageUp", "05"), ("PageUp", "04"), ("PageUp", "03"), ("PageUp", "02"),
            ("PageUp", "01"), ("PageDown", "02") })
        {
            await page.Keyboard.PressAsync(key);
            await Expect(focused).ToHaveAttributeAsync("data-date", new Regex($"^2026-{month}-"));
        }

        await Day(page, "Tuesday, February 3, 2026").ClickAsync();
        await Expect(Calendar(page)).ToHaveAttributeAsync("data-anchor", "2026-02-03");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(1);

        await page.Keyboard.PressAsync("PageDown");

        await Expect(focused).ToHaveAttributeAsync("data-date", new Regex("^2026-03-"));
        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(29);
    }

    // The keys are pressed without waiting, so they can land before the server has answered the ones before them.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldLandOnTheDayTheKeysMovedTo_WhenTheyRunAheadOfTheServer(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);

        foreach (var key in new[] { "PageUp", "PageUp", "PageUp", "PageUp", "PageUp", "PageDown", "ArrowRight" })
        {
            await page.Keyboard.PressAsync(key);
        }

        await Expect(Day(page, "Wednesday, February 4, 2026")).ToBeFocusedAsync();
        await Expect(Calendar(page).GetByRole(AriaRole.Grid).First).ToHaveAccessibleNameAsync("January 2026");
    }

    public static TheoryData<string, string> BrowsersAndPickKeys => new(
        from browser in AllBrowsers.Names
        from key in new[] { "Enter", "Space" }
        select (browser, key));

    // The keys are pressed without waiting, so the pick can land on a button the server hasn't moved to April yet.
    [Theory, MemberData(nameof(BrowsersAndPickKeys))]
    public async Task Calendar_ShouldPickTheDayTheKeysMovedTo_WhenEnterOrSpaceRunsAheadOfTheServer(string browser,
        string key)
    {
        var page = await OpenWithCalendarAsync(browser);

        foreach (var press in new[] { "PageUp", "PageUp", key })
        {
            await page.Keyboard.PressAsync(press);
        }

        await Expect(Calendar(page)).ToHaveAttributeAsync("data-anchor", "2026-04-03");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldPickTheDayAndKeepMoving_WhenAnArrowFollowsAnEnterThatRanAheadOfTheServer(
        string browser)
    {
        var page = await OpenWithCalendarAsync(browser);

        foreach (var press in new[] { "PageUp", "PageUp", "Enter", "ArrowRight" })
        {
            await page.Keyboard.PressAsync(press);
        }

        await Expect(Calendar(page)).ToHaveAttributeAsync("data-anchor", "2026-04-03");
        await Expect(Day(page, "Saturday, April 4, 2026")).ToBeFocusedAsync();
        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldPreviewTheRange_WhenAnArrowFollowsEnterBeforeTheServerAnswers(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);

        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Calendar(page)).ToHaveAttributeAsync("data-anchor", "2026-06-03");
        await Expect(Calendar(page).Locator("[data-preview]")).ToHaveCountAsync(1);
    }

    // Safari on a Mac doesn't focus a clicked button, so the click is sent without moving focus.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldMoveOnFromThePickedDay_WhenAClickLeavesFocusOnThePage(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await page.EvaluateAsync("() => document.activeElement.blur()");

        await Day(page, "Monday, June 22, 2026").DispatchEventAsync("click");
        await Expect(Calendar(page)).ToHaveAttributeAsync("data-anchor", "2026-06-22");
        await Expect(Day(page, "Monday, June 22, 2026, Start date")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Day(page, "Tuesday, June 23, 2026")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldKeepOneTabStop_WhenAMonthButtonClickLeavesFocusOnThePage(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await page.EvaluateAsync("() => document.activeElement.blur()");
        var previous = Calendar(page).GetByRole(AriaRole.Button, new() { Name = "Previous month" });

        await previous.DispatchEventAsync("click");
        await Expect(Calendar(page).GetByRole(AriaRole.Grid).First).ToHaveAccessibleNameAsync("May 2026");
        await Expect(Calendar(page).Locator("[data-part=day]:focus")).ToHaveCountAsync(0);
        await previous.DispatchEventAsync("click");
        await Expect(Calendar(page).GetByRole(AriaRole.Grid).First).ToHaveAccessibleNameAsync("April 2026");

        await Expect(Calendar(page).Locator("[data-part=day][tabindex='0']")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldPlaceItselfAndTakeFocus_WhenOpenedBeforeItsScriptHasLoaded(string browser)
    {
        var (page, script) = await OpenWithAScriptHeldAsync(browser, "/date-range", "**/date-range-picker.*js");
        var trigger = Picker(page).GetByRole(AriaRole.Button, new() { Name = "Choose dates" });

        await trigger.ClickAsync();
        await Expect(Calendar(page)).ToBeVisibleAsync();
        await Expect(trigger).Not.ToHaveAttributeAsync("data-ready", "");
        script.SetResult();

        await Expect(Day(page, "Wednesday, June 3, 2026, Start date")).ToBeFocusedAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Calendar(page)).ToHaveAttributeAsync("style", new Regex("--tb-anchored-offset"));
    }

    // A toggle still queued when the script replays the opening must not open the calendar a second time.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldLeaveFocusWhereItIs_WhenAnOpeningIsReportedTwice(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        var previous = Calendar(page).GetByRole(AriaRole.Button, new() { Name = "Previous month" });
        await previous.FocusAsync();

        await Calendar(page).EvaluateAsync(
            "panel => panel.dispatchEvent(Object.assign(new Event('toggle'), { newState: 'open' }))");

        await Expect(previous).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldClose_WhenShiftTabMovesFocusBackToItsButton(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Calendar(page).Locator("button").First.FocusAsync();

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(Calendar(page)).Not.ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Calendar_ShouldCloseAndKeepTheRange_WhenClickedOutside(string browser)
    {
        var page = await OpenWithCalendarAsync(browser);
        await Day(page, "Monday, June 22, 2026").ClickAsync();

        await page.Mouse.ClickAsync(page.ViewportSize!.Width - 4, page.ViewportSize.Height - 4);

        await Expect(Calendar(page)).Not.ToBeVisibleAsync();
        await Expect(page.GetByTestId("trip")).ToHaveTextAsync("2026-06-03 to 2026-06-10");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("0");
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

        await Expect(Day(page, "Wednesday, June 3, 2026, Start date")).ToBeFocusedAsync();
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
        await Day(page, "Monday, June 15, 2026").HoverAsync();
        var hovered = await page.RunAxe(AxeTests.WcagAA);
        // Found by its date, since picking it adds "Start date" to its name.
        var day = Calendar(page).Locator("[data-date='2026-06-22']");
        await day.ClickAsync();
        // The click's round trip to the server adds the start class after ClickAsync returns.
        await Expect(day).ToHaveClassAsync(new Regex("tb-date-range__day--start"));

        var chosen = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(hovered.Violations.Concat(chosen.Violations).Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }
}
