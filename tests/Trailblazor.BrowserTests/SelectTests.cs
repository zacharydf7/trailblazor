namespace Trailblazor.BrowserTests;

public class SelectTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Combobox(IPage page, string name = "Size") =>
        page.GetByRole(AriaRole.Combobox, new() { Name = name });

    private static ILocator Option(IPage page, string name) =>
        page.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });

    // The keys work once the select script has attached, which is when it sets aria-expanded.
    private async Task<IPage> OpenSelectAsync(string browser)
    {
        var page = await OpenAsync(browser, "/select");
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-expanded", "false");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldChooseAClickedOptionAndRefocusTheCombobox_WhenOpenedByClicking(string browser)
    {
        var page = await OpenSelectAsync(browser);

        await Combobox(page).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-expanded", "true");
        await Option(page, "Medium").ClickAsync();

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Medium");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(Combobox(page)).ToBeFocusedAsync();
        await Expect(Combobox(page)).ToContainTextAsync("Medium");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldMoveSkippingDisabledOptionsAndChooseWithEnter_WhenUsingTheKeyboard(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Small").GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Extra large").GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Extra large");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(Combobox(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldJumpToTheEndsAndChooseWithAltUp_WhenHomeOrEndIsPressed(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).FocusAsync();

        await page.Keyboard.PressAsync("End");
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Slim").GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("Home");
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Small").GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("Alt+ArrowUp");

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Small");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldFocusTheComboboxAndFollowThePointer_WhenTheLabelIsClickedAndAnOptionHovered(
        string browser)
    {
        var page = await OpenSelectAsync(browser);

        await page.GetByText("Size", new() { Exact = true }).ClickAsync();
        await Expect(Combobox(page)).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Option(page, "Medium").HoverAsync();

        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Medium").GetAttributeAsync("id"))!);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldOpenOnTheMatchingOption_WhenALetterIsTypedWhileClosed(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).FocusAsync();

        await page.Keyboard.TypeAsync("me");

        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Medium").GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Medium");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldFindTheFirstMatch_WhenALetterIsTypedWithNothingChosen(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).FocusAsync();

        await page.Keyboard.TypeAsync("s");

        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Small").GetAttributeAsync("id"))!);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldTreatSpaceAsPartOfTheText_WhenTypedMidWord(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).FocusAsync();

        await page.Keyboard.TypeAsync("extra s");

        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-activedescendant",
            (await Option(page, "Extra small").GetAttributeAsync("id"))!);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldCloseWithoutChoosing_WhenEscapeIsPressedOrTheComboboxIsClickedAgain(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).ClickAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();

        await Combobox(page).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Combobox(page).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(page.GetByTestId("size")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldKeepTheActiveOptionAndMoveOn_WhenTabIsPressedWhileOpen(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Medium");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(Combobox(page)).Not.ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldStayOpenAndUnchanged_WhenADisabledOptionIsClicked(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).ClickAsync();

        // Force (needed for aria-disabled) skips waiting for the list to settle, so hovering waits for it first.
        await Option(page, "Large").HoverAsync();
        await Option(page, "Large").ClickAsync(new() { Force = true });
        await page.WaitForTimeoutAsync(300);

        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Expect(page.GetByTestId("size")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldClearTheChoice_WhenTheClearButtonIsClicked(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).ClickAsync();
        await Option(page, "Small").ClickAsync();
        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Small");

        await page.GetByRole(AriaRole.Button, new() { Name = "Clear" }).ClickAsync();

        await Expect(page.GetByTestId("size")).ToBeEmptyAsync();
        await Expect(Combobox(page)).ToContainTextAsync("Pick a size");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(Combobox(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldCloseOnlyItsList_WhenEscapeIsPressedInsideADialog(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Choose a color" });
        var color = Combobox(page, "Color");
        await Expect(color).ToHaveAttributeAsync("aria-expanded", "false");
        await color.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(dialog).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldPostTheChosenOption_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-select");

        await page.GetByLabel("Size").SelectOptionAsync("Medium");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Medium");
        await Expect(page.GetByLabel("Size")).ToHaveValueAsync("Medium");
    }
}
