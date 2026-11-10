using System.Text.RegularExpressions;

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
}
