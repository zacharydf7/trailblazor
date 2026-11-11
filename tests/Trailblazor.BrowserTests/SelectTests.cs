using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SelectTests(BrowserFixture fixture) : SelectKeysTests(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldChooseThroughTheTestingHelper_WhenItAttachesOnFirstUse(string browser)
    {
        var page = await OpenAsync(browser, "/select-on-use");

        await page.Tb().Select("Trail").ChooseAsync("Eagle Ridge");

        await Expect(page.GetByTestId("trail")).ToHaveTextAsync("Eagle Ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldOpenItsList_WhenClickedBesideTheComboboxBeforeItsFirstUse(string browser)
    {
        var page = await OpenAsync(browser, "/select-on-use");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" });
        await Expect(trail).ToHaveAttributeAsync("data-attach-on-use", "");

        // A click on the field's edge, outside the combobox, neither focuses it nor passes over it.
        await trail.Locator("xpath=..").ClickAsync(new() { Position = new() { X = 2, Y = 2 } });

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Trail" })).ToBeVisibleAsync();
        await Expect(trail).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldFocusTheCombobox_WhenItsLabelIsClickedBeforeItsFirstUse(string browser)
    {
        var (page, _) = await OpenWithAScriptHeldAsync(browser, "/select-on-use", "**/select.*js");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" });
        await Expect(trail).ToHaveAttributeAsync("data-attach-on-use", "");

        await page.Locator("label", new() { HasText = "Trail" }).ClickAsync();

        await Expect(trail).ToBeFocusedAsync();
        await Expect(trail).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldOpenItsList_WhenAKeyIsPressedBeforeItsScriptHasLoaded(string browser)
    {
        var (page, script) = await OpenWithAScriptHeldAsync(browser, "/select-on-use", "**/select.*js");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" });
        await Expect(trail).ToHaveAttributeAsync("data-attach-on-use", "");

        await trail.FocusAsync();
        await trail.PressAsync("ArrowDown");
        script.SetResult();

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Trail" })).ToBeVisibleAsync();
        await Expect(trail).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldOpenItsList_WhenClickedBeforeItsScriptHasLoaded(string browser)
    {
        var (page, script) = await OpenWithAScriptHeldAsync(browser, "/examples/select", "**/select.*js");
        var shipping = page.GetByRole(AriaRole.Combobox, new() { Name = "Shipping" });

        // A click on the field's edge, outside the combobox, takes no focus, so the focus that follows is the script's.
        await shipping.Locator("xpath=..").ClickAsync(new() { Position = new() { X = 2, Y = 2 } });
        await Expect(shipping).Not.ToHaveAttributeAsync("data-ready", "");
        script.SetResult();

        await Expect(shipping).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Shipping" })).ToBeVisibleAsync();
        await Expect(shipping).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldStayClosed_WhenClickedTwiceBeforeItsScriptHasLoaded(string browser)
    {
        var (page, script) = await OpenWithAScriptHeldAsync(browser, "/examples/select", "**/select.*js");
        var shipping = page.GetByRole(AriaRole.Combobox, new() { Name = "Shipping" });

        await shipping.ClickAsync();
        await shipping.ClickAsync();
        script.SetResult();

        await Expect(shipping).ToHaveAttributeAsync("data-ready", "");
        await Expect(shipping).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Shipping" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseInTheGivenRowOrById_WhenSelectsShareTheirLabel(string browser)
    {
        var page = await OpenAsync(browser, "/repeated-fields");

        await page.Tb().Select(page.GetByTestId("row-1").GetByLabel("Size")).ChooseAsync("Large");
        await page.Tb().Select(page.Locator("#second-size")).ChooseAsync("Small");

        await Expect(page.GetByTestId("rows")).ToHaveTextAsync("large - | small -");
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
    public async Task Select_ShouldPostTheChosenOption_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-select");

        await page.GetByLabel("Size").SelectOptionAsync("Medium");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Medium | none");
        await Expect(page.GetByLabel("Size")).ToHaveValueAsync("Medium");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldShowItsEmptyMessageInMutedText_WhenRenderedStaticallyWithNoOptions(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-select");
        var status = page.GetByLabel("Saved routes").Locator("option[data-part=status]");

        await Expect(status).ToHaveTextAsync("Nothing to choose from");
        Assert.Equal(await MutedColorAsync(page),
            await status.EvaluateAsync<string>("status => getComputedStyle(status).color"));
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

    private static async Task<int> UnnamedWarningsAsync(IPage page) => (await page.ConsoleMessagesAsync())
        .Count(message => message.Type == "warning" && message.Text.Contains("Give the TbOption a Text."));
}
