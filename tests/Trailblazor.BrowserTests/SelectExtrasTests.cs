using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SelectExtrasTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Combobox(IPage page) => page.GetByRole(AriaRole.Combobox, new() { Name = "Trails" });

    private static ILocator Park(IPage page) =>
        page.GetByRole(AriaRole.Combobox, new() { Name = "Park", Exact = true });

    // Hidden options drop out, so this lists the options the filter leaves.
    private static ILocator ParkOptions(IPage page) =>
        page.GetByRole(AriaRole.Listbox, new() { Name = "Park", Exact = true }).GetByRole(AriaRole.Option);

    private async Task<IPage> OpenExtrasAsync(string browser)
    {
        var page = await OpenAsync(browser, "/select-extras");
        await Expect(Combobox(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldKeepSayingTheListIsEmpty_WhenAnEmptyListIsFiltered(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        var favorites = page.GetByRole(AriaRole.Combobox, new() { Name = "Favorites", Exact = true });
        await favorites.ClickAsync();
        var popup = page.Locator($"#{await favorites.GetAttributeAsync("aria-controls")}");

        await popup.Locator("[data-part=filter]").FillAsync("z");

        await Expect(popup.Locator("[data-part=status]")).ToHaveTextAsync("Nothing to choose from");
        await Expect(popup.Locator("[data-part=status]")).ToBeVisibleAsync();
        await Expect(popup.Locator("[data-part=no-results]")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chips_ShouldRemoveAChoiceWithoutOpeningTheList_WhenItsButtonIsClicked(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        var chips = Combobox(page).Locator("[data-part=chip]");
        await Expect(chips).ToHaveTextAsync(["Pine Hollow", "Cedar Flats"]);

        await chips.First.Locator("[data-part=chip-remove]").ClickAsync();

        await Expect(page.GetByTestId("trails")).ToHaveTextAsync("Cedar Flats");
        await Expect(Combobox(page)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Combobox(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chips_ShouldBeSmallChipsWithA24PixelRemoveTarget_WhenShown(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        var chip = Combobox(page).Locator("[data-part=chip]").First;

        await Expect(chip).ToHaveCSSAsync("height", "24px");
        await Expect(chip.Locator("[data-part=chip-remove]")).ToHaveCSSAsync("width", "24px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chips_ShouldEndALongLabelInAnEllipsis_WhenTheSelectIsNarrow(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        var chip = Combobox(page).Locator("[data-part=chip]").First;

        await chip.EvaluateAsync("chip => chip.closest('.tb-field').style.inlineSize = '7rem'");

        await Expect(chip).ToHaveCSSAsync("height", "24px");
        Assert.True(await chip.EvaluateAsync<bool>("""
            chip => {
                const label = chip.querySelector('.tb-chip__label');
                const box = chip.closest('.tb-field__control').getBoundingClientRect();
                return chip.getBoundingClientRect().right <= box.right && label.scrollWidth > label.clientWidth;
            }
            """));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chips_ShouldAnnounceWhatTheChipShows_WhenTheOptionHasNoText(string browser)
    {
        var page = await OpenExtrasAsync(browser);

        await Combobox(page).Locator("[data-part=chip]").First.Locator("[data-part=chip-remove]").ClickAsync();

        await Expect(page.Locator("[aria-live=polite]")).ToContainTextAsync("Pine Hollow removed");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldOpenTheListWithoutTouchingTheChips_WhenChoosing(string browser)
    {
        var page = await OpenExtrasAsync(browser);

        await page.Tb().Select("Trails").ChooseAsync("Eagle Ridge");

        await Expect(page.GetByTestId("trails")).ToHaveTextAsync("Pine Hollow, Cedar Flats, Eagle Ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Backspace_ShouldRemoveTheLastChip_WhenTheComboboxHasFocus(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Combobox(page).FocusAsync();

        await page.Keyboard.PressAsync("Backspace");

        await Expect(page.GetByTestId("trails")).ToHaveTextAsync("Pine Hollow");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Groups_ShouldLabelTheirOptionsAndLetTheArrowsCrossThem_WhenTheListIsOpen(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Combobox(page).FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Trails" });
        await Expect(listbox.GetByRole(AriaRole.Group, new() { Name = "Valley" })
            .GetByRole(AriaRole.Option)).ToHaveTextAsync(["Cedar Flats"]);
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("trails")).ToHaveTextAsync("Pine Hollow, Cedar Flats, Eagle Ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Comparer_ShouldKeepTheChoicesMarked_WhenTheValuesAreLoadedAgain(string browser)
    {
        var page = await OpenExtrasAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Reload" }).ClickAsync();
        await Combobox(page).ClickAsync();

        var options = page.GetByRole(AriaRole.Option);
        await Expect(options).ToHaveCountAsync(3);
        await Expect(options.Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(options.Nth(2)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldStartWithTheTypedTextAndChooseTheFirstMatch_WhenTypingOnTheSelect(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Park(page).FocusAsync();

        await page.Keyboard.TypeAsync("anon rim");
        var filter = page.GetByRole(AriaRole.Combobox, new() { Name = "Filter" });
        await Expect(filter).ToBeFocusedAsync();
        await Expect(filter).ToHaveValueAsync("anon rim");
        await Expect(ParkOptions(page)).ToHaveTextAsync(["Cañon Rim"]);
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("park")).ToHaveTextAsync("Cañon Rim");
        await Expect(Park(page)).ToBeFocusedAsync();
        await Expect(Park(page)).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldMoveThroughTheMatchesWithTheArrows_WhenFocusIsInTheFilter(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Park(page).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var filter = page.GetByRole(AriaRole.Combobox, new() { Name = "Filter" });
        await Expect(filter).ToBeFocusedAsync();

        await page.Keyboard.TypeAsync("ri");
        await Expect(ParkOptions(page)).ToHaveTextAsync(["Eagle Ridge", "Cañon Rim"]);
        await page.Keyboard.PressAsync("Backspace");
        await Expect(ParkOptions(page)).ToHaveTextAsync(["Eagle Ridge", "Cedar Flats", "Cañon Rim"]);
        await page.Keyboard.PressAsync("ArrowDown");
        var active = await filter.GetAttributeAsync("aria-activedescendant");
        await Expect(page.Locator($"[id='{active}']")).ToHaveTextAsync("Cedar Flats");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("park")).ToHaveTextAsync("Cedar Flats");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldHideEmptyGroupsSayNoMatchesAndClearOnClose_WhenNothingMatches(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Park(page).ClickAsync();
        var filter = page.GetByRole(AriaRole.Combobox, new() { Name = "Filter" });
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Park", Exact = true });

        await filter.FillAsync("ridge");
        await Expect(listbox.GetByRole(AriaRole.Group)).ToHaveCountAsync(1);
        await filter.FillAsync("zz");
        await Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "No matches" })).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(Park(page)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(filter).ToHaveValueAsync("");
        await Expect(ParkOptions(page)).ToHaveCountAsync(4);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldChooseTheActiveMatchAndMoveOn_WhenTabIsPressedInASingleSelect(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Park(page).FocusAsync();
        await page.Keyboard.TypeAsync("cedar");

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("park")).ToHaveTextAsync("Cedar Flats");
        await Expect(Park(page)).Not.ToBeFocusedAsync();
        await Expect(Park(page)).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldStayOpenWithFocusAndText_WhenChoosingInAMultiSelect(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        var parks = page.GetByRole(AriaRole.Combobox, new() { Name = "Parks" });
        await parks.ClickAsync();
        var filter = page.GetByRole(AriaRole.Combobox, new() { Name = "Filter" });
        await filter.FillAsync("e");

        await page.GetByRole(AriaRole.Option, new() { Name = "Eagle Ridge" }).ClickAsync();
        await Expect(page.GetByTestId("parks")).ToHaveTextAsync("Eagle Ridge");
        await Expect(filter).ToBeFocusedAsync();
        await Expect(filter).ToHaveValueAsync("e");
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("parks")).ToHaveTextAsync("Eagle Ridge");
        await Expect(parks).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SelectAll_ShouldChooseAndThenTakeOutEveryOptionAndStayOpen_WhenEnterIsPressedOnIt(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        var parks = page.GetByRole(AriaRole.Combobox, new() { Name = "Parks" });
        var all = page.GetByRole(AriaRole.Option, new() { Name = "Select all" });
        await parks.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(all).ToHaveAttributeAsync("data-active", "");

        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("parks")).ToHaveTextAsync("Pine Hollow, Eagle Ridge, Cedar Flats");
        await Expect(all).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(parks).ToHaveAttributeAsync("aria-expanded", "true");
        var result = await page.RunAxe(AxeTests.WcagAA);
        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));

        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("parks")).ToHaveTextAsync("");
        await Expect(all).ToHaveAttributeAsync("aria-selected", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SelectAll_ShouldHideWhileTheFilterHasText_WhenTheSelectIsFilterable(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Parks" }).ClickAsync();
        var filter = page.GetByRole(AriaRole.Combobox, new() { Name = "Filter" });
        var all = page.GetByRole(AriaRole.Option, new() { Name = "Select all" });
        await Expect(all).ToBeVisibleAsync();

        await filter.FillAsync("e");

        await Expect(all).ToBeHiddenAsync();
        await filter.FillAsync("");
        await Expect(all).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SelectAll_ShouldBePassedOverByTyping_WhenItsNameMatchesTheTypedText(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        var seasons = page.GetByRole(AriaRole.Combobox, new() { Name = "Seasons" });
        await Expect(seasons).ToHaveAttributeAsync("data-ready", "");
        await seasons.FocusAsync();

        await page.Keyboard.PressAsync("s");

        await Expect(page.GetByRole(AriaRole.Option, new() { Name = "Spring" })).ToHaveAttributeAsync("data-active", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseAnOption_WhenTheSelectIsFilterable(string browser)
    {
        var page = await OpenExtrasAsync(browser);

        await page.Tb().Select("Park").ChooseAsync("Pine Hollow");

        await Expect(page.GetByTestId("park")).ToHaveTextAsync("Pine Hollow");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseEachOption_WhenAMultipleSelectIsFilterable(string browser)
    {
        var page = await OpenExtrasAsync(browser);

        await page.Tb().Select("Parks").ChooseAsync("Pine Hollow", "Cedar Flats");

        await Expect(page.GetByTestId("parks")).ToHaveTextAsync("Pine Hollow, Cedar Flats");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldHaveNoAccessibilityViolations_WhenOpenWithAFilter(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Park(page).ClickAsync();
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Filter" }).FillAsync("zz");

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldHaveNoAccessibilityViolations_WhenOpenWithGroupsAndChips(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Combobox(page).ClickAsync();

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldReadUnchooseAndClearTheChosenOptions_WhenGivenASelect(string browser)
    {
        var page = await OpenAsync(browser, "/examples/select");
        var topics = page.Tb().Select("Notify me about");
        await topics.ChooseAsync("Returns", "Offers");
        Assert.Equal(["Orders", "Returns", "Offers"], await topics.ChosenAsync());

        await topics.UnchooseAsync("Returns");
        Assert.Equal(["Orders", "Offers"], await topics.ChosenAsync());
        Assert.Equal("Orders, Offers", await topics.TextAsync());
        await topics.ClearAsync();
        await page.Tb().Select("Campsite").ClearAsync();

        await Expect(page.GetByText("Notifications value:")).ToHaveTextAsync("Notifications value:");
        await Expect(page.GetByText("Campsite value:")).ToHaveTextAsync("Campsite value: null");
        Assert.Null(await page.Tb().Select("Campsite").TextAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Select("Campsite").UnchooseAsync("Lake"));
    }
}
