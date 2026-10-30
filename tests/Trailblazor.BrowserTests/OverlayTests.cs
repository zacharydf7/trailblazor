namespace Trailblazor.BrowserTests;

public class OverlayTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Overlay(IPage page) => page.Locator("[data-part=overlay]");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Overlay_ShouldCloseOnAClickOnTheScrimOrEscape_WhenCloseOnClickIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay");
        var show = page.GetByRole(AriaRole.Button, new() { Name = "Show page overlay" });

        await show.ClickAsync();
        await Expect(Overlay(page)).ToHaveAttributeAsync("data-ready", "");
        await page.GetByText("Click outside this box").ClickAsync();
        await Expect(Overlay(page)).ToBeVisibleAsync();
        await page.Mouse.ClickAsync(5, 5);
        await Expect(Overlay(page)).ToHaveCountAsync(0);

        await show.ClickAsync();
        await Expect(Overlay(page)).ToHaveAttributeAsync("data-ready", "");
        await page.Keyboard.PressAsync("Escape");
        await Expect(Overlay(page)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Overlay_ShouldStopThePageScrolling_WhileAPageOverlayLocksScroll(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay");
        var html = page.Locator("html");
        await Expect(html).ToHaveCSSAsync("overflow-y", "visible");

        await page.GetByRole(AriaRole.Button, new() { Name = "Show page overlay" }).ClickAsync();

        await Expect(html).ToHaveCSSAsync("overflow-y", "hidden");
        await Expect(Overlay(page)).ToHaveAttributeAsync("data-ready", "");
        await page.Keyboard.PressAsync("Escape");
        await Expect(html).ToHaveCSSAsync("overflow-y", "visible");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Overlay_ShouldCoverOnlyItsRegionAndCatchClicksThere_WhenContained(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay");
        var report = page.GetByTestId("report");

        await page.GetByRole(AriaRole.Button, new() { Name = "Load the report" }).ClickAsync();

        var cover = (await Overlay(page).BoundingBoxAsync())!;
        var region = (await report.BoundingBoxAsync())!;
        // It covers the region inside its 1px border.
        Assert.Equal(region.X + 1, cover.X, tolerance: 0.5);
        Assert.Equal(region.Y + 1, cover.Y, tolerance: 0.5);
        Assert.Equal(region.Width - 2, cover.Width, tolerance: 0.5);
        Assert.Equal(region.Height - 2, cover.Height, tolerance: 0.5);
        var hit = await page.GetByRole(AriaRole.Button, new() { Name = "Open the details", IncludeHidden = true })
            .EvaluateAsync<string?>("""
                button => {
                    const box = button.getBoundingClientRect();
                    return document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2)?.dataset.part;
                }
                """);
        Assert.Equal("scrim", hit);
        await Expect(page.GetByRole(AriaRole.Progressbar, new() { Name = "Loading the report" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Overlay_ShouldAppearWithoutFading_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay", new() { ReducedMotion = ReducedMotion.Reduce });

        await page.GetByRole(AriaRole.Button, new() { Name = "Show page overlay" }).ClickAsync();

        await Expect(Overlay(page)).ToHaveCSSAsync("animation-duration", "0s");
    }
}
