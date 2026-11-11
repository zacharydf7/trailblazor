using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class PopoverTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Trigger(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldTakeFocusAndGiveItBack_WhenOpenedAndClosedWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");
        var trigger = Trigger(page, "Filters");
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        await trigger.FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter the report" });
        await Expect(panel).ToBeVisibleAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(panel.GetByLabel("Only open items")).ToBeFocusedAsync();
        var (button, box) = ((await trigger.BoundingBoxAsync())!, (await panel.BoundingBoxAsync())!);
        Assert.True(box.Y >= button.Y + button.Height, "The panel should open below its trigger.");

        await page.Keyboard.PressAsync("Escape");

        await Expect(panel).ToBeHiddenAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(trigger).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldPlaceItselfAndTakeFocus_WhenClickedBeforeItsScriptHasLoaded(string browser)
    {
        var (page, script) = await OpenWithAScriptHeldAsync(browser, "/examples/popover", "**/popover.*js");
        var trigger = Trigger(page, "Filters");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter the report" });

        await trigger.ClickAsync();
        await Expect(panel).ToBeVisibleAsync();
        await Expect(trigger).Not.ToHaveAttributeAsync("data-ready", "");
        script.SetResult();

        await Expect(panel.GetByLabel("Only open items")).ToBeFocusedAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(panel).ToHaveAttributeAsync("style", new Regex("--tb-anchored-offset"));
    }

    // A toggle still queued when the script replays the opening must not open the panel a second time.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldLeaveFocusWhereItIs_WhenAnOpeningIsReportedTwice(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");
        var trigger = Trigger(page, "Filters");
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        await trigger.ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter the report" });
        await Expect(panel.GetByLabel("Only open items")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(panel.GetByLabel("Only open items")).Not.ToBeFocusedAsync();

        await panel.EvaluateAsync(
            "panel => panel.dispatchEvent(Object.assign(new Event('toggle'), { newState: 'open' }))");

        await Expect(panel.GetByLabel("Only open items")).Not.ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldCloseFromCodeAndOnAClickElsewhere_WhenItsOpenIsBound(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");

        var panel = await page.Tb().Popover("Filters").OpenAsync();
        await panel.GetByLabel("Only open items").CheckAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        await Expect(panel).ToBeHiddenAsync();
        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Filter state", Exact = true }))
            .ToHaveTextAsync("Open items");

        await page.Tb().Popover("Filters").OpenAsync();
        await page.Mouse.ClickAsync(5, 5);
        await Expect(panel).ToBeHiddenAsync();
        await Expect(Trigger(page, "Filters")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldOpenWithoutTakingFocusAndCloseOnEscape_WhenHovered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");
        var trigger = Trigger(page, "Keyboard shortcuts");
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard shortcuts" });

        await trigger.HoverAsync();
        await Expect(panel).ToBeVisibleAsync();
        await Expect(trigger).Not.ToBeFocusedAsync();
        var (button, box) = ((await trigger.BoundingBoxAsync())!, (await panel.BoundingBoxAsync())!);
        // It opens on top, or flips below when the page has no room above the trigger.
        Assert.True(box.Y + box.Height <= button.Y || box.Y >= button.Y + button.Height,
            "The panel should open beside its trigger, not over it.");
        await panel.HoverAsync();
        await page.WaitForTimeoutAsync(600);
        await Expect(panel).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(panel).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldStayOpenAndTakeFocus_WhenTheTriggerIsClickedAfterAHover(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");
        var trigger = Trigger(page, "Keyboard shortcuts");
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard shortcuts" });
        await trigger.HoverAsync();
        await Expect(panel).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await page.Mouse.MoveAsync(5, 5);
        await page.WaitForTimeoutAsync(600);

        await Expect(panel).ToBeVisibleAsync();
        await Expect(panel).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(trigger).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldStayOpen_WhenTheUserWorksInAHoverOpenedPanelAndThePointerLeaves(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");
        var trigger = Trigger(page, "Keyboard shortcuts");
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard shortcuts" });
        await trigger.HoverAsync();
        await Expect(panel).ToBeVisibleAsync();

        await panel.ClickAsync();
        await page.Mouse.MoveAsync(5, 5);
        await page.WaitForTimeoutAsync(600);

        await Expect(panel).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldNotCloseAnotherPopover_WhenAHoverTriggerIsRestedOn(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");
        var filters = await page.Tb().Popover("Filters").OpenAsync();

        await Trigger(page, "Keyboard shortcuts").HoverAsync();
        await page.WaitForTimeoutAsync(600);

        await Expect(filters).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard shortcuts" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldOpenOnHover_WhenItSitsInsideAnotherOpenPopover(string browser)
    {
        var page = await OpenAsync(browser, "/popover");
        var filters = await page.Tb().Popover("Filters").OpenAsync();
        var hint = Trigger(page, "What does status mean?");
        await Expect(hint).ToHaveAttributeAsync("data-ready", "");

        await hint.HoverAsync();

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "What does status mean?" })).ToBeVisibleAsync();
        await Expect(filters).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldClose_WhenTabMovesFocusOutOfThePanel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");
        var panel = await page.Tb().Popover("Filters").OpenAsync();
        await panel.GetByRole(AriaRole.Button, new() { Name = "Apply" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(panel).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldOpenFromAnIconTriggerAndFromCode_WhenAskedEitherWay(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");

        var notifications = await page.Tb().Popover("Notifications").OpenAsync();
        await Expect(notifications).ToHaveTextAsync("You're all caught up.");
        await page.Tb().Popover("Notifications").CloseAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Show filter options" }).ClickAsync();

        var filters = page.GetByRole(AriaRole.Dialog, new() { Name = "Filter the report" });
        await Expect(filters).ToBeVisibleAsync();
        await Expect(filters.GetByLabel("Only open items")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PopoverHelper_ShouldOpenAndCloseAPopover_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-popover");

        var panel = await page.Tb().Popover("Help").OpenAsync();
        await Expect(panel).ToHaveTextAsync("Ask the team in the help channel.");
        await page.Tb().Popover("Help").CloseAsync();

        await Expect(panel).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldOpenFromACustomTrigger_WhenItsAttributesAreOnAButton(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");

        var panel = await page.Tb().Popover("Ada Lovelace").OpenAsync();

        await Expect(panel).ToHaveAccessibleNameAsync("Ada Lovelace");
        await Expect(panel.GetByRole(AriaRole.Link, new() { Name = "Account settings" })).ToBeFocusedAsync();
        await page.Tb().Popover("Ada Lovelace").CloseAsync();
        await Expect(panel).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldOpenNeitherOnAClickNorOnHover_WhenDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/popover-disabled");
        var help = Trigger(page, "Help");
        var account = page.GetByRole(AriaRole.Button, new() { Name = "Account" });
        await Expect(help).ToHaveAttributeAsync("data-ready", "");

        await help.HoverAsync(new() { Force = true });
        await page.WaitForTimeoutAsync(600);
        await help.ClickAsync(new() { Force = true });
        await account.ClickAsync(new() { Force = true });

        await Expect(help).ToBeDisabledAsync();
        await Expect(account).ToBeDisabledAsync();
        await Expect(page.Locator("[data-part=panel]:popover-open")).ToHaveCountAsync(0);
    }

    // Closing the panel in the task that opened it merges the two toggle events into one that says it stayed closed.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldTellTheParentItClosed_WhenItClosesBeforeTheOpeningFromCodeIsReported(string browser)
    {
        var page = await OpenAsync(browser, "/popover");
        await Expect(Trigger(page, "Notes")).ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync("""
            () => {
                const show = HTMLElement.prototype.showPopover;
                HTMLElement.prototype.showPopover = function (...options) {
                    show.apply(this, options);
                    this.hidePopover();
                    window.openedFromCode = true;
                };
            }
            """);

        await page.GetByRole(AriaRole.Button, new() { Name = "Open the notes" }).ClickAsync();
        // The server renders the popover open before it asks the script to open it, so a "closed" after this can only
        // come from the report.
        await page.WaitForFunctionAsync("() => window.openedFromCode");

        await Expect(page.GetByTestId("notes-open")).ToHaveTextAsync("closed");
    }
}
