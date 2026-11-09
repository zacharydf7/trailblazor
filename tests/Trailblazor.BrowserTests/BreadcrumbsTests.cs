namespace Trailblazor.BrowserTests;

public class BreadcrumbsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Trail(IPage page) => page.GetByRole(AriaRole.Navigation, new() { Name = "Breadcrumb" });

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
