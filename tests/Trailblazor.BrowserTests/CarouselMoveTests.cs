using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Moves between slides, on slides that take two seconds to move so a test can look at one while it runs.
public class CarouselMoveTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static readonly Regex AnyMove = new(".");

    private static ILocator Carousel(IPage page, string name = "Trail photos") =>
        page.GetByRole(AriaRole.Region, new() { Name = name });

    private static ILocator Slides(ILocator carousel) => carousel.Locator("[data-part=slides]");

    private async Task<IPage> OpenMovingAsync(string browser, bool rtl = false, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, $"/carousel-move?rtl={rtl}", options);
        await Expect(Carousel(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // Presses a button, waits for the move it starts, and says which side of the leaving slide the arriving one is on.
    private static async Task<string> SideOfArrivalAsync(ILocator carousel, string button)
    {
        await carousel.Locator($"[data-part={button}]").ClickAsync();
        await Expect(Slides(carousel)).ToHaveAttributeAsync("data-move", AnyMove);
        var offset = await carousel.EvaluateAsync<double>("""
            carousel => {
                const leaving = carousel.querySelector('[data-leaving]').getBoundingClientRect();
                const arriving = carousel.querySelector('[data-part=slide]:not([hidden]):not([data-leaving])')
                    .getBoundingClientRect();
                return arriving.left - leaving.left;
            }
            """);
        await Expect(Slides(carousel)).Not.ToHaveAttributeAsync("data-move", AnyMove, new() { Timeout = 5000 });
        return offset > 0 ? "right" : "left";
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldBringTheSlideInFromTheWayItTurns_WhenItWrapsEitherWayInEitherDirection(
        string browser)
    {
        var sides = new List<string>();
        foreach (var rtl in new[] { false, true })
        {
            var carousel = Carousel(await OpenMovingAsync(browser, rtl));
            // From the first slide, Previous wraps back to the last and Next wraps forward to the first again.
            sides.Add(await SideOfArrivalAsync(carousel, "previous"));
            sides.Add(await SideOfArrivalAsync(carousel, "next"));
        }

        Assert.Equal(["left", "right", "right", "left"], sides);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldTurnWithoutAMove_WhenTheUserAsksForReducedMotion(string browser)
    {
        var page = await OpenMovingAsync(browser, options: new() { ReducedMotion = ReducedMotion.Reduce });
        var carousel = Carousel(page);
        await carousel.EvaluateAsync("""
            carousel => new MutationObserver(() => window.moved ||= carousel.querySelector('[data-move]') !== null)
                .observe(carousel, { subtree: true, attributes: true })
            """);

        await carousel.Locator("[data-part=next]").ClickAsync();
        await Expect(carousel.Locator("[data-part=slide]:not([hidden])")).ToHaveTextAsync(["Cedar"]);

        Assert.False(await page.EvaluateAsync<bool>("() => window.moved === true"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldReplaceTheMoveWithTwoSlidesDrawn_WhenTurnedAgainWhileMoving(string browser)
    {
        var carousel = Carousel(await OpenMovingAsync(browser));
        var next = carousel.Locator("[data-part=next]");

        await next.ClickAsync();
        await Expect(Slides(carousel)).ToHaveAttributeAsync("data-move", "1");
        await next.ClickAsync();
        await Expect(Slides(carousel)).ToHaveAttributeAsync("data-move", "2");
        var drawn = await carousel.Locator("[data-part=slide]:not([hidden])").AllTextContentsAsync();
        await Expect(Slides(carousel)).Not.ToHaveAttributeAsync("data-move", AnyMove, new() { Timeout = 5000 });

        Assert.Equal(["Cedar", "Eagle"], drawn.Select(text => text.Trim()));
        await Expect(carousel.Locator("[data-part=slide]:not([hidden])")).ToHaveTextAsync(["Eagle"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldEndTheMove_WhenItsAnimationIsCanceledWithoutATurn(string browser)
    {
        var page = await OpenMovingAsync(browser);
        var carousel = Carousel(page);
        await carousel.Locator("[data-part=next]").ClickAsync();
        await Expect(Slides(carousel)).ToHaveAttributeAsync("data-move", "1");

        // A new transition swaps the running animations, which cancels them, well before the two seconds are up.
        await page.GetByRole(AriaRole.Button, new() { Name = "Fade the trails" }).ClickAsync();

        await Expect(Slides(carousel)).Not.ToHaveAttributeAsync("data-move", AnyMove, new() { Timeout = 1000 });
        await Expect(carousel.Locator("[data-part=slide]:not([hidden])")).ToHaveTextAsync(["Cedar"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldMoveEachTurnAndSettle_WhenItsSlidesTurnByThemselves(string browser)
    {
        var page = await OpenMovingAsync(browser);
        var camps = Carousel(page, "Camp photos");
        await Expect(camps).ToHaveAttributeAsync("data-ready", "");
        await camps.EvaluateAsync("""
            camps => new MutationObserver(records => window.moves = (window.moves ?? 0)
                + records.filter(record => record.target.dataset.move !== undefined).length)
                .observe(camps, { subtree: true, attributeFilter: ['data-move'] })
            """);

        await Expect(camps.Locator("[data-part=slide]:not([hidden])")).ToHaveTextAsync(["Eagle camp"],
            new() { Timeout = 10_000 });
        await Expect(Slides(camps)).Not.ToHaveAttributeAsync("data-move", AnyMove);

        Assert.True(await page.EvaluateAsync<int>("() => window.moves") >= 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldHaveNoAccessibilityViolations_WhenMovingAndOnceSettled(string browser)
    {
        var page = await OpenMovingAsync(browser);
        var carousel = Carousel(page);

        await carousel.Locator("[data-part=next]").ClickAsync();
        await Expect(Slides(carousel)).ToHaveAttributeAsync("data-move", "1");
        // The camps turn by themselves, so the check looks only at the carousel that moves when asked.
        var during = await carousel.RunAxe(AxeTests.WcagAA);
        await Expect(Slides(carousel)).Not.ToHaveAttributeAsync("data-move", AnyMove, new() { Timeout = 5000 });
        var after = await carousel.RunAxe(AxeTests.WcagAA);

        Assert.Equal(("", ""), (Describe(during), Describe(after)));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReturnOnceTheMoveHasEnded_WhenItTurnsTheSlides(string browser)
    {
        var page = await OpenMovingAsync(browser);

        await page.Tb().Carousel("Trail photos").NextAsync();

        Assert.Null(await Slides(Carousel(page)).GetAttributeAsync("data-move"));
    }

    private static string Describe(AxeResult result) =>
        string.Join("; ", result.Violations.Select(violation => $"{violation.Id}: {violation.Help}"));
}
