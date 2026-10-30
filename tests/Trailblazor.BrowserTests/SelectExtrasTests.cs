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
    public async Task Helper_ShouldChooseAnOption_WhenTheSelectIsFilterable(string browser)
    {
        var page = await OpenExtrasAsync(browser);

        await page.Tb().Select("Park").ChooseAsync("Pine Hollow");

        await Expect(page.GetByTestId("park")).ToHaveTextAsync("Pine Hollow");
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
}
