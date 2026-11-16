using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Over a slow link the app hears each action late, so a helper that returns before the app has answered leaves the
// page as it was. Each test reads the page once, without retrying, right after the helper returns.
public class HelperWaitTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(TimeSpan.FromMilliseconds(400));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Choices_ShouldReturnOnceTheAppHasTheValue_WhenCheckedOrChosen(string browser)
    {
        var boxes = await OpenAsync(browser, "/examples/checkbox");
        var groups = await OpenAsync(browser, "/examples/checkbox-group");
        var radios = await OpenAsync(browser, "/examples/radio-group");

        await boxes.Tb().Checkbox("Email me updates").CheckAsync();
        await groups.Tb().CheckboxGroup("Trail days").CheckAsync("Monday");
        await radios.Tb().RadioGroup("Paper size").ChooseAsync("A4");

        Assert.Equal(1, await boxes.GetByText("Updates value: True", new() { Exact = true }).CountAsync());
        Assert.Equal(1,
            await groups.GetByText("Trail days value: Saturday, Monday", new() { Exact = true }).CountAsync());
        Assert.Equal(1, await radios.GetByText("Paper size value: A4", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Form_ShouldReturnOnceTheFormHasHandledIt_WhenSubmitted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/form");

        await page.Tb().Form("Trip sign-up").SubmitAsync();

        Assert.Equal(3, (await page.Tb().Form("Trip sign-up").ErrorsAsync()).Count);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldReturnOnceTheSlideShows_WhenMoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");

        await page.Tb().Carousel("Featured trails").NextAsync();

        Assert.Equal(1, await page.Tb().Carousel("Featured trails").ActiveIndexAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chip_ShouldReturnOnceTheAppHasTheToggle_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip");

        await page.Tb().ChipGroup("Filter by status").Chip("Queued").ClickAsync();

        Assert.True(await page.Tb().ChipGroup("Filter by status").Chip("Queued").SelectedAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stepper_ShouldReturnOnceTheStepIsCurrent_WhenChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/stepper");
        await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).First.ClickAsync();
        await Expect(page.Tb().Stepper("Trip booking").Stepper.Locator("[aria-current=step]"))
            .ToContainTextAsync("Gear");

        await page.Tb().Stepper("Trip booking").GoToAsync("Trip");

        Assert.Equal("Trip", await page.Tb().Stepper("Trip booking").CurrentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasTheValue_WhenEntered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");

        await page.Tb().NumberField("Quantity").EnterAsync("7");

        Assert.Equal(1, await page.GetByText("Quantity value: 7", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasEachStepAndStopAtItsLimit_WhenStepped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var quantity = page.Tb().NumberField("Quantity");

        await quantity.StepAsync(3);
        var stepped = await page.GetByText("Quantity value: 4", new() { Exact = true }).CountAsync();
        await quantity.StepAsync(-10);

        Assert.Equal(1, stepped);
        Assert.Equal(1, await page.GetByText("Quantity value: 0", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldThrowOnStepping_WhenTheFieldIsReadOnlyOrHidesItsButtons(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");

        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().NumberField("Permit limit").StepAsync(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().NumberField("Year").StepAsync(1));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DatePickers_ShouldReturnOnceTheFieldShowsItsError_WhenTextItCantReadIsEntered(string browser)
    {
        var dates = await OpenAsync(browser, "/date-picker");
        var times = await OpenAsync(browser, "/examples/date-time-picker");
        var ranges = await OpenAsync(browser, "/examples/date-range-picker");

        await dates.Tb().DatePicker("Start date").EnterAsync("not a date");
        await times.Tb().DateTimePicker("Guided hike starts").EnterAsync("soon");
        await ranges.Tb().DateRangePicker("Trip dates").EnterAsync(null, "someday");

        Assert.True(await dates.Tb().DatePicker("Start date").InvalidAsync());
        Assert.True(await times.Tb().DateTimePicker("Guided hike starts").InvalidAsync());
        Assert.True(await ranges.Tb().DateRangePicker("Trip dates").InvalidAsync());
        await Expect(times.Tb().DateTimePicker("Guided hike starts").DateInput).ToHaveValueAsync("soon");
        await Expect(ranges.GetByRole(AriaRole.Textbox, new() { Name = "Trip dates End date" }))
            .ToHaveValueAsync("someday");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DatePickers_ShouldThrowOnEntering_WhenTheFieldIsStaticReadOnlyOrDisabled(string browser)
    {
        var statics = await OpenStaticAsync(browser, "/static-date-picker");
        var staticTimes = await OpenStaticAsync(browser, "/static-date-time-picker");
        var states = await OpenAsync(browser, "/examples/date-picker");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => statics.Tb().DatePicker("Start date").EnterAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => staticTimes.Tb().DateTimePicker("Starts").EnterAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => states.Tb().DatePicker("Season opens").EnterAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => states.Tb().DatePicker("Last season opened").EnterAsync("x"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasEachValue_WhenItUpdatesAsTextIsTyped(string browser)
    {
        var page = await OpenAsync(browser, "/number-live");
        var copies = page.Tb().NumberField("Copies");

        await copies.EnterAsync("7");
        await copies.EnterAsync("8");

        Assert.Equal("8", await page.GetByTestId("copies").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasTheValue_WhenTheAppReKeysTheField(string browser)
    {
        var page = await OpenAsync(browser, "/number-live");

        await page.Tb().NumberField("Trips").EnterAsync("7");

        Assert.Equal("7", await page.GetByTestId("trips").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldReturnOnceTheAppHasTheValue_WhenTheAppReKeysTheSelect(string browser)
    {
        var page = await OpenAsync(browser, "/choice-rekey");

        await page.Tb().Select("Trail").ChooseAsync("Ridge loop");

        Assert.Equal("Ridge loop", await page.GetByTestId("trail").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldReturnOnceTheAppHasTheValue_WhenTheAppReKeysTheSelectAndRenamesTheOption(
        string browser)
    {
        var page = await OpenAsync(browser, "/choice-rekey");

        await page.Tb().Select("Image").ChooseAsync("Summit");

        Assert.Equal("Summit", await page.GetByTestId("image").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiSelect_ShouldReturnOnceTheAppHasEachValue_WhenTheAppReKeysTheSelect(string browser)
    {
        var page = await OpenAsync(browser, "/choice-rekey");

        await page.Tb().Select("Days").ChooseAsync("Monday", "Wednesday");

        Assert.Equal("Monday, Wednesday", await page.GetByTestId("days").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldReturnOnceTheAppHasTheValue_WhenTheAppReKeysTheField(string browser)
    {
        var page = await OpenAsync(browser, "/choice-rekey");

        await page.Tb().Autocomplete("Trailhead").ChooseAsync("gate", "South gate");

        Assert.Equal("South gate", await page.GetByTestId("trailhead").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldReturnOnceTheSuggestionsAnswerTheText_WhenTypedWithoutChoosing(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");
        var lake = page.Tb().Autocomplete("Lake");

        await lake.TypeAsync("a");

        Assert.Equal(["Crater", "Sapphire"], await lake.SuggestionsAsync());
        Assert.Equal("a", await lake.TextAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldReturnOnceTheAppHasTheValue_WhenTextIsEnteredOrTheFieldCleared(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");

        await page.Tb().Autocomplete("Pack weight (kg)").EnterAsync("8");
        await page.Tb().Autocomplete("Lake").ClearAsync();

        Assert.Equal(1, await page.GetByText("Packing: 8 kg", new() { Exact = true }).CountAsync());
        Assert.Equal(1, await page.GetByText("Lake value: null", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldReturn_WhenTheEnteredTextIsTheFieldsOwnOrIsRefused(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");
        var lake = page.Tb().Autocomplete("Lake");

        await lake.EnterAsync("Mirror");
        await lake.EnterAsync("Nowhere");

        Assert.Equal("Mirror", await lake.TextAsync());
        Assert.Equal(1, await page.GetByText("Lake value: Mirror", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldReturnOnceTheAppHasTheValue_WhenTextIsEnteredAfterTyping(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");
        var lake = page.Tb().Autocomplete("Lake");

        // The field still holds focus from the typing, so leaving it compares with the text it had then.
        await lake.TypeAsync("Nowhere");
        await lake.EnterAsync("Mirror");
        await lake.TypeAsync("Crater");
        await lake.EnterAsync("Crater");

        Assert.Equal(1, await page.GetByText("Lake value: Crater", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldWaitForNewSuggestions_WhenTheSameTextIsTypedAfterAClear(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");
        var lake = page.Tb().Autocomplete("Lake");

        await lake.TypeAsync("a");
        await lake.ClearAsync();
        await lake.TypeAsync("a");

        Assert.Equal(["Crater", "Sapphire"], await lake.SuggestionsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldReturnOnceTheAppHasTheValue_WhenTheAppReKeysTheList(string browser)
    {
        var page = await OpenAsync(browser, "/choice-rekey");

        await page.Tb().ListBox("Camp").ChooseAsync("Pine camp");

        Assert.Equal("Pine camp", await page.GetByTestId("camp").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TransferList_ShouldReturnOnceTheAppHasTheItems_WhenTheAppReKeysTheList(string browser)
    {
        var page = await OpenAsync(browser, "/choice-rekey");

        await page.Tb().TransferList("Crew").MoveAsync("Ada", "Linus");

        Assert.Equal("Ada, Linus", await page.GetByTestId("crew").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Interactive_ShouldWaitForThePagesOwnMark_WhenEnhancedNavigationReachesAnInteractivePage(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted-plain");
        await page.Tb().InteractiveAsync();
        var trip = page.GetByRole(AriaRole.Button, new() { Name = "Add trip" });

        // A test waits for the page it navigated to, then for the page to turn interactive.
        await page.EvaluateAsync("() => Blazor.navigateTo('/per-page-trip')");
        await Expect(trip).ToBeVisibleAsync();
        await page.Tb().InteractiveAsync();
        var fresh = await trip.And(page.Locator("[data-clicks]")).CountAsync();
        // The same page again keeps its interactive part, and with it the part's mark.
        await page.EvaluateAsync("""
            () => new Promise(loaded => {
                Blazor.addEventListener('enhancedload', loaded, { once: true });
                Blazor.navigateTo('/per-page-trip?day=2');
            })
            """);
        await page.Tb().InteractiveAsync();
        var kept = await trip.And(page.Locator("[data-clicks]")).CountAsync();
        // Another page replaces the interactive part, and waits for the new part's own mark.
        var dialog = page.GetByRole(AriaRole.Button, new() { Name = "Dialog" });
        await page.EvaluateAsync("() => Blazor.navigateTo('/per-page')");
        await Expect(dialog).ToBeVisibleAsync();
        await page.Tb().InteractiveAsync();

        Assert.Equal((1, 1, 1), (fresh, kept, await dialog.And(page.Locator("[data-clicks]")).CountAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldReturnOnceTheAppHasTheAnswer_WhenAButtonIsChosenOrTheDialogIsCanceled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete report" });

        await delete.ClickAsync();
        await page.Tb().Dialog("Delete report?").ChooseAsync("Delete");
        Assert.Equal(1, await page.GetByText("Deleted", new() { Exact = true }).CountAsync());
        await delete.ClickAsync();
        await page.Tb().Dialog("Delete report?").CancelAsync();

        Assert.Equal(1, await page.GetByText("Kept", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenEntered(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");

        await page.Tb().RichTextEditor("Summary").EnterAsync("<p>Hi</p>");

        Assert.Equal("<p>Hi</p>", await page.GetByTestId("summary").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenTheEditorUpdatesAsTextIsTyped(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = page.Tb().RichTextEditor("Notes");

        await notes.EnterAsync("<p>Old</p>");
        await notes.EnterAsync("<p>New</p>");

        Assert.Equal("<p>New</p>", await page.GetByTestId("notes").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenTheAppTransformsThePaste(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");

        await page.Tb().RichTextEditor("Shouting").EnterAsync("<p>hey</p>");

        Assert.Equal("<p>HEY</p>", await page.GetByTestId("shouting").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenItsHandlerStoresItLate(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");

        await page.Tb().RichTextEditor("Live").EnterAsync("<p>Late</p>");

        Assert.Equal("<p>Late</p>", await page.GetByTestId("live").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldReturnOnceTheAppHasAnswered_WhenATagIsAddedRefusedOrRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tag-input");
        var features = page.Tb().TagInput("Trail features");

        await features.AddAsync("Views");
        Assert.Contains("Views", await features.Tags.AllTextContentsAsync(), StringComparer.Ordinal);
        await features.AddAsync("Views");
        Assert.Equal("true", await features.Input.GetAttributeAsync("aria-invalid"));
        await features.AddAsync(",");
        Assert.Null(await features.Input.GetAttributeAsync("aria-invalid"));
        await features.RemoveAsync("Views");

        Assert.DoesNotContain("Views", await features.Tags.AllTextContentsAsync(), StringComparer.Ordinal);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldReturnOnceTheAppHasTheTabs_WhenOneIsAddedOrClosed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        var open = page.GetByRole(AriaRole.Tablist, new() { Name = "Open trails" }).GetByRole(AriaRole.Tab);
        var before = await open.CountAsync();
        var days = page.GetByRole(AriaRole.Tablist, new() { Name = "Trip days" }).GetByRole(AriaRole.Tab);
        var count = await days.CountAsync();

        await page.Tb().Tabs("Open trails").CloseAsync("Cedar flats");
        Assert.Equal(before - 1, await open.CountAsync());
        await page.Tb().Tabs("Trip days").AddAsync();

        Assert.Equal(count + 1, await days.CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldReturnOnceTheAppHasTheAnswer_WhenConfirmed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popconfirm");

        await page.Tb().Popconfirm("Delete trail").ConfirmAsync();

        Assert.Equal("Deleted.", await page.Locator("output").First.TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldReturnOnceTheAppHasTheAnswer_WhenConfirmingRemovesItsTrigger(string browser)
    {
        var page = await OpenAsync(browser, "/popconfirm-remove");

        await page.Tb().Popconfirm("Delete Cedar Flats").ConfirmAsync();

        Assert.Equal(1, await page.Locator("li").CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toasts_ShouldReturnOnceTheActionHasRun_WhenTheActionOutlastsTheToast(string browser)
    {
        var page = await OpenAsync(browser, "/toasts");
        await page.Tb().Button("Remove slowly").ClickAsync();

        await page.Tb().Toasts.ChooseAsync("Removed slowly", "Undo");

        Assert.Equal("undone slowly", await page.GetByTestId("result").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldReturnOnceTheDialogOpens_WhenItsHandlerAwaitsTheDialogsAnswer(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");

        // The handler awaits the answer, so it finishes only after the test gives one.
        await page.Tb().Button("Edit beside the page").ClickAsync();
        await page.Tb().Dialog("Edit name").CancelAsync();

        Assert.False(await page.Tb().Dialog("Edit name").IsOpenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldWaitForItsHandler_WhenADialogWasOpenBeforeTheClick(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-windows");
        await page.Tb().Button("Open front").ClickAsync();
        await page.Tb().Dialog("Front").WaitForAsync();

        // Only a dialog that opens after the click ends the wait early; this handler closes the open one.
        await page.Tb().Button("Close front later").ClickAsync();

        Assert.False(await page.Tb().Dialog("Front").IsOpenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldReturn_WhenItsHandlerReloadsThePage(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-reload");
        await page.Tb().Button("Open sign out").ClickAsync();

        // The reload replaces the page while the helper waits, which ends the wait.
        await page.Tb().Button("Sign out").ClickAsync();

        // The handler never closes the dialog, so only the reload takes it away.
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldReturnOnceTheItemsHandlerHasRun_WhenItOutlastsTheMenu(string browser)
    {
        var page = await OpenAsync(browser, "/menu");

        await page.Tb().Menu("Trip").ChooseAsync("Archive slowly");
        var archived = await page.GetByTestId("trip").TextContentAsync();
        await page.Tb().Menu("Trip").ChooseAsync("Notify slowly");

        Assert.Equal(("archived", "notifying"), (archived, await page.GetByTestId("trip").TextContentAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextMenu_ShouldReturnOnceTheItemsHandlerHasRun_WhenItOutlastsTheMenu(string browser)
    {
        var page = await OpenAsync(browser, "/context-menu");

        await page.Tb().ContextMenu("Trail actions").On("Lake path").ChooseAsync("Slow");

        Assert.Equal("Slow Lake path", await page.GetByTestId("chosen").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldReturnOnceTheAppHasRunItsCallback_WhenEachHelperActs(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");
        var tree = page.Tb().Tree("Slow");
        var slow = page.GetByTestId("slow");

        // Each read follows its helper at once, with no wait of the test's own.
        await tree.ExpandAsync("North");
        var expanded = await slow.TextContentAsync();
        await tree.SelectAsync("North", "Pass");
        var selected = await slow.TextContentAsync();
        await tree.CheckAsync("South");
        var checks = await slow.TextContentAsync();
        await tree.ActivateAsync("North", "Pass");
        var activated = await slow.TextContentAsync();
        await tree.DragBeforeAsync(["South"], ["North"]);

        Assert.Equal(("expanded 1", "selected 6", "checked 4, 5", "activated Pass", "moved South"),
            (expanded, selected, checks, activated, await slow.TextContentAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grid_ShouldReturnOnceTheAppHasRunItsCallback_WhenEachHelperActs(string browser)
    {
        var page = await OpenAsync(browser, "/grid-slow");
        var grid = page.Tb().Grid("Slow trails");
        var slow = page.GetByTestId("slow");

        // Each read follows its helper at once, with no wait of the test's own.
        await grid.SelectAsync("Ash");
        var selected = await slow.TextContentAsync();
        // A row click raises OnRowClick, then selects the row.
        await grid.ClickRowAsync("Birch");
        var clicked = await slow.TextContentAsync();
        await grid.SortAsync("Name", descending: true);
        var sorted = await slow.TextContentAsync();
        await grid.GoToPageAsync(2);
        var paged = await slow.TextContentAsync();
        // Filtering goes back to the first page, which the app hears of last.
        await grid.FilterAsync("Name", "a");
        var filtered = await slow.TextContentAsync();
        await page.Tb().Grid("Slow folders").ExpandAsync("Maps");
        var expanded = await slow.TextContentAsync();
        await page.Tb().Pagination("Slow pages").GoToPageAsync(3);

        Assert.Equal(("selected Ash", "selected Ash, Birch", "sorted name Descending", "grid page 2", "grid page 1",
            "expanded Maps", "page 3"), (selected, clicked, sorted, paged, filtered, expanded,
            await slow.TextContentAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldReturnOnceTheAppHasRunItsCallback_WhenTheirButtonOrFieldIsSlow(string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");
        var tb = page.Tb();
        var slow = page.GetByTestId("slow");
        var seen = new List<string?>();

        // Each read follows its helper at once, with no wait of the test's own.
        foreach (var act in new Func<Task>[]
        {
            () => tb.Alert("Trail closed for repairs.").CloseAsync(),
            () => tb.InsertDivider("Add a stop").InsertAsync(),
            () => tb.Popconfirm("Remove trail").ConfirmAsync(),
            () => tb.Popconfirm("Remove trail").CancelAsync(),
            () => tb.Viewport("Slow map").ZoomInAsync(),
            () => tb.Toolbar("Slow tools").ClickAsync("Pin"),
            () => tb.ToggleGroup("Slow view").ChooseAsync("Terrain"),
            () => tb.Carousel("Slow slides").NextAsync(),
            () => tb.Select("Slow region").ClearAsync(),
            () => tb.ColorPicker("Slow marker").ChooseAsync("#aa0000"),
            () => tb.DatePicker("Slow day").ChooseAsync(new DateOnly(2026, 5, 2)),
            () => tb.DateRangePicker("Slow trip").ChooseAsync(new DateOnly(2026, 5, 2), new DateOnly(2026, 5, 4)),
            () => tb.Tabs("Slow tabs").SelectAsync("Notes"),
            () => tb.Chip("Open").ClickAsync(),
            () => tb.Chip("Shaded").ClickAsync(),
            () => tb.Chip("Muddy").RemoveAsync(),
            () => tb.List("Slow campsites").ClickAsync("Lakeside"),
            () => tb.AccordionItem("Slow route").ExpandAsync(),
            () => tb.Stepper("Slow booking").GoToAsync("Gear"),
            () => tb.Carousel("Slow slides").GoToAsync(0),
            () => tb.Calendar("Slow hike").ChooseAsync(new DateOnly(2026, 5, 2)),
        })
        {
            await act();
            seen.Add(await slow.TextContentAsync());
        }

        Assert.Equal(["alert closed", "inserted", "removed", "kept", "zoom 1.25", "pinned", "view Terrain", "slide 1",
            "region ", "color #aa0000", "day 2026-05-02 #1", "trip 2026-05-02/2026-05-04", "clicked notes", "open True",
            "shaded", "muddy removed", "camp Lakeside", "route True",
            "step gear", "slide 0", "hike 2026-05-02"], seen);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DatePicker_ShouldReturnOnceTheAppHasRunItsCallback_WhenTheSameDateReplacesTextItCouldNotRead(
        string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");
        var day = page.Tb().DatePicker("Slow day");
        await day.ChooseAsync(new DateOnly(2026, 5, 2));
        await day.EnterAsync("not a day");

        // The field still has the date, but the box's new text raises a change the app answers.
        await day.ChooseAsync(new DateOnly(2026, 5, 2));

        Assert.Equal("day 2026-05-02 #2", await page.GetByTestId("slow").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldClickOnceInteractiveAndWaitForTheApp_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/helpers-slow");

        var clicking = page.Tb().List("Slow campsites").ClickAsync("Lakeside");
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await clicking;

        Assert.Equal("camp Lakeside", await page.GetByTestId("slow").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NavMenu_ShouldReturnOnceTheGroupsExpandedChangedHasRun_WhenItOpensAGroup(string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");
        var settings = page.GetByTestId("settings");
        // The link is to this page, so the group opens as the page loads; it's closed here for the helper to open.
        await Expect(settings).ToHaveTextAsync("settings True");
        await page.GetByRole(AriaRole.Button, new() { Name = "Slow settings" }).ClickAsync();
        await Expect(settings).ToHaveTextAsync("settings False");

        await page.Tb().NavMenu("Slow navigation").FollowAsync("Slow settings", "Profile");

        Assert.Equal("settings True", await settings.TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NavMenu_ShouldReturnOnceTheGroupsExpandedChangedHasRun_WhenItCollapsesOrExpandsAGroup(
        string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");
        var menu = page.Tb().NavMenu("Slow navigation");
        var settings = page.GetByTestId("settings");
        // The link is to this page, so the group opens as the page loads.
        await Expect(settings).ToHaveTextAsync("settings True");

        await menu.CollapseAsync("Slow settings");
        var collapsed = (await settings.TextContentAsync(), await menu.ExpandedAsync("Slow settings"));
        await menu.ExpandAsync("Slow settings");

        Assert.Equal(("settings False", false), collapsed);
        Assert.Equal(("settings True", true), (await settings.TextContentAsync(),
            await menu.ExpandedAsync("Slow settings")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldReturnOnceThePanesCollapsedChangedHasRun_WhenItCollapsesOrRestoresAPane(
        string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");
        var outline = page.Tb().Splitter("Resize Slow outline");
        var slow = page.GetByTestId("slow");

        await outline.CollapseAsync();
        var collapsed = (await slow.TextContentAsync(), await outline.CollapsedAsync());
        // Collapsing it again changes nothing, so the app has nothing to answer.
        await outline.CollapseAsync();
        var again = await outline.CollapsedAsync();
        await outline.RestoreAsync();

        Assert.Equal(("outline True", true, true), (collapsed.Item1, collapsed.Item2, again));
        Assert.Equal(("outline False", false), (await slow.TextContentAsync(), await outline.CollapsedAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SignaturePad_ShouldReturnOnceTheAppHasRunItsCallback_WhenUndoOrClearIsSlow(string browser)
    {
        var page = await OpenAsync(browser, "/helpers-slow");
        var pad = page.Tb().SignaturePad("Slow signature");
        var slow = page.GetByTestId("slow");
        await pad.DrawAsync((50, 50), (300, 150));
        await pad.DrawAsync((50, 150), (300, 50));
        await Expect(slow).ToHaveTextAsync("strokes 2");

        await pad.UndoAsync();
        var undone = await slow.TextContentAsync();
        await pad.ClearAsync();

        Assert.Equal(("strokes 1", "strokes 0"), (undone, await slow.TextContentAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DateTimePicker_ShouldReturnOnceTheAppHasRunItsCallback_WhenItsHandlerIsSlow(string browser)
    {
        Assert.SkipWhen(browser == "webkit", "WebKit on Windows has no time box.");
        var page = await OpenAsync(browser, "/helpers-slow");

        await page.Tb().DateTimePicker("Slow start").ChooseAsync(new DateTime(2026, 5, 2, 9, 30, 0));
        var started = await page.GetByTestId("slow").TextContentAsync();
        // The box shows 09:30 for 09:30:15, so the same text still changes the time.
        await page.Tb().DateTimePicker("Slow finish").ChooseAsync(new DateTime(2026, 5, 2, 9, 30, 0));

        Assert.Equal(("start 2026-05-02T09:30", "finish 2026-05-02T09:30:00"),
            (started, await page.GetByTestId("slow").TextContentAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldReturnAtOnce_WhenTheRowPageStepOrSlideIsAlreadyAsAsked(string browser)
    {
        var page = await OpenAsync(browser, "/grid-slow");
        var grid = page.Tb().Grid("Slow trails");
        await grid.SelectAsync("Ash");
        var pages = await OpenAsync(browser, "/pagination");
        var steps = await OpenAsync(browser, "/helpers-slow");

        // None changes anything, so the app has nothing to answer, and a wait for it would time out and throw.
        await grid.SelectAsync("Ash");
        await pages.Tb().Pagination("In a form").GoToPageAsync(1);
        await steps.Tb().Stepper("Slow booking").GoToAsync("Dates");
        await steps.Tb().Carousel("Slow slides").GoToAsync(0);

        Assert.Equal(("1", "Dates", ""), (await pages.GetByTestId("box-page").TextContentAsync(),
            await steps.Tb().Stepper("Slow booking").CurrentAsync(), await steps.GetByTestId("slow").TextContentAsync()));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeDrag_ShouldReturnAtOnce_WhenTheDropLandsNowhere(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");

        // Without a drop to wait for, the helper's wait would time out and throw.
        await page.Tb().Tree("Slow").DragIntoAsync(["South"], ["South"]);

        Assert.Equal("", await page.GetByTestId("slow").TextContentAsync());
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Tree("Places").DragBeforeAsync(["South"], ["North"]));
        Assert.Contains("can't be dragged", refused.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeSelect_ShouldReturnOnceTheAppHasTheChoice_WhenItsHandlerOutlastsTheClose(string browser)
    {
        var page = await OpenAsync(browser, "/tree-select-states");

        await page.Tb().TreeSelect("Slow trail").ChooseAsync("North ridge", "Pine Hollow");

        Assert.Equal("pine", await page.GetByLabel("Slow", new() { Exact = true }).TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiTreeSelect_ShouldReturnOnceTheAppHasTheChecks_WhenItsHandlerOutlastsTheCheck(string browser)
    {
        var page = await OpenAsync(browser, "/tree-select-states");

        await page.Tb().TreeSelect("Slow visits").CheckAsync("North ridge", "Eagle Ridge");

        Assert.Equal("eagle", await page.GetByLabel("Slow checks", new() { Exact = true }).TextContentAsync());
    }
}
