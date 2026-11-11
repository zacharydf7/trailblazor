namespace Trailblazor.BrowserTests;

// Options a component's script reads as it attaches take effect when they change later, by attaching it again.
public class LiveOptionsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldFilterAndChooseByKey_WhenFilterableIsTurnedOnLater(string browser)
    {
        var page = await OpenAsync(browser, "/live-options");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" });
        await Expect(trail).ToHaveAttributeAsync("data-ready", "");

        var attached = Reattached(trail);
        await page.GetByRole(AriaRole.Switch, new() { Name = "Filter the trails" }).ClickAsync();
        await attached;
        await trail.ClickAsync();
        await page.Keyboard.TypeAsync("shore");
        await Expect(page.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Lake shore"]);
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Trail chosen")).ToHaveTextAsync("Lake shore");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldOpenOnItsNewSide_WhenPlacementChangesLater(string browser)
    {
        var page = await OpenAsync(browser, "/live-options");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Details" });
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");

        var attached = Reattached(trigger);
        await page.GetByRole(AriaRole.Switch, new() { Name = "Open above" }).ClickAsync();
        await attached;
        await trigger.ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Details" });
        await Expect(panel).ToBeVisibleAsync();

        var box = (await panel.BoundingBoxAsync())!;
        var button = (await trigger.BoundingBoxAsync())!;
        Assert.True(box.Y + box.Height <= button.Y + 1, "The panel isn't above its trigger.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldShowItsMovedButtonsAndThenMoveThemAgain_WhenItTurnsVerticalAndBack(string browser)
    {
        var page = await OpenAsync(browser, "/live-options");
        var toolbar = page.GetByRole(AriaRole.Toolbar, new() { Name = "Notes" });
        await Expect(toolbar).ToHaveAttributeAsync("data-ready", "");
        var last = toolbar.GetByRole(AriaRole.Button, new() { Name = "Strikethrough" });
        await Expect(last).ToBeHiddenAsync();

        var attached = Reattached(toolbar);
        await page.GetByRole(AriaRole.Switch, new() { Name = "Stand the toolbar up" }).ClickAsync();
        await attached;
        await Expect(last).ToBeVisibleAsync();
        attached = Reattached(toolbar);
        await page.GetByRole(AriaRole.Switch, new() { Name = "Stand the toolbar up" }).ClickAsync();
        await attached;

        await Expect(last).ToBeHiddenAsync();
    }

    // Resolves once the component's script lets go of the element and a new one marks it ready again.
    private static Task Reattached(ILocator element) => element.EvaluateAsync("""
        element => new Promise(resolve => {
            let dropped = false;
            new MutationObserver((_, observer) => {
                if (!element.hasAttribute('data-ready')) {
                    dropped = true;
                } else if (dropped) {
                    observer.disconnect();
                    resolve();
                }
            }).observe(element, { attributes: true, attributeFilter: ['data-ready'] });
        })
        """);
}
