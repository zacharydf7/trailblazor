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
        // The arrows pass over what is written between the rows.
        await Row(page, "Docs").FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Row(page, "Docs")).ToHaveAttributeAsync("aria-expanded", "false");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Row(page, "Source")).ToBeFocusedAsync();
        await Expect(Tree(page).GetByTestId("between")).ToBeVisibleAsync();
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
    public async Task Helper_ShouldOpenRowsAsTheyAppearAndFollowThePathThatLeadsOn_WhenRowsShareTextsAndKeys(
        string browser)
    {
        var page = await OpenItemsAsync(browser);
        var library = page.Tb().Tree("Library");

        await library.ExpandAllAsync();

        // Legend's row appears only once Map opens, which only appears once Atlas opens.
        await Expect(await library.ItemAsync("Shelf", "Atlas", "Map", "Legend")).ToBeVisibleAsync();
        Assert.Equal(2, (await library.ItemsAsync("Shelf")).Count);
        var index = page.GetByRole(AriaRole.Tree, new() { Name = "Index" });
        await Expect(index.Locator("[role=treeitem][data-text='Entries']"))
            .ToHaveAttributeAsync("aria-expanded", "false");
        await page.Tb().Tree("Index").ExpandAsync("Entries");
        await Expect(await page.Tb().Tree("Index").ItemAsync("Entries", "Entry")).ToBeVisibleAsync();
        // The path to Page passes the rows of the Sections tree inside Chapter's row, which aren't Outline's.
        await Expect(await page.Tb().Tree("Outline").ItemAsync("Part one", "Chapter", "Page")).ToBeVisibleAsync();
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeItem_ShouldWarnInTheConsole_WhenItHoldsContentButNoRowsAndNoHasChildren(string browser)
    {
        var page = await OpenHiddenContentAsync(browser);

        Assert.Equal(["Gate", "Notes"], await HiddenContentWarningsAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeItem_ShouldNotWarn_WhenALeafRowGetsItsFirstChildRow(string browser)
    {
        var page = await OpenHiddenContentAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Add a pond trail" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Pond" })).ToHaveAttributeAsync("aria-expanded",
            "false");
        Assert.Equal(["Gate", "Notes"], await HiddenContentWarningsAsync(page));
    }

    // Declared rows exist only once their tree has built them, which is when the check can run.
    private async Task<IPage> OpenHiddenContentAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tree-hidden-content");
        foreach (var row in new[] { "Notes", "Docs", "Ridge", "Pond", "Lake", "Gate" })
        {
            await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = row })).ToBeVisibleAsync();
        }

        return page;
    }

    // The rows the warnings name, sorted, since the trees may build in either order. A console message can reach the
    // test a little after the page logs it, as in WebKit, so this waits up to five seconds for the two expected, then
    // half a second more for any that shouldn't be there.
    private static async Task<string[]> HiddenContentWarningsAsync(IPage page)
    {
        var rows = await WarnedRowsAsync(page);
        for (var waited = 0; rows.Length < 2 && waited < 5000; waited += 100)
        {
            await page.WaitForTimeoutAsync(100);
            rows = await WarnedRowsAsync(page);
        }

        await page.WaitForTimeoutAsync(500);
        return await WarnedRowsAsync(page);
    }

    private static async Task<string[]> WarnedRowsAsync(IPage page) => [.. (await page.ConsoleMessagesAsync())
        .Where(message => message.Type == "warning" && message.Text.Contains("Set HasChildren to show the content"))
        .Select(message => message.Text.Split('"')[1]).Order()];
}
