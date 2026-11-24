namespace Trailblazor.BrowserTests;

// A scope whose Trapped and Blocking change while it is there.
public class FocusScopeChangeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // Shows the scope from the keyboard, so Show is what had the focus as the scope started.
    private async Task<IPage> OpenScopeAsync(string browser)
    {
        var page = await OpenAsync(browser, "/focus-scope-change");
        await PressAsync(Button(page, "Show"));
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Editor" })).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // Presses a button with the keyboard, which leaves the focus on it in every browser.
    private static async Task PressAsync(ILocator button)
    {
        await button.FocusAsync();
        await button.Page.Keyboard.PressAsync("Enter");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldStartAndStopKeepingTabInside_WhenTrappedChangesAfterItStarted(string browser)
    {
        var page = await OpenScopeAsync(browser);
        var scope = page.GetByRole(AriaRole.Region, new() { Name = "Editor" });
        var (trap, last, after) = (Button(page, "Trap"), Button(page, "Edit"), Button(page, "After"));

        // Not trapped at first: Tab leaves the scope from its last control.
        await last.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(after).ToBeFocusedAsync();

        await trap.ClickAsync();
        await Expect(scope).ToHaveAttributeAsync("data-trapped", "");
        await last.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(trap).ToBeFocusedAsync();

        await trap.ClickAsync();
        await Expect(scope).Not.ToHaveAttributeAsync("data-trapped", "");
        await last.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(after).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldHoldWhatIsOutsideAndGiveItBack_WhenBlockingChangesAfterItStarted(string browser)
    {
        var page = await OpenScopeAsync(browser);
        var (block, outside) = (Button(page, "Block"), Button(page, "Outside"));

        await block.ClickAsync();

        await Expect(page.Locator("[data-tb-blocked]").First).ToBeAttachedAsync();
        Assert.True(await outside.EvaluateAsync<bool>("element => element.closest('[inert]') !== null"));
        Assert.False(await block.EvaluateAsync<bool>("element => element.closest('[inert]') !== null"));

        await block.ClickAsync();

        await Expect(page.Locator("[data-tb-blocked], [inert]")).ToHaveCountAsync(0);
        await outside.ClickAsync();
        await Expect(page.GetByTestId("outside")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldKeepTheFocusInside_WhenAHoldThatStartedInsideItEnds(string browser)
    {
        var page = await OpenScopeAsync(browser);
        var block = Button(page, "Block");

        await PressAsync(block);
        await Expect(page.Locator("[data-tb-blocked]").First).ToBeAttachedAsync();
        await PressAsync(block);
        await Expect(page.Locator("[data-tb-blocked]")).ToHaveCountAsync(0);
        // The scope hears that the hold ended a moment after the hold has gone. A click from script leaves the focus
        // alone, and the mark it brings shows the scope has heard everything that was sent before it.
        await Button(page, "Trap").DispatchEventAsync("click");
        var scope = page.GetByRole(AriaRole.Region, new() { Name = "Editor" });
        await Expect(scope).ToHaveAttributeAsync("data-trapped", "");

        // The focus stays on the control that ended the hold, and doesn't go to Show, which had it as the scope
        // started.
        await Expect(block).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scope_ShouldFocusItsFirstControl_WhenTheControlThatStartedTheHoldInsideItIsGone(string browser)
    {
        var page = await OpenScopeAsync(browser);

        // Edit gives way to Done as the hold starts, and Done goes as it ends, with the focus on it.
        await PressAsync(Button(page, "Edit"));
        await PressAsync(Button(page, "Done"));

        await Expect(page.Locator("[data-tb-blocked]")).ToHaveCountAsync(0);
        await Expect(Button(page, "Trap")).ToBeFocusedAsync();
    }
}
