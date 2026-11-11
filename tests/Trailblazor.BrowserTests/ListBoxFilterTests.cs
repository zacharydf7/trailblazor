namespace Trailblazor.BrowserTests;

// A Filterable list box narrows its options as the user types in the box above them.
public class ListBoxFilterTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldShowOnlyOptionsThatMatchIgnoringCaseAndAccents_WhenTyped(string browser)
    {
        var page = await OpenFilterAsync(browser);
        var trail = page.GetByRole(AriaRole.Listbox, new() { Name = "Trail", Exact = true });
        var filter = FilterOf(trail);
        await Expect(filter).ToHaveAccessibleDescriptionAsync("Trail");

        await filter.FillAsync("eagle");
        await Expect(trail.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Éagle Ridge"]);

        await filter.FillAsync("zzz");
        await Expect(trail.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "No matches" })).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldMoveIntoTheListAndChooseOnlyShownOptions_WhenDownIsPressed(string browser)
    {
        var page = await OpenFilterAsync(browser);
        var trail = page.GetByRole(AriaRole.Listbox, new() { Name = "Trail", Exact = true });
        await FilterOf(trail).FillAsync("lo");

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(trail).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByLabel("Trail chosen")).ToHaveTextAsync("Pine Hollow");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CtrlA_ShouldChooseOnlyTheShownOptions_WhenAFilterNarrowsAMultipleList(string browser)
    {
        var page = await OpenFilterAsync(browser);
        var trails = page.GetByRole(AriaRole.Listbox, new() { Name = "Trails" });
        await FilterOf(trails).FillAsync("lo");

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(trails).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Control+a");

        await Expect(page.GetByLabel("Trails chosen")).ToHaveTextAsync("Lookout Loop, Pine Hollow");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filterable_ShouldShowEveryOptionAgain_WhenTurnedOffWhileFiltered(string browser)
    {
        var page = await OpenFilterAsync(browser);
        var camp = page.GetByRole(AriaRole.Listbox, new() { Name = "Camp" });
        await FilterOf(camp).FillAsync("lo");
        await Expect(camp.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);

        await page.GetByRole(AriaRole.Switch, new() { Name = "Filter the camps" }).ClickAsync();

        await Expect(FilterOf(camp)).ToHaveCountAsync(0);
        await Expect(camp.GetByRole(AriaRole.Option)).ToHaveCountAsync(5);
        await Expect(camp.GetByRole(AriaRole.Group, new() { Name = "Closed" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldHideEmptyGroupsAndOptionsAddedLater_WhenTheyDontMatch(string browser)
    {
        var page = await OpenFilterAsync(browser);
        var region = page.GetByRole(AriaRole.Listbox, new() { Name = "Region" });
        await FilterOf(region).FillAsync("pine");
        await Expect(region.GetByRole(AriaRole.Group)).ToHaveCountAsync(1);
        await Expect(region.GetByRole(AriaRole.Group)).ToHaveAccessibleNameAsync("North");

        await page.GetByRole(AriaRole.Button, new() { Name = "Add a trail" }).ClickAsync();

        var added = region.Locator("[role=option]", new() { HasText = "Lake Shore" });
        await Expect(added).ToHaveCountAsync(1);
        // Its group is hidden too, so the option's own mark is what shows it was filtered.
        await Expect(added).ToHaveAttributeAsync("hidden", "");
        await Expect(region.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Pine Hollow"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Groups_ShouldAllShow_WhenTheListIsntFilterable(string browser)
    {
        var page = await OpenFilterAsync(browser);
        var season = page.GetByRole(AriaRole.Listbox, new() { Name = "Season" });

        await Expect(season.GetByRole(AriaRole.Group)).ToHaveCountAsync(2);
        await Expect(season.GetByRole(AriaRole.Group, new() { Name = "Winter" })).Not.ToHaveAttributeAsync("hidden", "");
    }

    // Each filter sits beside its own list, inside the field.
    private static ILocator FilterOf(ILocator listbox) =>
        listbox.Locator("xpath=..").GetByRole(AriaRole.Textbox, new() { Name = "Filter" });

    private async Task<IPage> OpenFilterAsync(string browser)
    {
        var page = await OpenAsync(browser, "/listbox-filter");
        foreach (var name in new[] { "Trail", "Trails", "Camp", "Region", "Season" })
        {
            await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = name, Exact = true }))
                .ToHaveAttributeAsync("data-ready", "");
        }

        return page;
    }
}
