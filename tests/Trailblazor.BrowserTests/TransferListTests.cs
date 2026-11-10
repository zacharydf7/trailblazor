using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TransferListTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenCrewAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/transfer-list");
        var crew = page.GetByRole(AriaRole.Group, new() { Name = "Trip crew" });
        foreach (var list in await crew.GetByRole(AriaRole.Listbox).AllAsync())
        {
            await Expect(list).ToHaveAttributeAsync("data-ready", "");
        }

        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TransferList_ShouldMoveTheMarkedItem_WhenItsMarkedAndMovedWithTheKeys(string browser)
    {
        var page = await OpenCrewAsync(browser);
        await page.GetByRole(AriaRole.Listbox, new() { Name = "Hikers" }).FocusAsync();
        var move = page.GetByRole(AriaRole.Button, new() { Name = "Move to On this trip" });

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Space");
        // The button turns on once the server has the mark; until then Enter on it does nothing.
        await Expect(move).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await page.Keyboard.PressAsync("Tab");
        await Expect(move).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Crew")).ToHaveTextAsync("Blake, Casey, Emery");
        await Expect(move).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "On this trip" }).GetByRole(AriaRole.Option))
            .ToHaveTextAsync(["Blake", "Casey", "Emery"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MoveAsync_ShouldMoveTheItemsAndWaitForThem_WhenCalled(string browser)
    {
        var page = await OpenCrewAsync(browser);
        var crew = page.Tb().TransferList("Trip crew");

        await crew.MoveAsync("Casey", "Jordan");
        await crew.MoveBackAsync("Blake");

        Assert.Equal(["Casey", "Emery", "Jordan"], await crew.ChosenAsync());
        await Expect(page.GetByLabel("Crew")).ToHaveTextAsync("Casey, Emery, Jordan");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TransferList_ShouldMoveOnlyTheItemsShown_WhenTheListIsSearchedAndAllAreMoved(string browser)
    {
        var page = await OpenCrewAsync(browser);

        await page.GetByLabel("Search Hikers").FillAsync("er");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Hikers" }).GetByRole(AriaRole.Option))
            .ToHaveTextAsync(["Avery", "Harper"]);
        await page.GetByRole(AriaRole.Button, new() { Name = "Move all to On this trip" }).ClickAsync();

        await Expect(page.GetByLabel("Crew")).ToHaveTextAsync("Avery, Blake, Emery, Harper");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TransferList_ShouldTurnItsArrowsAround_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenCrewAsync(browser);
        var crew = page.GetByRole(AriaRole.Group, new() { Name = "Trip crew" });

        await crew.EvaluateAsync("fieldset => fieldset.dir = 'rtl'");

        await Expect(crew.Locator("[data-part=move-chosen] svg"))
            .ToHaveCSSAsync("transform", "matrix(-1, 0, 0, 1, 0, 0)");
    }
}
