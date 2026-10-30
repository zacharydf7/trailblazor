using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SelectExtrasTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Combobox(IPage page) => page.GetByRole(AriaRole.Combobox, new() { Name = "Trails" });

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
    public async Task Select_ShouldHaveNoAccessibilityViolations_WhenOpenWithGroupsAndChips(string browser)
    {
        var page = await OpenExtrasAsync(browser);
        await Combobox(page).ClickAsync();

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }
}
