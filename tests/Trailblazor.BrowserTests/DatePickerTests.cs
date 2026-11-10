using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DatePickerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator OpenButton(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Choose a date" });

    private static ILocator Day(IPage page, string name) =>
        page.GetByRole(AriaRole.Grid).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldOpenOnTheChosenDayAndPickWithTheKeys_WhenTheCalendarButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/date-picker");
        await Expect(OpenButton(page)).ToHaveAttributeAsync("data-ready", "");
        await OpenButton(page).FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(Day(page, "Wednesday, June 3, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-06-10");
        await Expect(page.GetByLabel("Start date")).ToHaveValueAsync("6/10/2026");
        await Expect(OpenButton(page)).ToBeFocusedAsync();
        await Expect(OpenButton(page)).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldCloseKeepingTheDateAndReopenOnIt_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/date-picker");
        await Expect(OpenButton(page)).ToHaveAttributeAsync("data-ready", "");
        await OpenButton(page).ClickAsync();
        await Expect(OpenButton(page)).ToHaveAttributeAsync("aria-expanded", "true");
        await Day(page, "Wednesday, June 3, 2026").FocusAsync();

        await page.Keyboard.PressAsync("PageDown");
        await page.Keyboard.PressAsync("Escape");

        await Expect(OpenButton(page)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-06-03");
        await Expect(OpenButton(page)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Day(page, "Wednesday, June 3, 2026")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(OpenButton(page)).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldTypeTheDateInTheFieldsFormat_WhenUsedFromTheTestingHelpers(string browser)
    {
        var page = await OpenAsync(browser, "/date-picker");

        await page.Tb().DatePicker("Start date").ChooseAsync(new DateOnly(2026, 9, 14));

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-09-14");
        Assert.Equal(new DateOnly(2026, 9, 14), await page.Tb().DatePicker("Start date").ValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ValueAsync_ShouldWaitForTheInteractiveField_WhenThePageIsStillPrerendered(string browser)
    {
        var page = await OpenStaticAsync(browser, "/date-picker");

        Assert.Equal(new DateOnly(2026, 6, 3), await page.Tb().DatePicker("Start date").ValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ValueAsync_ShouldReturnNull_WhenTheFieldIsCleared(string browser)
    {
        var page = await OpenAsync(browser, "/date-picker");

        await page.Tb().DatePicker("Start date").ChooseAsync(null);

        Assert.Null(await page.Tb().DatePicker("Start date").ValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldPostTheDate_WhenThePickerIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-date-picker");

        await page.Tb().DatePicker("Start date").ChooseAsync(new DateOnly(2026, 9, 14));
        Assert.Equal(new DateOnly(2026, 9, 14), await page.Tb().DatePicker("Start date").ValueAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("2026-09-14");
    }
}
