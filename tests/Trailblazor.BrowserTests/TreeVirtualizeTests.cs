using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreeVirtualizeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Atlas = "Trail atlas";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldRenderTheRowsInViewOnly_WhenScrolledThroughTenThousandRows(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var tree = Tree(page);
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await tree.ScrollIntoViewIfNeededAsync();

        await tree.EvaluateAsync("tree => tree.scrollTop = tree.scrollHeight");

        await Expect(Row(page, "Trail 100.100")).ToBeInViewportAsync();
        Assert.InRange(await tree.Locator("[role=treeitem]").CountAsync(), 10, 60);
        await Expect(Row(page, "Trail 100.100")).ToHaveAttributeAsync("aria-posinset", "100");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldReachRowsThatArentRendered_WhenEndHomeAndTypingMoveFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        await Expect(Tree(page)).ToHaveAttributeAsync("data-ready", "");
        await Tree(page).ScrollIntoViewIfNeededAsync();
        await Row(page, "Region 1").FocusAsync();

        await page.Keyboard.PressAsync("End");
        await Expect(Row(page, "Trail 100.100")).ToBeFocusedAsync();
        await Expect(Row(page, "Trail 100.100")).ToBeInViewportAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Row(page, "Region 1")).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("region 50");

        await Expect(Row(page, "Region 50")).ToBeFocusedAsync();
        await Expect(Row(page, "Region 50")).ToBeInViewportAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldKeepTheFocusedRowAndMoveOnFromIt_WhenTheTreeIsScrolledAway(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");
        var tree = Tree(page);
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await tree.ScrollIntoViewIfNeededAsync();
        await Row(page, "Trail 1.2").FocusAsync();

        await tree.EvaluateAsync("tree => tree.scrollTop = tree.scrollHeight");
        await Expect(Row(page, "Trail 100.100")).ToBeAttachedAsync();
        await Expect(Row(page, "Trail 1.2")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Row(page, "Trail 1.3")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Row(page, "Region 1")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeLocator_ShouldFindARowFarBelowItsParent_WhenTheTreeIsVirtualized(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        var row = await page.Tb().Tree(Atlas).ItemAsync("Region 70", "Trail 70.90");
        await row.ClickAsync();

        await Expect(row).ToHaveTextAsync("Trail 70.90");
        await Expect(row).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFollowTheScroll_WhenVirtualizeTurnsOnAfterTheTreeAttaches(string browser)
    {
        var page = await OpenAsync(browser, "/tree-virtualize");
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Numbers" });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        await Expect(tree).ToHaveAttributeAsync("data-virtual", "");
        await tree.EvaluateAsync("tree => tree.scrollTop = tree.scrollHeight");

        await Expect(tree.Locator("[role=treeitem][data-text='Number 2000']")).ToBeInViewportAsync();
    }

    private static ILocator Tree(IPage page) => page.GetByRole(AriaRole.Tree, new() { Name = Atlas });

    private static ILocator Row(IPage page, string text) =>
        Tree(page).Locator($"[role=treeitem][data-text='{text}']");
}
