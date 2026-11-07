using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class NarrowPlacementTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const int WindowWidth = 320;

    private async Task<IPage> OpenNarrowAsync(string browser)
    {
        var page = await OpenAsync(browser, "/narrow-placement");
        await page.SetViewportSizeAsync(WindowWidth, 600);
        return page;
    }

    // Placing a popup marks its side, which may come a frame after it shows.
    private static async Task<LocatorBoundingBoxResult> PlacedBoxAsync(ILocator popup)
    {
        await Expect(popup).ToHaveAttributeAsync("data-side", new Regex("."));
        return (await popup.BoundingBoxAsync())!;
    }

    private static async Task ExpectInsideTheWindowAsync(ILocator popup)
    {
        var box = await PlacedBoxAsync(popup);
        Assert.True(box.X >= 0 && box.X + box.Width <= WindowWidth, $"The popup spans {box.X} to {box.X + box.Width}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldStayInsideTheWindow_WhenNeitherSideHasRoom(string browser)
    {
        var page = await OpenNarrowAsync(browser);

        var panel = await page.Tb().Popover("Share").OpenAsync();

        await ExpectInsideTheWindowAsync(panel);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayInsideTheWindow_WhenNeitherSideHasRoom(string browser)
    {
        var page = await OpenNarrowAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).HoverAsync();

        var tooltip = page.GetByRole(AriaRole.Tooltip);
        await Expect(tooltip).ToBeVisibleAsync();
        await ExpectInsideTheWindowAsync(tooltip);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldWrapBesideItsTriggerAndHoldStill_WhenItsTextCanWrap(string browser)
    {
        var page = await OpenNarrowAsync(browser);
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Note", Exact = true });
        await trigger.HoverAsync();
        var tooltip = page.GetByRole(AriaRole.Tooltip);
        await Expect(tooltip).ToBeVisibleAsync();

        var first = await PlacedBoxAsync(tooltip);
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        var later = await PlacedBoxAsync(tooltip);

        var button = (await trigger.BoundingBoxAsync())!;
        Assert.True(later.X >= button.X + button.Width, $"The tooltip starts at {later.X}, over the trigger.");
        Assert.True(later.X + later.Width <= WindowWidth, $"The tooltip ends at {later.X + later.Width}.");
        Assert.Equal(first.X, later.X);
        Assert.Equal(first.Width, later.Width);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldGoBackToItsNaturalWidth_WhenTheWindowWidens(string browser)
    {
        var page = await OpenNarrowAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Note", Exact = true }).HoverAsync();
        var tooltip = page.GetByRole(AriaRole.Tooltip);
        await Expect(tooltip).ToBeVisibleAsync();
        var wrapped = await PlacedBoxAsync(tooltip);

        await page.SetViewportSizeAsync(1000, 600);

        await page.WaitForFunctionAsync("([tooltip, width]) => tooltip.getBoundingClientRect().width > width",
            new object[] { await tooltip.ElementHandleAsync(), (double)wrapped.Width });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldWrapBeforeItsTrigger_WhenPlacedAtTheStart(string browser)
    {
        var page = await OpenNarrowAsync(browser);
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Note before" });
        await trigger.HoverAsync();
        var tooltip = page.GetByRole(AriaRole.Tooltip);
        await Expect(tooltip).ToBeVisibleAsync();

        var box = await PlacedBoxAsync(tooltip);

        var button = (await trigger.BoundingBoxAsync())!;
        Assert.True(box.X >= 0 && box.X + box.Width <= button.X,
            $"The tooltip spans {box.X} to {box.X + box.Width}, and the trigger starts at {button.X}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldKeepItsScrollPlace_WhenPlacedAgainWhileWrapped(string browser)
    {
        var page = await OpenNarrowAsync(browser);
        await page.SetViewportSizeAsync(WindowWidth, 300);
        var panel = await page.Tb().Popover("Terms").OpenAsync();
        await PlacedBoxAsync(panel);
        await panel.EvaluateAsync("panel => panel.scrollTop = 100");
        Assert.Equal(100, await panel.EvaluateAsync<double>("panel => panel.scrollTop"));

        await page.EvaluateAsync("() => window.dispatchEvent(new Event('resize'))");

        Assert.Equal(100, await panel.EvaluateAsync<double>("panel => panel.scrollTop"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Trigger_ShouldTakeTheClick_WhenItsTooltipCoversIt(string browser)
    {
        var page = await OpenNarrowAsync(browser);
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Save" });
        await trigger.HoverAsync();
        var tooltip = page.GetByRole(AriaRole.Tooltip);
        await Expect(tooltip).ToBeVisibleAsync();
        var box = await PlacedBoxAsync(tooltip);
        var button = (await trigger.BoundingBoxAsync())!;
        Assert.True(box.X < button.X + button.Width / 2, "The tooltip should cover the trigger's middle.");

        await page.Mouse.ClickAsync(button.X + button.Width / 2, button.Y + button.Height / 2);

        await Expect(page.GetByTestId("saves")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldStayInsideTheWindowAndUsable_WhenNeitherSideHasRoom(string browser)
    {
        var page = await OpenNarrowAsync(browser);
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await trigger.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Move" }).ClickAsync();

        var item = page.GetByRole(AriaRole.Menuitem, new() { Name = "To the shared folder" });
        await Expect(item).ToBeVisibleAsync();
        await ExpectInsideTheWindowAsync(page.GetByRole(AriaRole.Menu).Filter(new() { Has = item }).Last);
        await item.ClickAsync();

        await Expect(page.GetByTestId("moved")).ToHaveTextAsync("True");
    }
}
