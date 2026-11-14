using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The Playwright reads and no-name lookups that match bUnit's helpers of the same names.
public class DriverReadsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Options_ShouldListTheOptionsInOrder_WhenReadFromASelectOrAListBox(string browser)
    {
        var page = await OpenAsync(browser, "/examples/select");
        var listBoxes = await OpenAsync(browser, "/examples/listbox");

        Assert.Equal(["Standard (3-5 days)", "Express (1 day)", "Overnight"],
            await page.Tb().Select("Shipping").OptionsAsync());
        Assert.Equal(["Ridge loop", "Creek path", "Summit spur", "Lakeshore", "Old mill", "Fern gully"],
            await listBoxes.Tb().ListBox("Trail").OptionsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Reads_ShouldGiveWhatTheComponentsShow_WhenAsked(string browser)
    {
        var tabs = await OpenAsync(browser, "/examples/tabs");
        var numbers = await OpenAsync(browser, "/examples/number-field");
        await numbers.Tb().NumberField("Quantity").EnterAsync("7");
        var trees = await OpenAsync(browser, "/examples/tree-view");

        Assert.Equal(["Route", "Alerts", "Filters"], await tabs.Tb().Tabs("Trail settings").TabsAsync());
        Assert.Equal("Route", await tabs.Tb().Tabs("Trail settings").SelectedAsync());
        Assert.Equal("7", await numbers.Tb().NumberField("Quantity").TextAsync());
        Assert.Equal(["North ridge", "River valley"], await trees.Tb().Tree("Open and closed regions").TextsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Texts_ShouldBeEmpty_WhenTheTreeHasNoRows(string browser)
    {
        var page = await OpenAsync(browser, "/tree-starts-empty");

        Assert.Empty(await page.Tb().Tree("Stops").TextsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IsSigned_ShouldTurnTrue_WhenSomethingIsDrawn(string browser)
    {
        var page = await OpenAsync(browser, "/signature-pad");
        var pad = page.Tb().SignaturePad("Signature");
        Assert.False(await pad.IsSignedAsync());

        await pad.DrawAsync((50, 50), (150, 120));

        Assert.True(await pad.IsSignedAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IsOpen_ShouldFollowThePalette_WhenItsShortcutOpensIt(string browser)
    {
        var page = await OpenAsync(browser, "/command-palette");
        var palette = page.Tb().CommandPalette("Command palette");
        Assert.False(await palette.IsOpenAsync());

        await palette.OpenAsync();
        await palette.OpenAsync();

        // Had the second call closed the palette, the search would fail.
        await palette.SearchAsync("tab");
        Assert.True(await palette.IsOpenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ClickRow_ShouldRaiseTheRowClick_WhenGivenACellsText(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");

        await page.Tb().Grid("Trail maps").ClickRowAsync("Pine Hollow loop");

        await Expect(page.GetByLabel("Map open")).ToContainTextAsync("Map open: Pine Hollow loop.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextMenu_ShouldOpenOnTheElementWithTheKey_WhenFoundByItsName(string browser)
    {
        var page = await OpenAsync(browser, "/context-menu");

        await page.Tb().ContextMenu("Trail actions").On("Lake path").ChooseAsync("Rename Lake path");
        await Expect(page.GetByTestId("chosen")).ToHaveTextAsync("Rename Lake path");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().ContextMenu("Trail actions").On("Nowhere").ChooseAsync("Rename"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextMenu_ShouldWaitForTheElementWithTheKey_WhenItRendersAfterTheMenuIsReady(string browser)
    {
        var page = await OpenAsync(browser, "/context-menu");
        await Expect(page.Locator(".tb-context-menu[data-part=target]").First).ToHaveAttributeAsync("data-ready", "");

        // A row that arrives a moment later, as one the app loads would.
        await page.EvaluateAsync("""
            () => setTimeout(() => {
                const late = document.createElement('button');
                late.dataset.contextKey = 'Late trail';
                late.textContent = 'Late trail';
                document.querySelector('[data-context-key]').after(late);
            }, 500)
            """);
        await page.Tb().ContextMenu("Trail actions").On("Late trail").ChooseAsync("Rename Late trail");

        await Expect(page.GetByTestId("chosen")).ToHaveTextAsync("Rename Late trail");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldFindTheOnlyComponent_WhenGivenNoName(string browser)
    {
        var drags = await OpenAsync(browser, "/drag-scroll");
        var builder = await OpenAsync(browser, "/query-builder");
        var viewport = await OpenAsync(browser, "/viewport");
        var single = await OpenAsync(browser, "/one-each");

        Assert.Equal("Rule 1", (await drags.Tb().Sortable().ItemsAsync())[0]);
        Assert.Equal(["Ridge loop", "Lake path"], await single.Tb().Tree().TextsAsync());
        await single.Tb().Grid().SortAsync("Trail");
        await Expect(single.Locator("tbody tr[data-part=row]").First).ToHaveTextAsync("Lake path");
        await builder.Tb().QueryBuilder().AddConditionAsync();
        Assert.Equal(1.0, await viewport.Tb().Viewport().ZoomAsync());
        Assert.Equal(2, await single.Tb().Pagination().PageAsync());
    }
}
