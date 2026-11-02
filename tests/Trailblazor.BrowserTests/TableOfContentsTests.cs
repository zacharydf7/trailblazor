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
        await Expect(page.GetByText("In view: guide-climb")).ToBeVisibleAsync();
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
