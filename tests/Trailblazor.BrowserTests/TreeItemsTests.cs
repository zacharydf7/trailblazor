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
    public async Task NestedTree_ShouldKeepItsKeysAndClicksToItself_WhenInsideAnotherTreesRow(string browser)
    {
        var page = await OpenItemsAsync(browser);
        var outline = page.GetByRole(AriaRole.Tree, new() { Name = "Outline" });
        var sections = page.GetByRole(AriaRole.Tree, new() { Name = "Sections" });
        await Expect(sections).ToHaveAttributeAsync("data-ready", "");
        var intro = sections.Locator("[role=treeitem][data-text='Intro']");

        var body = sections.Locator("[role=treeitem][data-text='Body']");
        // The open Chapter row around the inner tree mustn't turn the inner tree's closed chevrons.
        await Expect(outline.Locator("[role=treeitem][data-text='Chapter']")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(body.Locator(".tb-tree__chevron")).ToHaveCSSAsync("transform", "none");

        await intro.ClickAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(body).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(sections.Locator("[role=treeitem][data-text='Detail']")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("sections")).ToHaveTextAsync("intro");
        await Expect(page.GetByTestId("outline")).ToHaveTextAsync("");
        // A press on an inner row, which can't be dragged, mustn't drag the outer row it sits in.
        var box = (await intro.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2 + 30, new() { Steps = 5 });
        await Expect(outline).Not.ToHaveAttributeAsync("data-dragging", "");
        await page.Mouse.UpAsync();
        // Focus in the inner tree leaves the outer tree's tab stop on its own row.
        await Expect(outline.Locator("[role=treeitem][data-text='Part one']")).ToHaveAttributeAsync("tabindex", "0");
        await outline.Locator("[role=treeitem][data-text='Page']").FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(outline.Locator("[role=treeitem][data-text='Chapter']")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");

        await Expect(outline.Locator("[role=treeitem][data-text='Part one']")).ToBeFocusedAsync();
        var result = await page.RunAxe(AxeTests.WcagAA);
        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
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
