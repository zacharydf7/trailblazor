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
    public async Task NestedTree_ShouldPassAClickOffItsRowsToTheRowAroundIt_WhenClicked(string browser)
    {
        var page = await OpenItemsAsync(browser);
        var sections = page.GetByRole(AriaRole.Tree, new() { Name = "Sections" });
        await Expect(sections).ToHaveAttributeAsync("data-ready", "");

        await sections.DispatchEventAsync("click");

        await Expect(page.GetByTestId("outline")).ToHaveTextAsync("chapter");
        await Expect(page.GetByTestId("sections")).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldKeepItsEllipsis_WhenARowsContentUsesTheTextPart(string browser)
    {
        var page = await OpenItemsAsync(browser);
        var text = page.GetByRole(AriaRole.Tree, new() { Name = "Long names" }).Locator("[data-part=text]");

        var shown = await text.EvaluateAsync<string>(
            "text => `${text.scrollWidth > text.clientWidth} ${getComputedStyle(text).textOverflow}`");

        Assert.Equal("true ellipsis", shown);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldKeepItsEllipsisAndItsMatchHighlight_WhenATemplateWrapsItsMarkedText(string browser)
    {
        var page = await OpenItemsAsync(browser);
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Wrapped names" });
        var region = tree.Locator("[role=treeitem][data-text^='North']");
        var trail = tree.Locator("[role=treeitem][data-text^='Pine']");
        var spur = tree.Locator("[role=treeitem][data-text^='Waterfall']");
        await Expect(spur).ToBeVisibleAsync();

        // Each name is cut off and ends before the row's button: the two in a wrapper, and the spur's, a TbText
        // marked straight in the template. The loop's wrapper lays a mark out beside its name, on the same line.
        foreach (var row in new[] { region, trail, spur })
        {
            var layout = await row.EvaluateAsync<string>("""
                row => {
                    const text = row.querySelector('[data-part=text]');
                    const end = row.querySelector('[data-part=end]');
                    return [text.scrollWidth > text.clientWidth, getComputedStyle(text).textOverflow,
                        text.getBoundingClientRect().right <= end.getBoundingClientRect().left + 1].join(' ');
                }
                """);
            Assert.Equal("true ellipsis true", layout);
        }

        var beside = await trail.EvaluateAsync<bool>("""
            row => {
                const mark = row.querySelector('[data-testid=mark]').getBoundingClientRect();
                const text = row.querySelector('[data-part=text]').getBoundingClientRect();
                return mark.right <= text.left + 1 && mark.top < text.bottom && text.top < mark.bottom;
            }
            """);
        Assert.True(beside, "The loop's name isn't beside its mark.");

        // The filter's matches stand out, and their parent row doesn't.
        var weights = new List<int>();
        foreach (var row in new[] { region, trail, spur })
        {
            weights.Add(await row.Locator("[data-part=text]").First.EvaluateAsync<int>(
                "text => Number(getComputedStyle(text).fontWeight)"));
        }

        Assert.True(weights[1] > weights[0] && weights[2] > weights[0],
            $"The matches' weights are {weights[1]} and {weights[2]}, their region's {weights[0]}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldLeaveAnotherComponentsTextPartAlone_WhenARowsTemplateHoldsALabel(string browser)
    {
        var page = await OpenItemsAsync(browser);
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Wrapped names" });
        var label = tree.Locator("[role=treeitem][data-text^='Stone'] .tb-label__text");
        await Expect(label).ToBeVisibleAsync();

        // The label's text is a part named text too, and keeps wrapping as a label does.
        Assert.Equal("text", await label.GetAttributeAsync("data-part"));
        await Expect(label).ToHaveCSSAsync("text-overflow", "clip");
        await Expect(label).Not.ToHaveCSSAsync("white-space", "nowrap");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldKeepAFewCharactersClearOfTheEnd_WhenARowIsDeeperThanTheTreeIsWide(
        string browser)
    {
        var page = await OpenItemsAsync(browser);
        var deep = page.GetByRole(AriaRole.Tree, new() { Name = "Deep" });
        var row = deep.Locator("[role=treeitem][data-key='deep-9']");
        var text = row.Locator(":scope > .tb-tree__content > .tb-tree__text");
        await Expect(text).ToBeVisibleAsync();

        var width = await text.EvaluateAsync<double>("text => text.getBoundingClientRect().width");
        var clear = await row.EvaluateAsync<bool>("""
            row => row.querySelector(':scope > .tb-tree__content > .tb-tree__text').getBoundingClientRect().right
                <= row.querySelector(':scope > .tb-tree__end').getBoundingClientRect().left
            """);

        Assert.True(width >= 30, $"The text is {width}px wide.");
        Assert.True(clear);
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
        // The release lands below the pressed row, so the browser clicks the inner tree, and that click mustn't select
        // the row around it. Blazor handles a page's events in order, so once the next click's selection shows, a
        // stray click's would have too.
        await sections.Locator("[role=treeitem][data-text='Detail']").ClickAsync();
        await Expect(page.GetByTestId("sections")).ToHaveTextAsync("detail");
        await Expect(page.GetByTestId("outline")).ToHaveTextAsync("");
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
    public async Task NestedTree_ShouldLeaveTheRowAroundItAlone_WhenADoubleClicksSecondPressEndsOnAnotherRow(
        string browser)
    {
        var page = await OpenItemsAsync(browser);
        var sections = page.GetByRole(AriaRole.Tree, new() { Name = "Sections" });
        await Expect(sections).ToHaveAttributeAsync("data-ready", "");
        var box = (await sections.Locator("[role=treeitem][data-text='Intro']").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();
        await page.Mouse.DownAsync(new() { ClickCount = 2 });
        await page.Mouse.MoveAsync(x, y + 30, new() { Steps = 5 });
        await page.Mouse.UpAsync(new() { ClickCount = 2 });

        // Blazor handles a page's events in order, so once the next click's selection shows, a stray one's would too.
        await sections.Locator("[role=treeitem][data-text='Body']").ClickAsync();
        await Expect(page.GetByTestId("sections")).ToHaveTextAsync("body");
        var chapter = page.GetByRole(AriaRole.Tree, new() { Name = "Outline" })
            .Locator("[role=treeitem][data-text='Chapter']");
        await Expect(chapter).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByTestId("outline")).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NestedTree_ShouldLeaveTheRowAroundItAlone_WhenADoubleClicksSecondPressDragsARow(string browser)
    {
        var page = await OpenAsync(browser, "/nested-tree-content");
        var maps = page.GetByRole(AriaRole.Tree, new() { Name = "Maps" });
        await Expect(maps).ToHaveAttributeAsync("data-ready", "");
        var from = (await maps.Locator("[role=treeitem][data-text='North']").BoundingBoxAsync())!;
        var to = (await maps.Locator("[role=treeitem][data-text='South']").BoundingBoxAsync())!;
        var (x, y) = (from.X + from.Width / 2, from.Y + from.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();
        await page.Mouse.DownAsync(new() { ClickCount = 2 });
        await page.Mouse.MoveAsync(x, to.Y + to.Height / 2, new() { Steps = 10 });
        await page.Mouse.UpAsync(new() { ClickCount = 2 });

        // Blazor handles a page's events in order, so once the button's count shows, a stray double-click's toggle
        // would have too.
        await page.GetByRole(AriaRole.Button, new() { Name = "Notes" }).ClickAsync();
        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("1");
        await Expect(page.GetByRole(AriaRole.Tree, new() { Name = "Atlas" })
            .Locator("[role=treeitem][data-text='Region']")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NestedTree_ShouldLetAKeyClickContentBetweenItsRows_WhenAPressEndedOutsideTheTree(string browser)
    {
        var page = await OpenAsync(browser, "/nested-tree-content");
        var parts = page.GetByRole(AriaRole.Tree, new() { Name = "Parts" });
        await Expect(parts).ToHaveAttributeAsync("data-ready", "");
        var notes = page.GetByRole(AriaRole.Button, new() { Name = "Notes" });
        await Expect(notes).ToBeVisibleAsync();
        var from = (await parts.Locator("[role=treeitem][data-text='Intro']").BoundingBoxAsync())!;
        var to = (await page.GetByRole(AriaRole.Tree, new() { Name = "Guide" })
            .Locator("[role=treeitem][data-text='Appendix']").BoundingBoxAsync())!;

        // The press starts on an inner row and ends on an outer one, so its click never reaches the inner tree.
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await notes.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("1");
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
