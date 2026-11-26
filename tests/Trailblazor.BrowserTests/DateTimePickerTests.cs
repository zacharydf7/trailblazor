using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DateTimePickerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator OpenButton(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Choose a date" });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldShowTheWholeDate_WhenItsFieldIsCentered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/date-time-picker");
        var date = page.Locator(".tb-field").Filter(new() { Has = page.GetByText("Gates open", new() { Exact = true }) })
            .Locator("[data-part=date-input]");
        await CenterFieldAsync(date);

        Assert.True(await ShowsWholeValueAsync(date));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldPickADayWithTheKeysAndKeepTheTime_WhenTheCalendarButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker");
        await Expect(OpenButton(page)).ToHaveAttributeAsync("data-ready", "");
        await OpenButton(page).FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Grid).GetByRole(AriaRole.Button,
            new() { Name = "Wednesday, June 3, 2026", Exact = true })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-06-10 14:30");
        await Expect(page.Tb().DateTimePicker("Starts").DateInput).ToHaveValueAsync("6/10/2026");
        await Expect(OpenButton(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldMoveFromTheDateToTheCalendarButtonToTheTime_WhenTabIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker");
        var picker = page.Tb().DateTimePicker("Starts");
        await Expect(OpenButton(page)).ToHaveAttributeAsync("data-ready", "");
        await picker.DateInput.FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(OpenButton(page)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(picker.TimeInput).ToBeFocusedAsync();
        await Expect(picker.TimeInput).ToHaveAccessibleNameAsync("Starts Time");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldKeepTheDate_WhenTheTimeBoxIsFilled(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker");
        var time = page.Tb().DateTimePicker("Starts").TimeInput;

        await time.FillAsync("09:15");
        await time.PressAsync("Tab");

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-06-03 09:15");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldKeepTheDateAndPickTheTime_WhenTheTimeBoxsListIsUsedWithTheKeys(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker");
        var time = page.Tb().DateTimePicker("Starts").TimeInput;
        await Expect(time).ToHaveAttributeAsync("data-ready", "");
        await time.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(time).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-06-03 15:00");
        await Expect(time).ToHaveValueAsync(new Regex(@"^3:00\sPM$"));
        await Expect(time).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldPickTheClickedTime_WhenTheClockButtonOpensTheList(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker");
        var time = page.Tb().DateTimePicker("Starts").TimeInput;
        await Expect(time).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Choose a time" }).ClickAsync();
        await Expect(time).ToBeFocusedAsync();
        await page.Locator($"[id='{await time.GetAttributeAsync("aria-controls")}'] [data-time='09:30']").ClickAsync();

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-06-03 09:30");
        await Expect(time).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldSetMidnight_WhenTheCultureWritesTwentyFourHourTimes(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker-german");

        await page.Tb().DateTimePicker("Ends").ChooseAsync(new DateTime(2026, 6, 4, 0, 0, 0));

        await Expect(page.GetByTestId("end")).ToHaveTextAsync("2026-06-04 00:00");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldTypeTheDateAndFillTheTime_WhenUsedFromTheTestingHelpers(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker");

        await page.Tb().DateTimePicker("Starts").ChooseAsync(new DateTime(2026, 9, 14, 16, 45, 0));

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-09-14 16:45");
        Assert.Equal(new DateTime(2026, 9, 14, 16, 45, 0), await page.Tb().DateTimePicker("Starts").ValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ValueAsync_ShouldWaitForTheInteractiveField_WhenThePageIsStillPrerendered(string browser)
    {
        var page = await OpenStaticAsync(browser, "/date-time-picker");

        Assert.Equal(new DateTime(2026, 6, 3, 14, 30, 0), await page.Tb().DateTimePicker("Starts").ValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ValueAsync_ShouldReturnNull_WhenTheFieldIsCleared(string browser)
    {
        var page = await OpenAsync(browser, "/date-time-picker");

        await page.Tb().DateTimePicker("Starts").ChooseAsync(null);

        Assert.Null(await page.Tb().DateTimePicker("Starts").ValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldPostTheValue_WhenThePickerIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-date-time-picker");

        await page.Tb().DateTimePicker("Starts").ChooseAsync(new DateTime(2026, 9, 14, 16, 45, 0));
        Assert.Equal(new DateTime(2026, 9, 14, 16, 45, 0), await page.Tb().DateTimePicker("Starts").ValueAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("2026-09-14 16:45");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldFillTheInput_WhenAStaticPickerIsFoundByALocator(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-date-time-picker");
        var starts = page.Tb().DateTimePicker(page.Locator("#starts"));

        await starts.ChooseAsync(new DateTime(2026, 9, 14, 16, 45, 0));

        Assert.Equal(new DateTime(2026, 9, 14, 16, 45, 0), await starts.ValueAsync());
    }
}
