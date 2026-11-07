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

        await page.GetByRole(AriaRole.Switch, new() { Name = "Loading the profile" }).ClickAsync();

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

        await Expect(page.Locator(".tb-skeleton").First).ToHaveCSSAsync("outline-style", "solid");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Skeleton_ShouldHideAndCoverItsContentUntilLoaded_WhenWrappingIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/skeleton");
        var trip = page.GetByRole(AriaRole.Region, new() { Name = "Trip" });
        var content = trip.Locator("[data-part=content]");

        await Expect(trip).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(page.GetByText("Ridge loop")).ToBeHiddenAsync();
        var region = (await trip.BoundingBoxAsync())!;
        var box = (await content.BoundingBoxAsync())!;
        Assert.Equal(box.Height, region.Height, tolerance: 1);
        // Narrow content gets a skeleton its own size, not one across the row.
        var skeleton = page.Locator(".tb-skeleton").Filter(new() { HasText = "Book the trip" });
        var book = (await skeleton.BoundingBoxAsync())!;
        var button = (await skeleton.Locator("button").BoundingBoxAsync())!;
        Assert.Equal((button.Width, button.Height), (book.Width, book.Height));
        await page.GetByRole(AriaRole.Switch, new() { Name = "Loading the trip" }).ClickAsync();

        await Expect(trip).Not.ToHaveAttributeAsync("aria-busy", "true");
        await Expect(page.GetByText("Ridge loop")).ToBeVisibleAsync();
        var loaded = (await trip.BoundingBoxAsync())!;
        Assert.Equal((region.Width, region.Height), (loaded.Width, loaded.Height));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Skeleton_ShouldSweepAndStopForReducedMotion_WhenItWaves(string browser)
    {
        var page = await OpenAsync(browser, "/examples/skeleton");
        var still = await OpenAsync(browser, "/examples/skeleton", new() { ReducedMotion = ReducedMotion.Reduce });
        const string Wave = """
            skeleton => [getComputedStyle(skeleton).backgroundImage,
                ...skeleton.getAnimations().map(animation => animation.animationName)]
            """;

        var moving = await page.GetByRole(AriaRole.Region, new() { Name = "Trip" }).EvaluateAsync<string[]>(Wave);
        var stopped = await still.GetByRole(AriaRole.Region, new() { Name = "Trip" }).EvaluateAsync<string[]>(Wave);

        Assert.StartsWith("linear-gradient", moving[0]);
        Assert.Equal(["tb-skeleton-wave"], moving[1..]);
        Assert.Equal(["none"], stopped);
    }
}
