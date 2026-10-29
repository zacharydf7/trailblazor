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

        // Pause in the gap between them for longer than the hide delay, then go on onto the tooltip.
        var trigger = await below.BoundingBoxAsync();
        var box = await tooltip.BoundingBoxAsync();
        var middle = box!.X + box.Width / 2;
        await page.Mouse.MoveAsync(middle, (trigger!.Y + trigger.Height + box.Y) / 2, new() { Steps = 5 });
        await page.WaitForTimeoutAsync(500);
        await page.Mouse.MoveAsync(middle, box.Y + box.Height / 2, new() { Steps = 5 });
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
                // Fires inside showPopover, while WebKit can deliver toggle after the tooltip is already visible.
                anchor.querySelector('[role=tooltip]')
                    .addEventListener('beforetoggle', () => window.opened = performance.now());
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
