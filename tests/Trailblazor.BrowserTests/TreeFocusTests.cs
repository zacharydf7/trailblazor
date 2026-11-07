namespace Trailblazor.BrowserTests;

public class TreeFocusTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tree-focus");
        await Expect(page.GetByRole(AriaRole.Tree)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private static ILocator Row(IPage page, string text) =>
        page.GetByRole(AriaRole.Tree).Locator($"[role=treeitem][data-text='{text}']");

    // Focusing the button and pressing Enter works in every browser; in WebKit a click doesn't focus it.
    private static async Task DeleteAsync(IPage page, string row)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = $"Delete {row}", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Row(page, row)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFocusTheNextRow_WhenTheFocusedRowIsDeleted(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await DeleteAsync(page, "Ridge");

        await Expect(Row(page, "Pass")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFocusTheRowBefore_WhenTheLastRowIsDeleted(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await DeleteAsync(page, "West");

        await Expect(Row(page, "East")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFocusTheRowNowInItsPlace_WhenRowsAboveWentFirst(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Row(page, "South").FocusAsync();

        await page.Keyboard.PressAsync("F3");

        await Expect(Row(page, "South")).ToHaveCountAsync(0);
        await Expect(Row(page, "East")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFocusTheParent_WhenCodeClosesItAroundTheFocusedRow(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Row(page, "Pass").FocusAsync();

        await page.Keyboard.PressAsync("F2");

        await Expect(Row(page, "Pass")).ToHaveCountAsync(0);
        await Expect(Row(page, "North")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFocusTheClosedRow_WhenCodeClosesARowAboveTheFocusedRowsParent(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Row(page, "Summit").FocusAsync();

        await page.Keyboard.PressAsync("F2");

        await Expect(Row(page, "Summit")).ToHaveCountAsync(0);
        await Expect(Row(page, "North")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldLeaveFocusOutside_WhenItsRowGoesAfterFocusLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Row(page, "East").FocusAsync();
        await page.Keyboard.PressAsync("F4");

        await page.GetByText("Text outside the tree").ClickAsync();
        await Expect(Row(page, "East")).ToHaveCountAsync(0);

        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body"));
    }
}
