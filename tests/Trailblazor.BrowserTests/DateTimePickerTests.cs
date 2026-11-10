using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DateTimePickerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Safari has a time box, but the WebKit build Playwright uses on Windows shows a text box whose edits never reach
    // the page.
    private const string NoTimeBox = "WebKit on Windows has no time box.";

    private static ILocator OpenButton(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Choose a date" });

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
        Assert.SkipWhen(browser == "webkit", NoTimeBox);
        var page = await OpenAsync(browser, "/date-time-picker");
        var time = page.Tb().DateTimePicker("Starts").TimeInput;

        await time.FillAsync("09:15");
        await time.PressAsync("Tab");

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-06-03 09:15");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldTypeTheDateAndFillTheTime_WhenUsedFromTheTestingHelpers(string browser)
    {
        Assert.SkipWhen(browser == "webkit", NoTimeBox);
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
}
