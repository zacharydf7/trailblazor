using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ListBoxTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator ListBox(IPage page, string name) =>
        page.GetByRole(AriaRole.Listbox, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldWrapALongOption_WhenItIsWiderThanTheList(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox");
        var list = ListBox(page, "Permit number");

        var longOption = await list.GetByRole(AriaRole.Option, new() { Name = "A permit for groups" }).BoundingBoxAsync();
        var shortOption = await list.GetByRole(AriaRole.Option, new() { Name = "Permit 1", Exact = true }).BoundingBoxAsync();

        Assert.True(longOption!.Height > shortOption!.Height, $"The option is {longOption.Height}px tall.");
    }

    private async Task<IPage> OpenListBoxAsync(string browser, string name)
    {
        var page = await OpenAsync(browser, "/examples/listbox");
        await Expect(ListBox(page, name)).ToHaveAttributeAsync("data-ready", "");
        await ListBox(page, name).FocusAsync();
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldChooseTheOptionItMovesTo_WhenTheArrowKeysAreUsed(string browser)
    {
        var page = await OpenListBoxAsync(browser, "Trail");
        var trail = page.GetByText("Trail: ");

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(trail).ToHaveTextAsync("Trail: Summit spur");
        // Lakeshore is disabled, so it is skipped.
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(trail).ToHaveTextAsync("Trail: Old mill");
        await page.Keyboard.PressAsync("Home");
        await Expect(trail).ToHaveTextAsync("Trail: Ridge loop");
        await page.Keyboard.PressAsync("End");
        await Expect(trail).ToHaveTextAsync("Trail: Fern gully");
        await page.Keyboard.PressAsync("ArrowUp");

        await Expect(trail).ToHaveTextAsync("Trail: Old mill");
        await Expect(ListBox(page, "Trail")).ToHaveAttributeAsync("aria-activedescendant",
            (await page.GetByRole(AriaRole.Option, new() { Name = "Old mill" }).GetAttributeAsync("id"))!);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldJumpToTheOptionTyped_WhenLettersAreTyped(string browser)
    {
        var page = await OpenListBoxAsync(browser, "Trail");

        await page.Keyboard.TypeAsync("fe");

        await Expect(page.GetByText("Trail: ")).ToHaveTextAsync("Trail: Fern gully");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldScrollTheActiveOptionIntoView_WhenItIsBelowTheRowsShown(string browser)
    {
        var page = await OpenListBoxAsync(browser, "Trail");
        var fern = page.GetByRole(AriaRole.Option, new() { Name = "Fern gully" });

        Assert.True(await ListBox(page, "Trail").EvaluateAsync<bool>("list => list.scrollHeight > list.clientHeight"));
        await page.Keyboard.PressAsync("End");

        await Expect(fern).ToBeInViewportAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldToggleWithSpaceAndExtendWithShift_WhenTheKeysAreUsed(string browser)
    {
        var page = await OpenListBoxAsync(browser, "Gear to pack");
        var packing = page.GetByText("Packing: ");

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(packing).ToHaveTextAsync("Packing: Tent");
        await page.Keyboard.PressAsync("Space");
        await Expect(packing).ToHaveTextAsync("Packing: Tent, Stove");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(packing).ToHaveTextAsync("Packing: Tent, Stove, Water filter");
        await page.Keyboard.PressAsync("Home");
        await page.Keyboard.PressAsync("Space");

        await Expect(packing).ToHaveTextAsync("Packing: Stove, Water filter");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldDropTheActiveOptionsRing_WhenFocusLeaves(string browser)
    {
        var page = await OpenListBoxAsync(browser, "Trail");
        var active = page.GetByRole(AriaRole.Option, new() { Name = "Creek path" });
        await Expect(active).ToHaveAttributeAsync("data-active", "");

        await page.Keyboard.PressAsync("Tab");

        await Expect(ListBox(page, "Gear to pack")).ToBeFocusedAsync();
        await Expect(active).ToHaveCSSAsync("outline-style", "none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldTakeFocus_WhenItsLabelIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox");
        await Expect(ListBox(page, "Trail")).ToHaveAttributeAsync("data-ready", "");

        await page.GetByText("Trail", new() { Exact = true }).ClickAsync();

        await Expect(ListBox(page, "Trail")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldChooseTheOption_WhenItIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox");

        await page.GetByRole(AriaRole.Option, new() { Name = "Ridge loop" }).ClickAsync();

        await Expect(page.GetByText("Trail: ")).ToHaveTextAsync("Trail: Ridge loop");
        await Expect(ListBox(page, "Trail")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldPostTheChosenOptions_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-listbox");

        await page.GetByLabel("Trail").SelectOptionAsync("Creek path");
        await page.GetByLabel("Gear").SelectOptionAsync(["Tent", "Headlamp"]);
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Creek path | Tent, Headlamp");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldShowItsEmptyMessageInMutedText_WhenRenderedStaticallyWithNoOptions(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-listbox");
        var status = page.GetByLabel("Saved trips").Locator("option[data-part=status]");

        await Expect(status).ToHaveTextAsync("Nothing to choose from");
        Assert.Equal(await MutedColorAsync(page),
            await status.EvaluateAsync<string>("status => getComputedStyle(status).color"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldChooseOptionsByName_WhenGivenAnInteractiveListboxsLabel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox");

        await page.Tb().ListBox("Trail").ChooseAsync("Old mill");
        await page.Tb().ListBox("Gear to pack").ChooseAsync("Stove", "Headlamp");
        await page.Tb().ListBox("Gear to pack").ChooseAsync("Tent");

        await Expect(page.GetByText("Trail: ")).ToHaveTextAsync("Trail: Old mill");
        await Expect(page.GetByText("Packing: ")).ToHaveTextAsync("Packing: Tent, Stove, Headlamp");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldChooseTheNativeWay_WhenTheListboxIsRenderedStatically(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-listbox");

        await page.Tb().ListBox("Trail").ChooseAsync("Ridge loop");
        await page.Tb().ListBox("Gear").ChooseAsync("Stove");
        await page.Tb().ListBox("Gear").ChooseAsync("Tent");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Ridge loop | Tent, Stove");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldThrow_WhenGivenSeveralOptionsForASingleListbox(string browser)
    {
        var page = await OpenAsync(browser, "/examples/listbox");

        await Assert.ThrowsAsync<ArgumentException>(
            () => page.Tb().ListBox("Trail").ChooseAsync("Old mill", "Ridge loop"));
    }
}
