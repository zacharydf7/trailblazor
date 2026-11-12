using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreeSelectTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static async Task<ILocator> OpenFieldAsync(IPage page)
    {
        var field = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Trail ") });
        await Expect(field).ToHaveAttributeAsync("data-ready", "");
        return field;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelect_ShouldSetItsRowsOffThePopupsEdges_WhenOpened(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenFieldAsync(page);
        await field.ClickAsync();
        var popup = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail", Exact = true });
        await Expect(popup).ToBeVisibleAsync();

        // Measured together, since the popup is placed a frame after it opens.
        var gaps = await popup.GetByRole(AriaRole.Treeitem).First.EvaluateAsync<double[]>("""
            row => {
                const own = row.getBoundingClientRect();
                const panel = row.closest('[role=dialog]').getBoundingClientRect();
                const tree = row.closest('.tb-tree-select__tree').getBoundingClientRect();
                return [own.left - panel.left, panel.right - own.right, own.top - tree.top];
            }
            """);

        Assert.True(gaps[0] >= 4, $"The row starts {gaps[0]}px in.");
        Assert.True(gaps[1] >= 4, $"The row ends {gaps[1]}px in.");
        Assert.True(gaps[2] >= 4, $"The first row is {gaps[2]}px below the tree's top.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelectLocator_ShouldSearchChooseAndReadTheField_WhenUsedByLabel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = page.Tb().TreeSelect("Trail");
        Assert.Null(await field.TextAsync());

        await field.SearchAsync("cedar");
        Assert.Equal(2, await page.GetByRole(AriaRole.Treeitem).CountAsync());
        await field.ChooseAsync("River valley", "Cedar Flats");

        await Expect(page.GetByLabel("Chosen trail")).ToHaveTextAsync("Chosen: cedar.");
        Assert.Equal("Cedar Flats", await field.TextAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelectLocator_ShouldUncheckEveryRow_WhenTheRegionIsPartlyChecked(string browser)
    {
        var page = await OpenAsync(browser, "/tree-select-states");
        var visited = page.GetByLabel("Visited", new() { Exact = true });
        await Expect(visited).ToHaveTextAsync("pine");

        await page.Tb().TreeSelect("Visited trails").UncheckAsync("North ridge");

        await Expect(visited).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelectLocator_ShouldLeaveAnItemChecked_WhenCheckedAgain(string browser)
    {
        var page = await OpenAsync(browser, "/tree-select-states");

        await page.Tb().TreeSelect("Visited trails").CheckAsync("North ridge", "Pine Hollow");

        await Expect(page.GetByLabel("Visited", new() { Exact = true })).ToHaveTextAsync("pine");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelectLocator_ShouldThrow_WhenTheFieldCantChange(string browser)
    {
        var page = await OpenAsync(browser, "/tree-select-states");
        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Plain trail") }))
            .ToHaveAttributeAsync("data-ready", "");

        foreach (var label in new[] { "Closed trail", "Fixed trail" })
        {
            var problem = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                page.Tb().TreeSelect(label).ChooseAsync("North ridge", "Pine Hollow"));
            Assert.Contains("disabled or read-only", problem.Message);
        }

        var search = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            page.Tb().TreeSelect("Plain trail").SearchAsync("pine"));
        Assert.Contains("no search", search.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelectLocator_ShouldCheckAndUncheckItems_WhenUsedByLabel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = page.Tb().TreeSelect("Regions");

        await field.CheckAsync("Coast", "Harbor bay");
        await Expect(page.GetByLabel("Chosen regions")).ToHaveTextAsync("Chosen: bay.");
        await field.UncheckAsync("Coast", "Harbor bay");

        await Expect(page.GetByLabel("Chosen regions")).ToHaveTextAsync("No regions chosen.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelect_ShouldChooseFromTheKeyboardAndReturnToTheField_WhenOpened(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenFieldAsync(page);

        await field.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Trail", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");
        var region = page.GetByRole(AriaRole.Treeitem, new() { Name = "North ridge" });
        await Expect(region).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(region).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Pine Hollow loop" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Chosen trail")).ToHaveTextAsync("Chosen: pine.");
        await Expect(field).ToBeFocusedAsync();
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(field).ToContainTextAsync("Pine Hollow loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Search_ShouldMoveToTheFirstMatch_WhenDownArrowIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenFieldAsync(page);

        await field.ClickAsync();
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" }).FillAsync("cedar");
        await Expect(page.GetByRole(AriaRole.Treeitem)).ToHaveCountAsync(2);
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Cedar Flats" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Search_ShouldMoveToAMatchOfAllTheText_WhenDownArrowFollowsTypingAtOnce(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenFieldAsync(page);
        await field.ClickAsync();

        // Down Arrow comes before the search's pause in typing is over.
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" }).PressSequentiallyAsync("cedar");
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Cedar Flats" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Search_ShouldMoveToTheChosenItemOrTheFirst_WhenDownArrowIsPressedWithoutASearch(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenFieldAsync(page);
        var search = page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" });

        await field.ClickAsync();
        await Expect(search).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "North ridge" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await page.GetByRole(AriaRole.Treeitem, new() { Name = "Eagle Ridge traverse" }).ClickAsync();
        await Expect(field).ToContainTextAsync("Eagle Ridge traverse");
        await field.ClickAsync();
        await Expect(search).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Eagle Ridge traverse" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelect_ShouldCloseWithoutChoosing_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenFieldAsync(page);

        await field.ClickAsync();
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("Escape");

        await Expect(field).ToBeFocusedAsync();
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByLabel("Chosen trail")).ToHaveTextAsync("No trail chosen.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Clear_ShouldEmptyTheFieldAndFocusIt_WhenTheClearButtonIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenFieldAsync(page);
        await field.ClickAsync();
        await page.GetByRole(AriaRole.Treeitem, new() { Name = "North ridge" }).Locator("[data-part=toggle]")
            .ClickAsync();
        await page.GetByRole(AriaRole.Treeitem, new() { Name = "Pine Hollow loop" }).ClickAsync();
        await Expect(page.GetByLabel("Chosen trail")).ToHaveTextAsync("Chosen: pine.");

        await page.GetByRole(AriaRole.Button, new() { Name = "Clear" }).ClickAsync();

        await Expect(page.GetByLabel("Chosen trail")).ToHaveTextAsync("No trail chosen.");
        await Expect(field).ToContainTextAsync("Choose a trail");
        await Expect(field).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear" })).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiTreeSelect_ShouldCheckFromTheKeyboardAndStayOpen_WhenEnterOrSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenRegionsAsync(page);

        await field.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var coast = page.GetByRole(AriaRole.Treeitem, new() { Name = "Coast" });
        await Expect(coast).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(coast).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowDown");
        var bay = page.GetByRole(AriaRole.Treeitem, new() { Name = "Harbor bay" });
        await Expect(bay).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(bay).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(coast).ToHaveAttributeAsync("aria-checked", "mixed");
        await Expect(page.GetByLabel("Chosen regions")).ToHaveTextAsync("Chosen: bay.");
        await Expect(bay).ToBeFocusedAsync();
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");

        await page.Keyboard.PressAsync("Space");
        await Expect(page.GetByLabel("Chosen regions")).ToHaveTextAsync("No regions chosen.");
        await page.Keyboard.PressAsync("Escape");
        await Expect(field).ToBeFocusedAsync();
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiTreeSelect_ShouldCheckAParentsItemsAndShowTheirText_WhenItsRowIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenRegionsAsync(page);

        await field.ClickAsync();
        await page.GetByRole(AriaRole.Treeitem, new() { Name = "Forest" }).ClickAsync();

        foreach (var key in new[] { "forest", "birch", "spruce" })
        {
            await Expect(page.GetByLabel("Chosen regions")).ToContainTextAsync(key);
        }

        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(field).ToContainTextAsync("Forest");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiTreeSelect_ShouldMoveToTheFirstMatchAndCheckIt_WhenDownArrowThenSpaceIsPressed(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-select");
        var field = await OpenRegionsAsync(page);

        await field.ClickAsync();
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" }).FillAsync("moor");
        await Expect(page.GetByRole(AriaRole.Treeitem)).ToHaveCountAsync(2);
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByRole(AriaRole.Treeitem, new() { Name = "Open moor" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Space");

        await Expect(page.GetByLabel("Chosen regions")).ToContainTextAsync("moor");
    }

    private static async Task<ILocator> OpenRegionsAsync(IPage page)
    {
        var field = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Regions ") });
        await Expect(field).ToHaveAttributeAsync("data-ready", "");
        return field;
    }
}
