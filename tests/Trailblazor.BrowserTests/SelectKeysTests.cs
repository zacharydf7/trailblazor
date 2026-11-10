namespace Trailblazor.BrowserTests;

// Keyboard tests and shared helpers for SelectTests; the slow-link class reruns only these.
public abstract class SelectKeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected static ILocator Combobox(IPage page, string name = "Size") =>
        page.GetByRole(AriaRole.Combobox, new() { Name = name });

    protected static ILocator Option(IPage page, string name) =>
        page.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });

    // The keys work once the select script has attached, which is when it sets data-ready.
    protected async Task<IPage> OpenSelectAsync(string browser)
    {
        var page = await OpenAsync(browser, "/select");
        await Expect(Combobox(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldStayClosed_WhenFocusMovesOnBeforeItsScriptHasLoaded(string browser)
    {
        var (page, script) = await OpenWithAScriptHeldAsync(browser, "/examples/select", "**/select.*js");
        var shipping = page.GetByRole(AriaRole.Combobox, new() { Name = "Shipping" });

        await shipping.ClickAsync();
        await page.Keyboard.PressAsync("Tab");
        var moved = await page.EvaluateAsync<string>("() => document.activeElement.id");
        script.SetResult();

        await Expect(shipping).ToHaveAttributeAsync("data-ready", "");
        await Expect(shipping).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.Equal(moved, await page.EvaluateAsync<string>("() => document.activeElement.id"));
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
        // A stopped clock keeps a slow machine's keys within the pause that ends a word.
        await page.Clock.SetFixedTimeAsync(DateTime.Now);

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
    public async Task Select_ShouldCloseOnlyItsList_WhenEscapeIsPressedInsideADialog(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Choose a color" });
        var color = Combobox(page, "Color");
        await Expect(color).ToHaveAttributeAsync("data-ready", "");
        await color.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(dialog).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldOpenFromCodeAndReportTheUserClosingIt_WhenOpenIsBound(string browser)
    {
        var page = await OpenSelectAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Show sizes" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-expanded", "true");
        await Combobox(page).FocusAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(page.GetByTestId("sizes-open")).ToHaveTextAsync("closed");
        await Combobox(page).ClickAsync();
        await Expect(page.GetByTestId("sizes-open")).ToHaveTextAsync("open");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiSelect_ShouldToggleWithSpaceAndEnterAndCloseWithoutToggling_WhenTabOrAltUpIsPressed(
        string browser)
    {
        var page = await OpenSelectAsync(browser);
        var toppings = Combobox(page, "Toppings");
        await Expect(toppings).ToHaveAttributeAsync("data-ready", "");
        await toppings.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync(" ");
        await Expect(page.GetByTestId("toppings")).ToHaveTextAsync("Cheese");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("toppings")).ToHaveTextAsync("Cheese, Ham");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(page.GetByTestId("toppings")).ToHaveTextAsync("Cheese, Ham");
        await toppings.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
        await Expect(page.GetByTestId("toppings")).ToHaveTextAsync("Cheese, Ham");
    }
}
