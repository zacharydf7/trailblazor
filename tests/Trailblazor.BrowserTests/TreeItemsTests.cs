using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreeItemsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Tree(IPage page) => page.GetByRole(AriaRole.Tree, new() { Name = "Files" });

    private static ILocator Row(IPage page, string text) => Tree(page).Locator($"[role=treeitem][data-text='{text}']");

    private async Task<IPage> OpenItemsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tree-items");
        await Expect(Tree(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeItems_ShouldOpenSelectAndCheckWithTheKeys_WhenWrittenAsMarkup(string browser)
    {
        var page = await OpenItemsAsync(browser);
        await Row(page, "Docs").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Row(page, "Docs")).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Row(page, "Guide")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("selected")).ToHaveTextAsync("guide");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync(" ");

        await Expect(page.GetByTestId("checked")).ToHaveTextAsync("api, docs, guide");
        await Expect(Row(page, "Source").Locator("strong")).ToHaveTextAsync("Source");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeItems_ShouldTakeTheirPlaceInTheMarkup_WhenOneIsShownLater(string browser)
    {
        var page = await OpenItemsAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Notes" }).ClickAsync();

        await Expect(Tree(page).Locator("[role=treeitem]")).ToHaveTextAsync(["Docs", "Notes", "Source code"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeItems_ShouldFollowTheMarkup_WhenKeyedItemsMove(string browser)
    {
        var page = await OpenItemsAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Reverse" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = "Trails" }).GetByRole(AriaRole.Treeitem))
            .ToHaveTextAsync(["Cedar Flats", "Eagle Ridge", "Pine Hollow"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldCheckAnItem_WhenTheTreeIsWrittenAsMarkup(string browser)
    {
        var page = await OpenItemsAsync(browser);

        await page.Tb().Tree("Files").CheckAsync("Docs", "API");

        await Expect(page.GetByTestId("checked")).ToHaveTextAsync("api");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeItems_ShouldHaveNoAccessibilityViolations_WhenOpen(string browser)
    {
        var page = await OpenItemsAsync(browser);
        await page.Tb().Tree("Files").ExpandAsync("Docs");

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }
}
