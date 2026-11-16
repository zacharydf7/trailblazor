using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The Playwright reads and no-name lookups that match bUnit's helpers of the same names.
public class DriverReadsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Pressed_ShouldReadAToggleButton_WhenItsPressedOrNot(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var pin = page.Tb().Button("Pin to top");
        var before = await pin.PressedAsync();

        await pin.ClickAsync();

        Assert.Equal((false, true, false), (before, await pin.PressedAsync(), await pin.MixedAsync()));
        var icons = await OpenAsync(browser, "/examples/icon-button");
        var bold = icons.Tb().Button("Bold");
        Assert.Equal((false, true), (await bold.PressedAsync(), await bold.MixedAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Indeterminate_ShouldReadTheMixedState_WhenABoolCheckboxIsNullAndThenChecked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox");
        var formats = page.Tb().Checkbox("All formats");
        var mixed = await formats.IndeterminateAsync();

        await formats.CheckAsync();

        Assert.Equal((true, false), (mixed, await formats.IndeterminateAsync()));
    }

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
    public async Task TreeSelectOption_ShouldReadAnItemsState_WhenGivenItsPath(string browser)
    {
        var page = await OpenAsync(browser, "/tree-select-states");
        var picked = page.Tb().TreeSelect("Picked trail");
        var visited = page.Tb().TreeSelect("Visited trails");

        var pine = await picked.OptionAsync("North ridge", "Pine Hollow");
        var eagle = await picked.OptionAsync("North ridge", "Eagle Ridge");
        var checkedPine = await visited.OptionAsync("North ridge", "Pine Hollow");
        var uncheckedEagle = await visited.OptionAsync("North ridge", "Eagle Ridge");

        Assert.Equal(new OptionInfo("Pine Hollow", Disabled: false, Chosen: true), pine);
        Assert.Equal(new OptionInfo("Eagle Ridge", Disabled: true, Chosen: false), eagle);
        Assert.True(checkedPine.Chosen);
        Assert.False(uncheckedEagle.Chosen);
        Assert.False(await picked.IsOpenAsync());
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
    public async Task TreeSelect_ShouldReadAsOpen_WhenItsToggleEventComesLate(string browser)
    {
        var page = await OpenAsync(browser, "/tree-select-states");
        // The browser fires toggle a task after the panel shows; holding the listeners back widens that gap.
        await page.AddInitScriptAsync("""
            const add = EventTarget.prototype.addEventListener;
            EventTarget.prototype.addEventListener = function (type, listener, options) {
                const late = event => setTimeout(() => listener.call(this, event), 300);
                return add.call(this, type, type === 'toggle' ? late : listener, options);
            };
            """);
        await page.ReloadAsync();
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var tree = page.Tb().TreeSelect("Plain trail");

        await tree.OpenAsync();

        Assert.True(await tree.IsOpenAsync());
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
        await single.Tb().Toolbar().ClickAsync("Pin");
        Assert.Equal(("The ridge trail is closed today.", 0, "True"), (await single.Tb().Banner().TextAsync(),
            await single.Tb().Carousel().ActiveIndexAsync(), await single.GetByTestId("pinned").TextContentAsync()));
        Assert.Equal(["Shaded"], await single.Tb().ChipGroup().ChipsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PanelElement_ShouldHandBackATabsPanel_WhenFoundByItsTab(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");

        await Expect(page.Tb().Tabs("Report sections").PanelElement("Summary")).ToContainTextAsync("Revenue grew");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldReadAPopoversPanel_WhenItIsClosed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popover");

        var panel = page.Tb().Popover("Trip details").Panel;

        await Expect(panel).ToBeHiddenAsync();
        await Expect(panel).ToContainTextAsync("Three nights on the ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AlertsAsync_ShouldReadEveryAlertsMessageInOrder_WhenThePageShowsSeveral(string browser)
    {
        // The page shows every alert example, the tones first.
        var page = await OpenAsync(browser, "/examples/alert");
        var tones = Enum.GetValues<Tone>().Select(tone => $"{tone} alert.").ToList();

        var alerts = await page.Tb().AlertsAsync();

        Assert.Equal(tones, alerts.Take(tones.Count));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ValueAsync_ShouldReadTheColorAColorPickerHolds_WhenOneIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/color-picker");
        var marker = page.Tb().ColorPicker("Trail marker");
        var before = await marker.ValueAsync();

        await marker.ChooseAsync("#336699");

        Assert.Equal(("#1f6feb", "#336699"), (before, await marker.ValueAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FilesAsync_ShouldListTheChosenFilesNames_WhenFilesAreChosen(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        var documents = page.Tb().FileUpload("Documents");
        var before = await documents.FilesAsync();

        await documents.ChooseAsync(new FilePayload { Name = "route.pdf", MimeType = "application/pdf", Buffer = [1] },
            new FilePayload { Name = "permit.pdf", MimeType = "application/pdf", Buffer = [2] });

        Assert.Empty(before);
        Assert.Equal(["route.pdf", "permit.pdf"], await documents.FilesAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toasts_ShouldTakeAToastsActionAndDismissAnother_WhenTheHelperIsUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toast");
        await page.Tb().Button("Archive report").ClickAsync();
        await page.Tb().Button("Save report").ClickAsync();

        var wrong = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Toasts.ChooseAsync("Report archived", "Restore"));
        await page.Tb().Toasts.ChooseAsync("Report archived", "Undo");
        await page.Tb().Toasts.DismissAsync("Report saved");

        Assert.Equal("The toast showing \"Report archived\" has no action named \"Restore\".", wrong.Message);
        await Expect(page.GetByText("Restored")).ToBeVisibleAsync();
        await Expect(page.Locator("[data-part=toast]:not([data-leaving])")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldReadAndChangeTheOnlyDialogsWindowState_WhenNoTitleIsGiven(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var closed = await page.Tb().Dialog().IsOpenAsync();
        await page.Tb().Button("Edit beside the page").ClickAsync();
        var dialog = page.Tb().Dialog();
        var states = new List<DialogWindowState> { await dialog.WindowStateAsync() };

        foreach (var step in new Func<Task>[]
            { dialog.MinimizeAsync, dialog.MinimizeAsync, dialog.RestoreAsync, dialog.MaximizeAsync, dialog.RestoreAsync })
        {
            await step();
            states.Add(await dialog.WindowStateAsync());
        }

        Assert.Equal((false, true, "Edit name"), (closed, await dialog.IsOpenAsync(), await dialog.TitleAsync()));
        Assert.Equal([DialogWindowState.Normal, DialogWindowState.Minimized, DialogWindowState.Minimized,
            DialogWindowState.Normal, DialogWindowState.Maximized, DialogWindowState.Normal], states);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task WaitForClosedAsync_ShouldWaitUntilTheAppClosesTheDialog_WhenItClosesItLater(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-windows");
        await page.Tb().Button("Open front").ClickAsync();
        await page.Tb().Dialog("Front").WaitForAsync();

        // A plain click, since the button helper would wait out the app's handler itself.
        await page.GetByRole(AriaRole.Button, new() { Name = "Close front later", Exact = true }).ClickAsync();
        await page.Tb().Dialog("Front").WaitForClosedAsync();

        await Expect(page.Locator("dialog[open], dialog[data-leaving]")).ToHaveCountAsync(0, new() { Timeout = 1 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldFollowAPrerenderedPopoversPanel_WhenThePageTurnsInteractive(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/examples/popover");
        var panel = page.Tb().Popover("Trip details").Panel;
        await Expect(panel).ToContainTextAsync("Three nights on the ridge");

        // The interactive render replaces the panel, with a new id.
        held.SetResult();
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });

        await Expect(panel).ToContainTextAsync("Three nights on the ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldPassOverAPopoverAndRefuseToGuess_WhenNoTitleIsGiven(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-windows");
        await page.Tb().Button("Open back").ClickAsync();
        await page.Tb().Popover("Share").OpenAsync();

        var title = await page.Tb().Dialog().TitleAsync();
        // The back window covers the page's buttons, so the click goes to the button itself.
        await page.GetByRole(AriaRole.Button, new() { Name = "Open front" }).EvaluateAsync("button => button.click()");
        await page.Tb().Dialog("Front").WaitForAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Dialog().TitleAsync());

        Assert.Equal(("Back", "2 dialogs are open; give the title of the one to use."), (title, error.Message));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ValueAsync_ShouldReadTheColorAStaticColorPickerShows_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-color-picker");
        var marker = page.Tb().ColorPicker("Marker");
        var empty = await marker.ValueAsync();

        await marker.ChooseAsync("#d1242f");

        Assert.Equal(("#336699", null, "#d1242f"),
            (await page.Tb().ColorPicker("Accent").ValueAsync(), empty, await marker.ValueAsync()));
    }
}
