namespace Trailblazor.BrowserTests;

public class ToggleGroupTests(BrowserFixture fixture) : ToggleGroupKeysTests(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldClearThePressedItem_WhenItIsPressedAgainInAClearableGroup(string browser)
    {
        var page = await OpenGroupsAsync(browser);
        var miles = Item(page, "Distance units", "Miles");

        await miles.ClickAsync();
        await Expect(page.GetByText("Units: ")).ToHaveTextAsync("Units: Miles");
        await miles.ClickAsync();

        await Expect(page.GetByText("Units: ")).ToHaveTextAsync("Units: none");
        await Expect(miles).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldFillThePressedItemWithTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group", new() { ForcedColors = ForcedColors.Active });

        await Expect(Item(page, "Map view", "Trails"))
            .ToHaveCSSAsync("background-color", await SystemColorAsync(page, "Highlight"));
    }
}
