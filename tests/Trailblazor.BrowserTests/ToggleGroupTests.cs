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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldKeepThePressedItemsHighlight_WhenHoveredWithColorsForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toggle-group", new() { ForcedColors = ForcedColors.Active });
        var highlight = await SystemColorAsync(page, "Highlight");

        // The outlined group and the quiet text one each have a hover fill of their own.
        foreach (var (group, item) in new[] { ("Map view", "Trails"), ("Note style", "Bold") })
        {
            await Item(page, group, item).HoverAsync();
            await Expect(Item(page, group, item)).ToHaveCSSAsync("background-color", highlight);
        }

        // A disabled pressed item keeps it too, since the hover rules skip disabled buttons.
        var bold = Item(page, "Note style", "Bold");
        await bold.EvaluateAsync("bold => bold.disabled = true");
        await bold.HoverAsync(new() { Force = true });
        await Expect(bold).ToHaveCSSAsync("background-color", highlight);
    }
}
