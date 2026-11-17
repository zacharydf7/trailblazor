using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TourTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Reduced motion places each step at once, so it is measured where it rests.
    private static readonly BrowserNewContextOptions Still = new() { ReducedMotion = ReducedMotion.Reduce };

    private static ILocator Step(IPage page) => page.Locator(".tb-tour [data-part=step]");

    private async Task<IPage> StartAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tour", Still);
        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await Expect(page.Locator(".tb-tour")).ToHaveAttributeAsync("data-ready", "");
        await Expect(Step(page)).ToBeFocusedAsync();
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tour_ShouldPlaceTheStepByItsTargetAndRingIt_WhenShown(string browser)
    {
        var page = await StartAsync(browser);

        var target = (await page.Locator("#tour-new-trail").BoundingBoxAsync())!;
        var step = (await Step(page).BoundingBoxAsync())!;
        var shade = (await page.Locator("[data-part=shade]").BoundingBoxAsync())!;
        Assert.True(step.Y >= target.Y + target.Height, $"The step starts at {step.Y}, above the target's bottom.");
        Assert.InRange(step.X - target.X, -1, 1);
        Assert.True(shade.X < target.X && shade.Y < target.Y && shade.X + shade.Width > target.X + target.Width
            && shade.Y + shade.Height > target.Y + target.Height, "The shade's hole doesn't ring the target.");
        // A click on the page lands on the tour, which covers it.
        Assert.True(await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).EvaluateAsync<bool>("""
            button => {
                const box = button.getBoundingClientRect();
                return document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2).closest('.tb-tour')
                    !== null;
            }
            """));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tour_ShouldKeepFocusInTheStep_WhenTabbing(string browser)
    {
        var page = await StartAsync(browser);

        for (var i = 0; i < 4; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            Assert.True(await Step(page).EvaluateAsync<bool>("step => step.contains(document.activeElement)"),
                $"Tab {i + 1} took focus out of the step.");
        }

        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.True(await Step(page).EvaluateAsync<bool>("step => step.contains(document.activeElement)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tour_ShouldGiveFocusBack_WhenEscapeSkipsIt(string browser)
    {
        var page = await StartAsync(browser);

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.Locator(".tb-tour")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" })).ToBeFocusedAsync();
        await Expect(page.GetByText("Skipped")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tour_ShouldCenterTheStep_WhenItsTargetIsMissingOrNotValid(string browser)
    {
        var page = await OpenAsync(browser, "/tour-missing-target", Still);
        await page.GetByRole(AriaRole.Button, new() { Name = "Start" }).ClickAsync();

        await ExpectCenteredAsync(page, "Not a selector");
        await page.Tb().Tour().NextAsync();
        await ExpectCenteredAsync(page, "Nothing to point at");
    }

    private static async Task ExpectCenteredAsync(IPage page, string title)
    {
        await Expect(page.Locator(".tb-tour [data-part=title]")).ToHaveTextAsync(title);
        await Expect(page.Locator(".tb-tour")).ToHaveAttributeAsync("data-centered", "");
        var step = (await Step(page).BoundingBoxAsync())!;
        var size = page.ViewportSize!;
        Assert.InRange(step.X + step.Width / 2 - size.Width / 2d, -2, 2);
        Assert.InRange(step.Y + step.Height / 2 - size.Height / 2d, -2, 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tour_ShouldMakeThePageBehindItInert_WhenShown(string browser)
    {
        var page = await StartAsync(browser);
        var start = page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" });
        const string Inert = "button => button.closest('[inert]') !== null";

        Assert.True(await start.EvaluateAsync<bool>(Inert));
        await page.GetByRole(AriaRole.Button, new() { Name = "Skip tour" }).ClickAsync();
        await Expect(start).ToBeFocusedAsync();
        Assert.False(await start.EvaluateAsync<bool>(Inert));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tour_ShouldMoveTheHighlightAtOnce_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await StartAsync(browser);

        Assert.True(await page.Locator("[data-part=shade]").EvaluateAsync<bool>(
            "shade => getComputedStyle(shade).transitionDuration.split(',').every(time => parseFloat(time) === 0)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldMoveThroughTheTour_WhenFoundOnThePage(string browser)
    {
        var page = await StartAsync(browser);
        var tour = page.Tb().Tour();

        await tour.NextAsync();
        await tour.NextAsync();
        Assert.Equal((2, "See the map"), (await tour.StepAsync(), await tour.TitleAsync()));
        await tour.BackAsync();
        await tour.NextAsync();
        await tour.NextAsync();

        Assert.False(await tour.IsOpenAsync());
        await Expect(page.GetByText("Completed")).ToBeVisibleAsync();
    }
}
