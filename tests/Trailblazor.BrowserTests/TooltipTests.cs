namespace Trailblazor.BrowserTests;

public class TooltipTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldAppearAboveTheTrigger_WhenThePointerRestsOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });

        await add.HoverAsync();

        await Expect(tooltip).ToBeVisibleAsync();
        var trigger = await add.BoundingBoxAsync();
        var box = await tooltip.BoundingBoxAsync();
        Assert.True(box!.Y + box.Height <= trigger!.Y + 1, "The tooltip should sit above its trigger.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldDescribeTheTriggerAndCloseOnEscape_WhenTheTriggerHasFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });

        await add.FocusAsync();
        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(add).ToHaveAccessibleDescriptionAsync("Add an item");
        await page.Keyboard.PressAsync("Escape");

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayOpen_WhenThePointerMovesOntoIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var below = page.GetByRole(AriaRole.Button, new() { Name = "Below" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Shown below the button" });
        await below.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        // Move slowly across the gap between them, which is where a tooltip without a hide delay would close.
        var box = await tooltip.BoundingBoxAsync();
        await page.Mouse.MoveAsync(box!.X + box.Width / 2, box.Y + box.Height / 2, new() { Steps = 10 });
        await page.WaitForTimeoutAsync(500);

        await Expect(tooltip).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldWaitBeforeAppearing_WhenThePointerArrives(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        // Times the gap in the page itself, since Playwright's hover can take longer than the delay.
        await add.EvaluateAsync("""
            button => {
                const anchor = button.parentElement;
                anchor.addEventListener('pointerenter', () => window.entered = performance.now());
                anchor.querySelector('[role=tooltip]').addEventListener('toggle', () => window.opened = performance.now());
            }
            """);

        await add.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        Assert.True(await page.EvaluateAsync<double>("() => window.opened - window.entered") >= 250);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldClose_WhenThePointerLeaves(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        await page.Mouse.MoveAsync(0, 0);

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldClose_WhenFocusMovesAway(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).FocusAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStillDescribeTheTrigger_WhenTheTriggerRewritesItsDescription(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await save.ClickAsync();

        await Expect(save).ToHaveAccessibleDescriptionAsync("Saved. Saves a copy");
    }
}
