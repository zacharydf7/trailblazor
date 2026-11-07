using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class RadioStopsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator Radio(IPage page, string name) =>
        page.GetByRole(AriaRole.Radio, new() { Name = name, Exact = true });

    private async Task<IPage> OpenScopeAsync(string browser, string opener)
    {
        var page = await OpenAsync(browser, "/radio-stops");
        await Button(page, opener).ClickAsync();
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrapBothWays_WhenTheCheckedRadioIsNotTheLastInTheGroupThatEndsIt(string browser)
    {
        var page = await OpenScopeAsync(browser, "Open group last");
        await Expect(Button(page, "Before")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Radio(page, "Last two")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Button(page, "Before")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrapBothWays_WhenTheCheckedRadioIsNotTheFirstInTheGroupThatStartsIt(string browser)
    {
        var page = await OpenScopeAsync(browser, "Open group first");
        await Expect(Radio(page, "First two")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Button(page, "After")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Radio(page, "First two")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrap_WhenFocusIsAnywhereInAnUncheckedGroupThatEndsIt(string browser)
    {
        var page = await OpenScopeAsync(browser, "Open nothing checked");
        await Expect(Button(page, "Before")).ToBeFocusedAsync();
        await Radio(page, "None two").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Button(page, "Before")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrapBackward_WhenFocusIsAnywhereInAnUncheckedGroupThatStartsIt(string browser)
    {
        var page = await OpenScopeAsync(browser, "Open nothing checked first");
        await Expect(Radio(page, "Open one")).ToBeFocusedAsync();
        await Radio(page, "Open two").FocusAsync();

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(Button(page, "After")).ToBeFocusedAsync();
    }

    // WebKit's own Tab treats radios with no name as one group, so it never reaches the second.
    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task FocusScope_ShouldTabToTheNextRadio_WhenRadiosHaveNoName(string browser)
    {
        var page = await OpenScopeAsync(browser, "Open nameless");
        await Radio(page, "Nameless one").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Radio(page, "Nameless two")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldTabToTheNextGroup_WhenTwoFormsShareARadioName(string browser)
    {
        var page = await OpenScopeAsync(browser, "Open two forms");
        await Radio(page, "Form one").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Radio(page, "Form two")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrap_WhenAControlHiddenByVisibilityEndsIt(string browser)
    {
        var page = await OpenScopeAsync(browser, "Open unseen last");
        await Button(page, "Seen").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Button(page, "Before")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStartOnTheCheckedRadio_WhenARadioGroupComesFirst(string browser)
    {
        var page = await OpenAsync(browser, "/radio-stops");

        await Button(page, "Open radios").ClickAsync();

        await Expect(Radio(page, "Fast")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStartOnTheFirstControlTabReaches_WhenHiddenAndInertOnesComeFirst(string browser)
    {
        var page = await OpenAsync(browser, "/radio-stops");

        await Button(page, "Open hidden").ClickAsync();

        await Expect(Button(page, "Usable")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldTakeFocusOnTheCheckedRadio_WhenItOpens(string browser)
    {
        var page = await OpenAsync(browser, "/radio-stops");
        var trigger = Button(page, "Pick a pace");
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");

        await trigger.ClickAsync();

        await Expect(Radio(page, "Fast").Last).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ColorPicker_ShouldTakeFocusOnTheChosenSwatch_WhenItOpens(string browser)
    {
        var page = await OpenAsync(browser, "/radio-stops");
        var trigger = Button(page, "Choose a color");
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");

        await trigger.ClickAsync();

        await Expect(page.Locator("[data-part=swatch]:checked")).ToBeFocusedAsync();
    }
}
