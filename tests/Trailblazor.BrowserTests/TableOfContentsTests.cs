using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class TableOfContentsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenGuideAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/examples/table-of-contents", options);
        await Expect(Contents(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private static ILocator Contents(IPage page) => page.GetByRole(AriaRole.Navigation, new() { Name = "Trail guide" });

    private static ILocator Link(IPage page, string name) =>
        Contents(page).GetByRole(AriaRole.Link, new() { Name = name, Exact = true });

    private static ILocator Guide(IPage page) => page.GetByRole(AriaRole.Region, new() { Name = "Ridge loop guide" });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldMarkTheSectionInView_WhenThePanelScrolls(string browser)
    {
        var page = await OpenGuideAsync(browser);
        await Expect(Link(page, "Getting there")).ToHaveAttributeAsync("aria-current", "location");

        await page.Locator("#guide-climb").EvaluateAsync("section => section.scrollIntoView()");

        await Expect(Link(page, "The climb")).ToHaveAttributeAsync("aria-current", "location");
        await Expect(Link(page, "Getting there")).Not.ToHaveAttributeAsync("aria-current", "location");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldReportTheSection_WhenAnotherComesIntoView(string browser)
    {
        var page = await OpenAsync(browser, "/examples/table-of-contents");
        var guide = page.GetByRole(AriaRole.Navigation, new() { Name = "Packing guide" });
        await Expect(guide).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Region, new() { Name = "Packing notes" })
            .EvaluateAsync("panel => panel.scrollTop = panel.scrollHeight");

        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Section in view", Exact = true }))
            .ToHaveTextAsync("packing-food");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldMarkTheLastSectionShowing_WhenThePanelIsScrolledToTheEnd(string browser)
    {
        var page = await OpenGuideAsync(browser);

        await Guide(page).EvaluateAsync("guide => guide.scrollTop = guide.scrollHeight");

        await Expect(Link(page, "Camping")).ToHaveAttributeAsync("aria-current", "location");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldKeepTheLastSection_WhenItIsTallerThanThePanelAndScrolledToTheEnd(
        string browser)
    {
        var page = await OpenGuideAsync(browser);
        await Guide(page).EvaluateAsync("guide => guide.append(Object.assign(document.createElement('div'), "
            + "{ style: 'block-size: 40rem' }))");

        await Guide(page).EvaluateAsync("guide => guide.scrollTop = guide.scrollHeight");

        await Expect(Link(page, "Camping")).ToHaveAttributeAsync("aria-current", "location");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldScrollToTheSectionAndFocusIt_WhenANestedLinkIsFollowed(string browser)
    {
        var page = await OpenGuideAsync(browser);

        await Link(page, "Along the ridge").ClickAsync();

        await Expect(page.Locator("#guide-ridge")).ToBeFocusedAsync();
        await Expect(Link(page, "Along the ridge")).ToHaveAttributeAsync("aria-current", "location");
        await Expect(page).ToHaveURLAsync(new Regex("#guide-ridge$"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldKeepTheFollowedLinkMarked_WhenALaterSectionWouldWinByPosition(
        string browser)
    {
        var page = await OpenGuideAsync(browser);

        // Water sits near the end, so the scroll can't bring it to the top, and at the bottom Camping would win by
        // position.
        await Link(page, "Water").ClickAsync();
        await Expect(Link(page, "Water")).ToHaveAttributeAsync("aria-current", "location");
        await page.WaitForFunctionAsync("""
            guide => guide.scrollTop + guide.clientHeight >= guide.scrollHeight - 2
            """, await Guide(page).ElementHandleAsync());
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        await Expect(Link(page, "Water")).ToHaveAttributeAsync("aria-current", "location");

        // Once the scroll has settled (150 ms after its last scroll), a key or click that doesn't scroll keeps the mark,
        // and so does a scroll somewhere else.
        await page.WaitForTimeoutAsync(300);
        var guide = (await Guide(page).BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(guide.X + guide.Width / 2, guide.Y + guide.Height / 2);
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.GetByRole(AriaRole.Region, new() { Name = "Packing notes" })
            .EvaluateAsync("panel => panel.scrollTop = panel.scrollHeight");
        // Long enough for a moved mark to come back from the server and render.
        await page.WaitForTimeoutAsync(500);
        await Expect(Link(page, "Water")).ToHaveAttributeAsync("aria-current", "location");

        // The next scroll of the guide, from any source, moves the mark back to the section in view.
        await Guide(page).EvaluateAsync("guide => guide.scrollTop = 0");

        await Expect(Link(page, "Getting there")).ToHaveAttributeAsync("aria-current", "location");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldMarkTheSectionInView_WhenAScrollAwayStartsBeforeTheFollowedOneSettles(
        string browser)
    {
        var page = await OpenGuideAsync(browser, new() { ReducedMotion = ReducedMotion.Reduce });

        // The click and the scroll back run in one script, so the scroll back lands while the jump is still settling.
        await Guide(page).EvaluateAsync("""
            guide => {
                document.querySelector('a[data-target="guide-water"]').click();
                guide.scrollTop = 0;
            }
            """);

        // Getting there was marked before the click too, so this waits until the jump has settled and its mark rendered.
        await page.WaitForTimeoutAsync(500);
        await Expect(Link(page, "Getting there")).ToHaveAttributeAsync("aria-current", "location");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TableOfContents_ShouldJumpWithoutScrollingSmoothly_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await OpenGuideAsync(browser, new() { ReducedMotion = ReducedMotion.Reduce });

        await Link(page, "Camping").ClickAsync();

        // Read at once: a smooth scroll would still be on its way.
        var gap = await Guide(page).EvaluateAsync<double>("guide => guide.scrollHeight - guide.clientHeight "
            + "- guide.scrollTop");
        Assert.True(gap < 1, $"The panel was {gap}px short of the end.");
    }
}
