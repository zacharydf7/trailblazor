using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class BreadcrumbsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Trail(IPage page) => page.GetByRole(AriaRole.Navigation, new() { Name = "Breadcrumb" });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReadTheTrailAndFollowALink_WhenTheTrailIsShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/breadcrumbs");
        var trail = page.Tb().Breadcrumbs("Breadcrumb");

        var items = await trail.ItemsAsync();
        var current = await trail.CurrentAsync();
        await trail.FollowAsync("Guides");

        Assert.Equal(["Home", "Guides", "Right-to-left"], items);
        Assert.Equal("Right-to-left", current);
        await Expect(page).ToHaveURLAsync(new Regex("/guides/getting-started$"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldWaitForTheTrailAndTimeOut_WhenNoTrailHasTheName(string browser)
    {
        var page = await OpenAsync(browser, "/examples/breadcrumbs");
        page.SetDefaultTimeout(1000);

        await Assert.ThrowsAsync<TimeoutException>(() => page.Tb().Breadcrumbs("Missing").ItemsAsync());
        await Assert.ThrowsAsync<TimeoutException>(() => page.Tb().Breadcrumbs("Missing").CurrentAsync());
        await Assert.ThrowsAsync<TimeoutException>(() => page.Tb().Breadcrumbs("Missing").FollowAsync("Guides"));
    }

    // Safari drops the list role from a list without bullets unless it is set again, and the separators must not be
    // read between the links.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Breadcrumbs_ShouldReadAsAListOfLinksWithoutTheSeparators_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/breadcrumbs");

        await Expect(Trail(page)).ToMatchAriaSnapshotAsync("""
            - navigation "Breadcrumb":
              - list:
                - listitem:
                  - link "Home"
                - listitem:
                  - link "Guides"
                - listitem: Right-to-left
            """);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Breadcrumbs_ShouldShowSeparatorsOnlyBetweenItems_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/breadcrumbs");
        var separators = Trail(page).Locator("[data-part=separator]");

        await Expect(separators.Nth(0)).ToBeHiddenAsync();
        await Expect(separators.Nth(1)).ToBeVisibleAsync();
        await Expect(separators.Nth(2)).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Breadcrumbs_ShouldRunFromRightToLeftWithMirroredChevrons_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/breadcrumbs");
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");

        var home = await Trail(page).GetByRole(AriaRole.Link, new() { Name = "Home" }).BoundingBoxAsync();
        var guides = await Trail(page).GetByRole(AriaRole.Link, new() { Name = "Guides" }).BoundingBoxAsync();

        Assert.True(home!.X > guides!.X, $"Home is at {home.X}, left of Guides at {guides.X}.");
        await Expect(Trail(page).Locator("[data-part=separator] svg").Nth(1))
            .ToHaveCSSAsync("transform", "matrix(-1, 0, 0, 1, 0, 0)");
    }
}
