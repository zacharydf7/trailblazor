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
    public async Task Option_ShouldTellWhetherAnOptionIsDisabledOrChosen_WhenASelectOrListBoxReadsIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/select");
        var listBoxes = await OpenAsync(browser, "/examples/listbox");
        await page.Tb().Select("Shipping").ChooseAsync("Express (1 day)");

        Assert.Equal(new OptionInfo("Express (1 day)", Disabled: false, Chosen: true),
            await page.Tb().Select("Shipping").OptionAsync("Express (1 day)"));
        Assert.Equal(new OptionInfo("Overnight", Disabled: true, Chosen: false),
            await page.Tb().Select("Shipping").OptionAsync("Overnight"));
        Assert.Equal(new OptionInfo("Creek path", Disabled: false, Chosen: true),
            await listBoxes.Tb().ListBox("Trail").OptionAsync("Creek path"));
        Assert.Equal(new OptionInfo("Lakeshore", Disabled: true, Chosen: false),
            await listBoxes.Tb().ListBox("Trail").OptionAsync("Lakeshore"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Option_ShouldLeaveOutTheNativePlaceholder_WhenAStaticSelectReadsIt(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-select");

        Assert.Equal(new OptionInfo("Small", Disabled: false, Chosen: false),
            await page.Tb().Select("Size").OptionAsync("Small"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Select("Size").OptionAsync("Pick a size"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Disabled_ShouldBeTrueOnlyForTheDisabledField_WhenReadFromItsHelper(string browser)
    {
        var selects = await OpenAsync(browser, "/examples/select");
        var listBoxes = await OpenAsync(browser, "/examples/listbox");
        var trees = await OpenAsync(browser, "/tree-select-states");

        Assert.Equal((false, true), (await selects.Tb().Select("Region").DisabledAsync(),
            await selects.Tb().Select("Country").DisabledAsync()));
        Assert.Equal((false, true), (await listBoxes.Tb().ListBox("Trail").DisabledAsync(),
            await listBoxes.Tb().ListBox("Truck").DisabledAsync()));
        Assert.Equal((false, false, true), (await trees.Tb().TreeSelect("Plain trail").DisabledAsync(),
            await trees.Tb().TreeSelect("Fixed trail").DisabledAsync(),
            await trees.Tb().TreeSelect("Closed trail").DisabledAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OpenAsync_ShouldShowTheListUntilClosed_WhenASelectOrTreeSelectOpens(string browser)
    {
        var page = await OpenAsync(browser, "/examples/select");
        var trees = await OpenAsync(browser, "/tree-select-states");
        var select = page.Tb().Select("Shipping");
        var tree = trees.Tb().TreeSelect("Plain trail");

        await select.OpenAsync();
        await tree.OpenAsync();
        var open = (await select.IsOpenAsync(), await tree.IsOpenAsync());
        await select.CloseAsync();
        await tree.CloseAsync();

        Assert.Equal((true, true), open);
        Assert.Equal((false, false), (await select.IsOpenAsync(), await tree.IsOpenAsync()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Select("Country").OpenAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Select("Region").OpenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldFindAFieldAndTheTabsInOneListsPanel_WhenPanelsShareLabelsAndNestTabs(string browser)
    {
        var page = await OpenAsync(browser, "/tabs-panels");
        var trip = page.Tb().Tabs("Trip");
        await trip.SelectAsync("Route");

        await trip.Panel("Route").TextField("Notes").EnterAsync("Steep");
        var legs = await trip.Panel("Route").Tabs().TabsAsync();
        await trip.SelectAsync("Gear");
        await trip.Panel("Gear").TextField("Notes").EnterAsync("Tent");
        await trip.Panel("Gear").TextField("Stops").EnterAsync("2");

        await Expect(page.GetByTestId("notes")).ToHaveTextAsync("Tent|Steep|2|");
        Assert.Equal(["Gear", "South"], legs);
        Assert.Equal(["Gear", "Route"], await page.Tb().Tabs().TabsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldReadItsRowsAndSelection_WhenAsked(string browser)
    {
        var page = await OpenAsync(browser, "/grid-select");
        var boxes = page.Tb().Grid("Boxes only");

        await boxes.SelectAsync("Birch");

        Assert.Equal(6, await boxes.RowCountAsync());
        Assert.Equal([["Ash"], ["Birch"], ["Cedar"], ["Dogwood"], ["Elm"], ["Fir"]], await boxes.RowsAsync());
        Assert.Equal([["Birch"]], await boxes.SelectedAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldReadBusyUntilItsRowsLoad_WhenItReloads(string browser)
    {
        var page = await OpenAsync(browser, "/grid-reload");
        var trails = page.Tb().Grid("Trails");
        var reload = page.GetByRole(AriaRole.Button, new() { Name = "Reload" });
        await reload.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Table, new() { Name = "Trails", Exact = true }))
            .ToHaveAttributeAsync("aria-busy", "true");
        var busy = await trails.IsBusyAsync();

        await reload.ClickAsync();
        await trails.WaitForRowsAsync();

        Assert.True(busy);
        Assert.False(await trails.IsBusyAsync());
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
