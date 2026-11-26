using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TimePickerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldSetTheTime_WhenTheBoxIsFilled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker");
        var start = Box(page, "Start time");
        await Expect(start).ToHaveAttributeAsync("data-ready", "");

        await start.FillAsync("2:45 PM");
        await start.PressAsync("Tab");

        await Expect(page.GetByText($"Starting at {new TimeOnly(14, 45):t}.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseReadAndClearTheTime_WhenTheFieldIsInteractive(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker");
        var start = page.Tb().TimePicker("Start time");

        await start.ChooseAsync(new TimeOnly(14, 45));
        await Expect(page.GetByText($"Starting at {new TimeOnly(14, 45):t}.")).ToBeVisibleAsync();
        var chosen = await start.ValueAsync();
        await start.ChooseAsync(null);

        Assert.Equal(new TimeOnly(14, 45), chosen);
        Assert.Null(await start.ValueAsync());
        await Expect(page.GetByText("No time chosen yet.")).ToBeVisibleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().TimePicker("Gates open").ChooseAsync(new TimeOnly(7, 0)));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldDropTheSeconds_WhenTheBoxAlreadyShowsTheTimeWithoutThem(string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");

        // The box shows 09:30 for 09:30:15, the same text the helper types for 09:30.
        await page.Tb().TimePicker("Slow end").ChooseAsync(new TimeOnly(9, 30));

        Assert.Equal("end 09:30:00", await page.GetByTestId("slow").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldReturnAtOnce_WhenTheBoxAlreadyShowsTheTimeWithItsSeconds(string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");
        var before = await page.GetByTestId("slow").TextContentAsync();

        // With nothing to change, there's no answer to wait for, and a wait for one would time out.
        await page.Tb().DateTimePicker("Slow lap").ChooseAsync(new DateTime(2026, 5, 2, 9, 30, 15));
        await page.Tb().TimePicker("Slow split").ChooseAsync(new TimeOnly(9, 30, 15));

        Assert.Equal(before, await page.GetByTestId("slow").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldShowAMessage_WhenTheTimeIsOutsideTheLimits(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker");
        var pickup = Box(page, "Pickup time");
        await Expect(pickup).ToHaveAttributeAsync("data-ready", "");

        await pickup.FillAsync("18:00");
        await pickup.PressAsync("Tab");

        // The app runs in this process, so it shows times in this process's culture.
        await Expect(page.Locator("[data-part=error]"))
            .ToHaveTextAsync($"Choose a time from {new TimeOnly(9, 0):t} to {new TimeOnly(17, 0):t}.");
        await Expect(pickup).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldPickATimeWithTheKeys_WhenTheListIsOpenedFromTheBox(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker");
        var start = Box(page, "Start time");
        await Expect(start).ToHaveAttributeAsync("data-ready", "");
        await start.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(start).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(start).ToHaveAttributeAsync("aria-activedescendant",
            await (await ListOf(page, start)).Locator("[data-time='08:30']").GetAttributeAsync("id") ?? "");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(start).ToHaveAttributeAsync("aria-activedescendant",
            await (await ListOf(page, start)).Locator("[data-time='09:00']").GetAttributeAsync("id") ?? "");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByText($"Starting at {new TimeOnly(9, 0):t}.")).ToBeVisibleAsync();
        await Expect(start).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(start).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldShowThePickedTime_WhenTheChosenTimeIsPickedAfterTyping(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker");
        var start = Box(page, "Start time");
        await Expect(start).ToHaveAttributeAsync("data-ready", "");
        await start.FocusAsync();
        await page.Keyboard.TypeAsync("10");
        await Expect(start).ToHaveAttributeAsync("aria-expanded", "true");

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Tab");

        await Expect(start).ToHaveValueAsync($"{new TimeOnly(8, 30):t}");
        await Expect(page.GetByText($"Starting at {new TimeOnly(8, 30):t}.")).ToBeVisibleAsync();
        await Expect(start).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldCloseTheListAndKeepTheTime_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker");
        var start = Box(page, "Start time");
        await Expect(start).ToHaveAttributeAsync("data-ready", "");
        await start.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");

        await page.Keyboard.PressAsync("Escape");

        await Expect(start).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(await ListOf(page, start)).ToBeHiddenAsync();
        await Expect(page.GetByText($"Starting at {new TimeOnly(8, 30):t}.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldOpenTheListAndPickTheClickedTime_WhenTheClockButtonIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/time-picker");
        var start = Box(page, "Start time");
        await Expect(start).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Choose a time" }).First.ClickAsync();
        await Expect(start).ToBeFocusedAsync();
        await (await ListOf(page, start)).Locator("[data-time='10:00']").ClickAsync();

        await Expect(page.GetByText($"Starting at {new TimeOnly(10, 0):t}.")).ToBeVisibleAsync();
        await Expect(start).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Picker_ShouldPostTheTime_WhenTheFormIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-time-picker");

        await page.Tb().TimePicker("Start time").ChooseAsync(new TimeOnly(16, 20));
        Assert.Equal(new TimeOnly(16, 20), await page.Tb().TimePicker("Start time").ValueAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("16:20");
    }

    private static ILocator Box(IPage page, string label) =>
        page.GetByRole(AriaRole.Combobox, new() { Name = label });

    private static async Task<ILocator> ListOf(IPage page, ILocator box) =>
        page.Locator($"[id='{await box.GetAttributeAsync("aria-controls")}']");
}
