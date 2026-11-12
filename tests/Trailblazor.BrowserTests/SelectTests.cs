using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SelectTests(BrowserFixture fixture) : SelectKeysTests(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldPutItsChevronAtTheLeft_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/select?dir=rtl");
        var field = page.Locator(".tb-field").Filter(new() { Has = page.GetByText("Size", new() { Exact = true }) });

        var chevron = (await field.Locator("[data-part=chevron]").BoundingBoxAsync())!;
        var box = (await field.Locator("[role=combobox][data-part=input]").BoundingBoxAsync())!;

        Assert.True(chevron.X + chevron.Width / 2 < box.X + box.Width / 2,
            $"The chevron's center {chevron.X + chevron.Width / 2} should be left of the box's {box.X + box.Width / 2}.");
    }

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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldReachAndChooseItsItemsByKey_WhenItsOptionsComeFromItems(string browser)
    {
        var page = await OpenAsync(browser, "/select-items");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Trail", Exact = true });

        await trail.FocusAsync();
        await trail.PressAsync("ArrowDown");
        await Expect(trail).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("trail")).ToHaveTextAsync("Ridge loop");
        await Expect(trail).ToContainTextAsync("Ridge loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldReachOptionsOutOfThePageByKey_WhenItsLongListIsVirtualized(string browser)
    {
        var page = await OpenAsync(browser, "/select-items");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Long trail", Exact = true });
        await Expect(trail).ToHaveAttributeAsync("data-ready", "");

        await trail.FocusAsync();
        await trail.PressAsync("ArrowDown");
        await Expect(trail).ToHaveAttributeAsync("aria-expanded", "true");
        await ExpectActiveAsync(page, trail, "Trail 1");
        Assert.InRange(await page.GetByRole(AriaRole.Listbox).GetByRole(AriaRole.Option).CountAsync(), 1, 99);
        await page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(page, trail, "Trail 1000");
        // Trail 999 is disabled, so the move up passes over it.
        await page.Keyboard.PressAsync("ArrowUp");
        await ExpectActiveAsync(page, trail, "Trail 998");
        await page.Keyboard.PressAsync("Home");
        await ExpectActiveAsync(page, trail, "Trail 1");
        await page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(page, trail, "Trail 1000");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("long-trail")).ToHaveTextAsync("Trail 1000");

        // Opening again starts on the chosen option, though it isn't in the page until then.
        await page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(page, trail, "Trail 1000");
        // From the chosen Trail 1000, typing wraps to Trail 1, which the closed list hasn't drawn. One key, since a
        // slow machine can split a typed word.
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("t");
        await ExpectActiveAsync(page, trail, "Trail 1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldFilterAllItsOptions_WhenItsLongListIsVirtualized(string browser)
    {
        var page = await OpenAsync(browser, "/select-items");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Filtered trail" });
        await Expect(trail).ToHaveAttributeAsync("data-ready", "");

        await trail.ClickAsync();
        await page.Keyboard.TypeAsync("rail 99");
        await Expect(page.GetByRole(AriaRole.Listbox).GetByRole(AriaRole.Option))
            .ToHaveTextAsync(["Trail 99", "Trail 990", "Trail 991", "Trail 992", "Trail 993", "Trail 994", "Trail 995",
                "Trail 996", "Trail 997", "Trail 998", "Trail 999"]);
        // End stays with the filter's text, so the arrow reaches the last match.
        for (var step = 0; step < 10; step++)
        {
            await page.Keyboard.PressAsync("ArrowDown");
        }

        await ExpectActiveAsync(page, trail, "Trail 999");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("filtered-trail")).ToHaveTextAsync("Trail 999");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldStartOnTheChosenOption_WhenItsVirtualizedListFirstOpensOrIsTypedIn(string browser)
    {
        var page = await OpenAsync(browser, "/select-items");
        var saved = page.GetByRole(AriaRole.Combobox, new() { Name = "Saved trail" });
        var typed = page.GetByRole(AriaRole.Combobox, new() { Name = "Typed trail" });
        await Expect(typed).ToHaveAttributeAsync("data-ready", "");

        await saved.FocusAsync();
        await saved.PressAsync("ArrowDown");
        await ExpectActiveAsync(page, saved, "Trail 500");
        await saved.PressAsync("Escape");
        // A single letter moves on from the chosen option.
        await typed.FocusAsync();
        await typed.PressAsync("t");
        await ExpectActiveAsync(page, typed, "Trail 501");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldReachTheEndsOfItsWholeList_WhenHomeOrEndOpensItsVirtualizedList(string browser)
    {
        var page = await OpenAsync(browser, "/select-items");
        var trail = page.GetByRole(AriaRole.Combobox, new() { Name = "Long trail", Exact = true });
        await Expect(trail).ToHaveAttributeAsync("data-ready", "");

        await trail.FocusAsync();
        await trail.PressAsync("End");
        await ExpectActiveAsync(page, trail, "Trail 1000");
        await trail.PressAsync("Escape");
        await trail.PressAsync("Home");
        await ExpectActiveAsync(page, trail, "Trail 1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiSelect_ShouldStayOnTheLastOption_WhenArrowDownIsPressedOnItInAVirtualizedList(string browser)
    {
        var page = await OpenAsync(browser, "/select-items");
        var trails = page.GetByRole(AriaRole.Combobox, new() { Name = "Long trails" });
        await Expect(trails).ToHaveAttributeAsync("data-ready", "");

        await trails.FocusAsync();
        await trails.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(page, trails, "Trail 1000");
        await page.Keyboard.PressAsync("ArrowDown");

        await ExpectActiveAsync(page, trails, "Trail 1000");
    }

    // The option marked active, which the combobox names to screen readers.
    internal static async Task ExpectActiveAsync(IPage page, ILocator combobox, string text)
    {
        var option = page.Locator($"[role=option][data-active][data-text='{text}']");
        await Expect(option).ToHaveCountAsync(1);
        await Expect(combobox).ToHaveAttributeAsync("aria-activedescendant", (await option.GetAttributeAsync("id"))!);
    }
}
