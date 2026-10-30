namespace Trailblazor.BrowserTests;

public class SkeletonTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Skeleton_ShouldPulseUntilTheContentArrives_WhenMotionIsAllowed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/skeleton", new() { ReducedMotion = ReducedMotion.NoPreference });
        var region = page.GetByRole(AriaRole.Region, new() { Name = "Profile" });
        await Expect(region).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(region.Locator(".tb-skeleton")).ToHaveCountAsync(4);
        await Expect(region.Locator(".tb-skeleton").First).ToHaveCSSAsync("animation-name", "tb-skeleton-pulse");

        await page.GetByRole(AriaRole.Switch, new() { Name = "Loading" }).ClickAsync();

        await Expect(region).ToHaveAttributeAsync("aria-busy", "false");
        await Expect(region.Locator(".tb-skeleton")).ToHaveCountAsync(0);
        await Expect(region.GetByText("Ada Lovelace")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Skeleton_ShouldStayStill_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await OpenAsync(browser, "/examples/skeleton", new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(page.Locator(".tb-skeleton").First).ToHaveCSSAsync("animation-name", "none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Skeleton_ShouldKeepItsShapes_WhenSizedAsACircleARectangleAndLines(string browser)
    {
        var page = await OpenAsync(browser, "/examples/skeleton");

        var circle = (await page.Locator(".tb-skeleton--circle").BoundingBoxAsync())!;
        var rectangle = (await page.Locator(".tb-skeleton--rectangle").BoundingBoxAsync())!;
        var line = (await page.Locator(".tb-skeleton--text").First.BoundingBoxAsync())!;

        Assert.Equal(48, circle.Width, tolerance: 0.5);
        Assert.Equal(circle.Width, circle.Height, tolerance: 0.5);
        Assert.Equal((100, 64), (rectangle.Width, rectangle.Height));
        Assert.Equal(16, line.Height, tolerance: 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Skeleton_ShouldKeepAnOutline_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/skeleton", new() { ForcedColors = ForcedColors.Active });

        await Expect(page.Locator(".tb-skeleton").First).ToHaveCSSAsync("border-top-style", "solid");
    }
}
