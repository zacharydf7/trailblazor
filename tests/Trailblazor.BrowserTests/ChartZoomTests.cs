using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Zooming into part of a chart's scale and moving along it with keys, drags and the wheel.
public class ChartZoomTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override bool HoldsTheMouse => true;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldZoomMoveAndShowAll_WhenThePlotHasFocusAndThePageRunsEitherWay(string browser)
    {
        var page = await OpenAsync(browser, "/chart-zoom");
        var plot = page.Tb().Chart("Readings").Chart.Locator("[data-part=plot]");
        var view = page.GetByTestId("view");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");

        // A quarter of a view 10 wide moves a scale of whole numbers by 3.
        await plot.Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("+");
        await Expect(view).ToHaveTextAsync("5 to 15");
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(view).ToHaveTextAsync("8 to 18");
        await page.Keyboard.PressAsync("Shift+ArrowLeft");
        await Expect(view).ToHaveTextAsync("5 to 15");
        await page.Keyboard.PressAsync("-");
        await Expect(view).ToHaveTextAsync("all");
        await page.Keyboard.PressAsync("=");
        await Expect(view).ToHaveTextAsync("5 to 15");
        await page.Keyboard.PressAsync("0");
        await Expect(view).ToHaveTextAsync("all");
        await Expect(plot.Locator(":focus")).ToHaveCountAsync(1);

        // Right to left, the scale runs from the right, so Shift+Left moves forward along it.
        await page.EvaluateAsync("() => { document.documentElement.dir = 'rtl'; }");
        await plot.Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("+");
        await page.Keyboard.PressAsync("Shift+ArrowLeft");
        await Expect(view).ToHaveTextAsync("8 to 18");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drag_ShouldMoveTheViewWithTheMarksAndNotClickAPoint_WhenThePageRunsEitherWay(string browser)
    {
        var page = await OpenAsync(browser, "/chart-zoom");
        var chart = page.Tb().Chart("Readings").Chart;
        var plot = chart.Locator("[data-part=plot]");
        var marks = plot.Locator(".tb-chart__marks");
        var view = page.GetByTestId("view");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        await chart.GetByRole(AriaRole.Button, new() { Name = "Zoom in" }).ClickAsync();
        await Expect(view).ToHaveTextAsync("5 to 15");

        foreach (var (direction, expected) in new[] { ("ltr", "8 to 18"), ("rtl", "2 to 12") })
        {
            await page.EvaluateAsync("direction => { document.documentElement.dir = direction; }", direction);
            var box = (await plot.BoundingBoxAsync())!;
            var y = box.Y + box.Height / 2;

            // Dragging the marks three tenths of the plot to the left shows what was three to their right: further
            // along the scale left to right, and back along it right to left.
            await page.Mouse.MoveAsync(box.X + box.Width * 0.65f, y);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(box.X + box.Width * 0.35f, y, new() { Steps = 5 });
            Assert.NotEqual("none", await marks.EvaluateAsync<string>("marks => getComputedStyle(marks).translate"));
            await page.Mouse.UpAsync();

            await Expect(view).ToHaveTextAsync(expected);
            Assert.Equal("none", await marks.EvaluateAsync<string>("marks => getComputedStyle(marks).translate"));
            await Expect(page.GetByTestId("clicked")).ToHaveTextAsync("0");

            await chart.GetByRole(AriaRole.Button, new() { Name = "Show all" }).ClickAsync();
            await chart.GetByRole(AriaRole.Button, new() { Name = "Zoom in" }).ClickAsync();
            await Expect(view).ToHaveTextAsync("5 to 15");
        }

        // A click that isn't a drag still activates the point under it.
        var plotBox = (await plot.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(plotBox.X + plotBox.Width / 2, plotBox.Y + plotBox.Height / 2);
        await Expect(page.GetByTestId("clicked")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CtrlWheel_ShouldZoomAroundThePointerANotchAtATime_WhenTurnedEitherWay(string browser)
    {
        var page = await OpenAsync(browser, "/chart-zoom");
        var plot = page.Tb().Chart("Readings").Chart.Locator("[data-part=plot]");
        var view = page.GetByTestId("view");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");
        var box = (await plot.BoundingBoxAsync())!;

        // Small turns add up to one zoom: half of 0 to 20 around 4, a fifth of the way along, which keeps 4 a fifth
        // of the way along.
        await page.Mouse.MoveAsync(box.X + box.Width * 0.2f, box.Y + box.Height / 2);
        await page.Keyboard.DownAsync("Control");
        for (var turn = 0; turn < 3; turn++)
        {
            await page.Mouse.WheelAsync(0, -30);
        }

        await page.WaitForTimeoutAsync(200);
        await Expect(view).ToHaveTextAsync("all");
        await page.Mouse.WheelAsync(0, -30);
        await Expect(view).ToHaveTextAsync("2 to 12");

        await page.Mouse.WheelAsync(0, 100);
        await Expect(view).ToHaveTextAsync("all");
        await page.Keyboard.UpAsync("Control");

        // Without Ctrl, the wheel scrolls the page as ever.
        await page.Mouse.WheelAsync(0, -100);
        await page.WaitForTimeoutAsync(200);
        await Expect(view).ToHaveTextAsync("all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldReadAndChangeTheViewAndWaitForTheApp_WhenTheChartIsZoomable(string browser)
    {
        var page = await OpenAsync(browser, "/chart-zoom");
        var chart = page.Tb().Chart("Readings");
        var view = page.GetByTestId("view");

        Assert.Equal(("Showing 0 to 20", false), (await chart.ViewAsync(), await chart.IsZoomedAsync()));
        // Each helper returns once the app has the new view.
        await chart.ZoomInAsync();
        Assert.Equal("5 to 15", await view.TextContentAsync());
        Assert.Equal(("Showing 5 to 15", true), (await chart.ViewAsync(), await chart.IsZoomedAsync()));
        await chart.MoveForwardAsync();
        Assert.Equal("8 to 18", await view.TextContentAsync());
        await chart.MoveBackAsync();
        Assert.Equal("5 to 15", await view.TextContentAsync());
        await chart.ZoomOutAsync();
        Assert.Equal("all", await view.TextContentAsync());
        Assert.Equal("The chart can't show any more of its scale.",
            (await Assert.ThrowsAsync<InvalidOperationException>(chart.ShowAllAsync)).Message);
        await chart.ZoomInAsync();
        await chart.ShowAllAsync();
        Assert.Equal("all", await view.TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HorizontalChart_ShouldMoveDownItsScale_WhenShiftDownIsPressedOrItsMarksAreDraggedUp(
        string browser)
    {
        var page = await OpenAsync(browser, "/chart-zoom");
        var plot = page.Tb().Chart("Rungs").Chart.Locator("[data-part=plot]");
        var view = page.GetByTestId("turned");
        await Expect(plot).ToHaveAttributeAsync("data-ready", "");

        await plot.Locator("[tabindex='0']").FocusAsync();
        await page.Keyboard.PressAsync("+");
        await Expect(view).ToHaveTextAsync("5 to 15");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(view).ToHaveTextAsync("8 to 18");
        await page.Keyboard.PressAsync("Shift+ArrowUp");
        await Expect(view).ToHaveTextAsync("5 to 15");

        // The items run down the chart, so dragging its marks up shows those further down.
        var box = (await plot.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        await page.Mouse.MoveAsync(x, box.Y + box.Height * 0.65f);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, box.Y + box.Height * 0.35f, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await Expect(view).ToHaveTextAsync("8 to 18");
    }
}
