namespace Trailblazor.BrowserTests;

public class ReadOnlyTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Each field and the boxes in it that take text.
    private static readonly (string Field, string Input)[] Boxes =
    [
        ("name", "[data-part=input]"), ("copies", "[data-part=input]"), ("town", "[data-part=input]"),
        ("color", "[data-part=input]"), ("dates", "[data-part=start-input]"), ("dates", "[data-part=end-input]"),
    ];

    public static TheoryData<string, string, string> BoxesInEachBrowser => new(
        from browser in AllBrowsers.Names
        from box in Boxes
        select (browser, box.Field, box.Input));

    [Theory, MemberData(nameof(BoxesInEachBrowser))]
    public async Task ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt(string browser,
        string field, string box)
    {
        var page = await OpenAsync(browser, "/read-only");
        var input = page.GetByTestId(field).Locator(box);
        var value = await input.InputValueAsync();

        await input.FocusAsync();
        await Expect(input).ToBeFocusedAsync();
        await input.PressAsync("End");
        await input.PressAsync("ArrowUp");
        await input.PressSequentiallyAsync("9");
        await input.PressAsync("Tab");

        await Expect(input).ToHaveValueAsync(value);
        await Expect(input).ToHaveAttributeAsync("readonly", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadOnlyAutocomplete_ShouldKeepItsListClosed_WhenTheKeysWouldOpenIt(string browser)
    {
        var page = await OpenAsync(browser, "/read-only");
        var town = page.GetByRole(AriaRole.Combobox, new() { Name = "Town" });
        await Expect(town).ToHaveAttributeAsync("data-ready", "");

        await town.PressAsync("ArrowDown");

        await Expect(town).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadOnlyPickers_ShouldTurnOffTheirButtonsAndHideClear_WhenReadOnly(string browser)
    {
        var page = await OpenAsync(browser, "/read-only");

        foreach (var field in new[] { "color", "dates" })
        {
            foreach (var button in await page.GetByTestId(field).GetByRole(AriaRole.Button).AllAsync())
            {
                await Expect(button).ToBeDisabledAsync();
            }
        }

        await Expect(page.Locator("[data-part=clear]")).ToHaveCountAsync(0);
    }

    public static TheoryData<string, string> PagesInEachBrowser => new(
        from browser in AllBrowsers.Names
        from path in new[] { "/read-only", "/static-read-only" }
        select (browser, path));

    [Theory, MemberData(nameof(PagesInEachBrowser))]
    public async Task ReadOnlyCheckbox_ShouldStayChecked_WhenClickedOrToggledWithSpace(string browser, string path)
    {
        var page = await OpenReadOnlyPageAsync(browser, path);
        var agree = page.GetByRole(AriaRole.Checkbox, new() { Name = "Agree" });

        await agree.ClickAsync();
        await Expect(agree).ToBeCheckedAsync();
        await agree.PressAsync("Space");

        await Expect(agree).ToBeCheckedAsync();
        await Expect(agree).ToBeFocusedAsync();
    }

    [Theory, MemberData(nameof(PagesInEachBrowser))]
    public async Task ReadOnlySlider_ShouldKeepItsValue_WhenDraggedOrMovedWithTheKeys(string browser, string path)
    {
        var page = await OpenReadOnlyPageAsync(browser, path);
        var volume = page.GetByRole(AriaRole.Slider, new() { Name = "Volume" });
        var box = (await volume.BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + box.Width - 2, box.Y + box.Height / 2);
        await volume.FocusAsync();
        await volume.PressAsync("ArrowRight");
        await volume.PressAsync("End");

        await Expect(volume).ToHaveValueAsync("40");
    }

    // On the static page nothing on the server holds the value, so only the pointer rules keep the thumb still.
    [Theory, MemberData(nameof(PagesInEachBrowser))]
    public async Task ReadOnlyRangeSlider_ShouldKeepItsThumb_WhenDragged(string browser, string path)
    {
        var page = await OpenReadOnlyPageAsync(browser, path);
        var start = page.Locator("[data-part=start-thumb]");
        var track = (await start.BoundingBoxAsync())!;
        var middle = track.Y + track.Height / 2;

        await page.Mouse.MoveAsync(track.X + track.Width * 0.2f, middle);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(track.X + track.Width * 0.5f, middle, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(start).ToHaveValueAsync("20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadOnlySwitchAndRangeSlider_ShouldKeepTheirValues_WhenTheUserTriesToChangeThem(string browser)
    {
        var page = await OpenAsync(browser, "/read-only");
        var alerts = page.GetByRole(AriaRole.Switch, new() { Name = "Alerts" });
        var start = page.Locator("[data-part=start-thumb]");

        await alerts.ClickAsync();
        await start.FocusAsync();
        await start.PressAsync("ArrowRight");
        await page.Locator("[data-part=end-thumb]").PressAsync("Home");

        await Expect(alerts).Not.ToBeCheckedAsync();
        await Expect(start).ToHaveValueAsync("20");
        await Expect(page.Locator("[data-part=end-thumb]")).ToHaveValueAsync("60");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadOnlyRadioGroup_ShouldKeepItsChoice_WhenTheUserTriesAnother(string browser)
    {
        var page = await OpenAsync(browser, "/read-only");
        var fast = page.GetByRole(AriaRole.Radio, new() { Name = "Fast" });

        await fast.FocusAsync();
        await fast.PressAsync("ArrowUp");
        await Expect(fast).ToBeFocusedAsync();
        await page.GetByText("Slow").ClickAsync(new() { Force = true });

        await Expect(fast).ToBeCheckedAsync();
        await Expect(page.GetByRole(AriaRole.Radiogroup, new() { Name = "Pace" }))
            .ToHaveAttributeAsync("aria-readonly", "true");
    }

    // The startup script holds these fields still, so a test waits for it: through interactivity, or on the static
    // page until its scripts have loaded.
    private async Task<IPage> OpenReadOnlyPageAsync(string browser, string path)
    {
        if (path == "/read-only")
        {
            return await OpenAsync(browser, path);
        }

        var page = await OpenStaticAsync(browser, path);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }
}
