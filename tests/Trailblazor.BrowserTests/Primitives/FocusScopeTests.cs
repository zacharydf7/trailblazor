namespace Trailblazor.BrowserTests.Primitives;

public class FocusScopeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CreateAsync_ShouldFocusFirstTabbable_WhenAutoFocusIsOn(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope");

        await ActivateOuterAsync(page);

        await Expect(page.GetByTestId("outer-first")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CreateAsync_ShouldLeaveFocusAlone_WhenAutoFocusIsOff(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?noAutoFocus=true");

        await ActivateOuterAsync(page);

        await Expect(page.GetByTestId("opener")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldWrapToFirstTabbable_WhenTrappedAndFocusIsOnLast(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?trapped=true");
        await ActivateOuterAsync(page);
        await page.GetByTestId("outer-last").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("outer-first")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ShiftTab_ShouldWrapToLastTabbable_WhenTrappedAndFocusIsOnFirst(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?trapped=true");
        await ActivateOuterAsync(page);
        await Expect(page.GetByTestId("outer-first")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(page.GetByTestId("outer-last")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldLeaveScope_WhenNotTrapped(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope");
        await ActivateOuterAsync(page);
        await page.GetByTestId("outer-last").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("outside")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldReturnToLastFocusedInside_WhenTrappedAndFocusMovesOutside(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?trapped=true");
        await ActivateOuterAsync(page);
        await page.GetByTestId("outer-last").FocusAsync();

        await page.GetByTestId("outside").FocusAsync();

        await Expect(page.GetByTestId("outer-last")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldStayInsideScope_WhenTrappedAndFocusedElementIsRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?trapped=true&removable=true");
        await ActivateOuterAsync(page);
        await page.GetByTestId("removable").PressAsync("Delete");
        await Expect(page.GetByTestId("removable")).ToHaveCountAsync(0);

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("outer-first")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldReturnToFirstTabbable_WhenTrappedAndLastFocusedElementWasRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?trapped=true&removable=true");
        await ActivateOuterAsync(page);
        await page.GetByTestId("removable").PressAsync("Delete");
        await Expect(page.GetByTestId("removable")).ToHaveCountAsync(0);

        await page.GetByTestId("outside").FocusAsync();

        await Expect(page.GetByTestId("outer-first")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldWrapInsideInnerScope_WhenTrappedScopesAreNested(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?trapped=true");
        await ActivateOuterAsync(page);
        await ActivateInnerAsync(page);
        await page.GetByTestId("inner-last").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("inner-first")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldWrapInsideOuterScope_WhenNestedInnerScopeIsDisposed(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?trapped=true");
        await ActivateOuterAsync(page);
        await ActivateInnerAsync(page);
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("active-scopes")).ToHaveTextAsync("1");
        await page.GetByTestId("outer-last").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("outer-first")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DisposeAsync_ShouldRestoreFocusToOpener_WhenRestoreFocusIsOn(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope");
        await ActivateOuterAsync(page);

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("active-scopes")).ToHaveTextAsync("0");
        await Expect(page.GetByTestId("opener")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DisposeAsync_ShouldLeaveFocusAlone_WhenRestoreFocusIsOff(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope?noRestoreFocus=true");
        await ActivateOuterAsync(page);

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("active-scopes")).ToHaveTextAsync("0");
        await Expect(page.GetByTestId("outer-first")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DisposeAsync_ShouldLeaveFocusAlone_WhenFocusMovedOutsideScope(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope");
        await ActivateOuterAsync(page);
        await page.GetByTestId("outside").FocusAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("active-scopes")).ToHaveTextAsync("0");
        await Expect(page.GetByTestId("outside")).ToBeFocusedAsync();
    }

    private static async Task ActivateOuterAsync(IPage page)
    {
        await page.GetByTestId("opener").PressAsync("Enter");
        await Expect(page.GetByTestId("active-scopes")).ToHaveTextAsync("1");
    }

    private static async Task ActivateInnerAsync(IPage page)
    {
        await page.GetByTestId("inner-opener").PressAsync("Enter");
        await Expect(page.GetByTestId("active-scopes")).ToHaveTextAsync("2");
        await Expect(page.GetByTestId("inner-first")).ToBeFocusedAsync();
    }
}
