using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SelectTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseInTheGivenRowOrById_WhenSelectsShareTheirLabel(string browser)
    {
        var page = await OpenAsync(browser, "/repeated-fields");

        await page.Tb().Select(page.GetByTestId("row-1").GetByLabel("Size")).ChooseAsync("Large");
        await page.Tb().Select(page.Locator("#second-size")).ChooseAsync("Small");

        await Expect(page.GetByTestId("rows")).ToHaveTextAsync("large - | small -");
    }

    private static ILocator Combobox(IPage page, string name = "Size") =>
        page.GetByRole(AriaRole.Combobox, new() { Name = name });

    private static ILocator Option(IPage page, string name) =>
        page.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });

    // The keys work once the select script has attached, which is when it sets data-ready.
    private async Task<IPage> OpenSelectAsync(string browser)
    {
        var page = await OpenAsync(browser, "/select");
        await Expect(Combobox(page)).ToHaveAttributeAsync("data-ready", "");
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
    public async Task Listbox_ShouldOpenJustBelowTheField_WhenOpened(string browser)
    {
        var page = await OpenSelectAsync(browser);
        var listbox = page.GetByRole(AriaRole.Listbox);

        await Combobox(page).ClickAsync();
        await Expect(listbox).ToHaveAttributeAsync("data-side", "bottom");

        var control = page.Locator("[data-part=control]").Filter(new() { Has = Combobox(page) });
        var controlBox = await control.BoundingBoxAsync();
        var listBox = await listbox.BoundingBoxAsync();
        Assert.Equal(controlBox!.Y + controlBox.Height + 4, listBox!.Y, tolerance: 1);
        Assert.Equal(controlBox.X, listBox.X, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldStayAtLeastAsWideAsTheField_WhenTheFieldGrowsWhileItIsOpen(string browser)
    {
        var page = await OpenSelectAsync(browser);
        await Combobox(page).ClickAsync();
        var listbox = page.GetByRole(AriaRole.Listbox);
        await Expect(listbox).ToBeVisibleAsync();

        var width = await Combobox(page).EvaluateAsync<int>("""
            combobox => {
                const field = combobox.closest('.tb-field');
                field.style.width = `${field.offsetWidth + 200}px`;
                return combobox.parentElement.offsetWidth;
            }
            """);

        await Expect(listbox).ToHaveCSSAsync("min-width", $"{width}px");
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
    public async Task Select_ShouldWarnOncePerListInTheConsole_WhenAnOptionHasNoTextOrName(string browser)
    {
        var page = await OpenAsync(browser, "/unnamed-option");

        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Badge" })).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Medal" })).ToHaveAttributeAsync("data-ready", "");

        Assert.Equal(2, await UnnamedWarningsAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldWarnOnlyOnceAnOptionWithNoName_WhenItArrivesAfterNamedOnes(string browser)
    {
        var page = await OpenAsync(browser, "/named-options");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Badge" })).ToHaveAttributeAsync("data-ready", "");
        Assert.Equal(0, await UnnamedWarningsAsync(page));

        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add a badge" });
        await page.RunAndWaitForConsoleMessageAsync(() => add.ClickAsync(),
            new() { Predicate = message => message.Text.Contains("Give the TbOption a Text.") });
        await add.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Option, new() { IncludeHidden = true })).ToHaveCountAsync(5);

        Assert.Equal(1, await UnnamedWarningsAsync(page));
    }

    private static async Task<int> UnnamedWarningsAsync(IPage page) => (await page.ConsoleMessagesAsync())
        .Count(message => message.Type == "warning" && message.Text.Contains("Give the TbOption a Text."));

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
    public async Task Select_ShouldPostTheChosenOption_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-select");

        await page.GetByLabel("Size").SelectOptionAsync("Medium");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Medium | none");
        await Expect(page.GetByLabel("Size")).ToHaveValueAsync("Medium");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiSelect_ShouldPostEveryChosenOption_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-select");

        await page.GetByLabel("Toppings").SelectOptionAsync(["Cheese", "Olives"]);
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("none | Cheese, Olives");
        await Expect(page.GetByLabel("Toppings")).ToHaveValuesAsync(["Cheese", "Olives"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiSelect_ShouldToggleOptionsAndStayOpen_WhenOptionsAreClicked(string browser)
    {
        var page = await OpenSelectAsync(browser);
        var toppings = Combobox(page, "Toppings");

        await toppings.ClickAsync();
        await Option(page, "Cheese").ClickAsync();
        await Option(page, "Olives").ClickAsync();
        await Expect(page.GetByTestId("toppings")).ToHaveTextAsync("Cheese, Olives");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeVisibleAsync();
        await Option(page, "Cheese").ClickAsync();

        await Expect(page.GetByTestId("toppings")).ToHaveTextAsync("Olives");
        await Expect(toppings).ToContainTextAsync("Olives");
        await Expect(toppings).ToBeFocusedAsync();
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiSelect_ShouldHighlightChosenOptions_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/select", new() { ForcedColors = ForcedColors.Active });
        var toppings = Combobox(page, "Toppings");
        await Expect(toppings).ToHaveAttributeAsync("data-ready", "");
        await toppings.ClickAsync();
        await Option(page, "Cheese").ClickAsync();
        await Expect(Option(page, "Cheese")).ToHaveAttributeAsync("aria-selected", "true");

        // Forced colors still give the box a fill, the page's own background color, so only Highlight stands out.
        var highlight = await SystemColorAsync(page, "Highlight");
        var chosen = await Option(page, "Cheese").Locator(".tb-select__box")
            .EvaluateAsync<string>("box => getComputedStyle(box).backgroundColor");

        Assert.Equal(highlight, chosen);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldChooseOptionsByName_WhenGivenAnInteractiveSelectsLabel(string browser)
    {
        var page = await OpenAsync(browser, "/select");

        await page.Tb().Select("Size").ChooseAsync("Medium");
        await page.Tb().Select("Toppings").ChooseAsync("Ham", "Olives");
        await page.Tb().Select("Toppings").ChooseAsync("Ham");

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Medium");
        await Expect(page.GetByTestId("toppings")).ToHaveTextAsync("Ham, Olives");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldChooseTheNativeWay_WhenTheSelectIsRenderedStatically(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-select");

        await page.Tb().Select("Size").ChooseAsync("Small");
        await page.Tb().Select("Toppings").ChooseAsync("Cheese");
        await page.Tb().Select("Toppings").ChooseAsync("Ham");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Small | Cheese, Ham");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldWaitForTheCombobox_WhenCalledWhileThePageIsStillPrerendered(string browser)
    {
        var page = await OpenStaticAsync(browser, "/select");

        await page.Tb().Select("Size").ChooseAsync("Medium");

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Medium");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldThrow_WhenGivenNoOptionOrSeveralForASingleSelect(string browser)
    {
        var page = await OpenAsync(browser, "/select");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => page.Tb().Select("Size").ChooseAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => page.Tb().Select("Size").ChooseAsync("Small", "Medium"));
    }
}
