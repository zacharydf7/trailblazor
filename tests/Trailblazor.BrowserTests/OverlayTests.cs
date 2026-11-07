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
    public async Task Overlay_ShouldKeepFocusFromThePageAndGiveItBack_WhenAPageOverlayIsBlocking(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay");
        var show = page.GetByRole(AriaRole.Button, new() { Name = "Show page overlay" });

        // From the keyboard, since a click doesn't focus a button in Safari.
        await show.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Overlay(page)).ToHaveAttributeAsync("data-ready", "");
        await Expect(show).Not.ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body "
            + "|| !!document.activeElement.closest('[data-part=overlay]')"));
        Assert.False(await page.Locator("[data-part=announcer]")
            .EvaluateAsync<bool>("live => !!live.closest('[inert]')"));
        await page.Keyboard.PressAsync("Escape");
        await Expect(show).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Overlay_ShouldKeepThePageBlocked_WhenTheFirstOfTwoBlockingOverlaysClosesFirst(string browser)
    {
        var page = await OpenAsync(browser, "/overlays");
        var first = page.GetByRole(AriaRole.Button, new() { Name = "First", Exact = true });
        await first.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Second" }).ClickAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Close the first" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Second", IncludeHidden = true }))
            .ToHaveCountAsync(0);

        Assert.True(await first.EvaluateAsync<bool>("button => !!button.closest('[inert]')"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Overlay_ShouldMakeOnlyItsRegionInert_WhenAContainedOverlayIsBlocking(string browser)
    {
        var page = await OpenAsync(browser, "/examples/overlay");
        var report = page.GetByRole(AriaRole.Region, new() { Name = "Quarterly report" });
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Load the report" });

        await toggle.ClickAsync();

        await Expect(report.Locator("[inert]")).Not.ToHaveCountAsync(0);
        Assert.True(await report.GetByRole(AriaRole.Button, new() { Name = "Open the details", IncludeHidden = true })
            .EvaluateAsync<bool>("button => !!button.closest('[inert]')"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Finish loading" }).ClickAsync();
        await Expect(report.Locator("[inert]")).ToHaveCountAsync(0);
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
        var report = page.GetByRole(AriaRole.Region, new() { Name = "Quarterly report" });

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
