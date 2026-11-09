namespace Trailblazor.BrowserTests;

public class EmptyStateTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EmptyState_ShouldHideItsIconFromScreenReaders_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/empty-state");

        var empty = page.Locator(".tb-empty-state").First;

        await Expect(empty).ToMatchAriaSnapshotAsync("""
            - heading "No trails saved yet" [level=3]
            - text: Trails you save show here, ready for your next hike.
            - button "Find a trail"
            """);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EmptyState_ShouldShowASmallerIcon_WhenCompact(string browser)
    {
        var page = await OpenAsync(browser, "/examples/empty-state");

        var sizes = await page.Locator("[data-part=icon] svg")
            .EvaluateAllAsync<double[]>("icons => icons.map(icon => icon.getBoundingClientRect().width)");

        Assert.Equal([48, 32], sizes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EmptyStateExample_ShouldPutFocusBackInTheSearch_WhenItsActionClearsIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/empty-state");

        await page.GetByRole(AriaRole.Button, new() { Name = "Show every trail" }).ClickAsync();

        await Expect(page.GetByLabel("Find a trail")).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Matching trails" }).GetByRole(AriaRole.Listitem))
            .ToHaveCountAsync(3);
    }
}
