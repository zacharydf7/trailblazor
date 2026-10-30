namespace Trailblazor.BrowserTests;

public class ReadOnlyTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Each field and the boxes in it that take text.
    private static readonly (string Field, string Input)[] Boxes =
    [
        ("name", "[data-part=input]"), ("copies", "[data-part=input]"), ("town", "[data-part=input]"),
        ("color", "[data-part=input]"), ("dates", "[data-part=start-input]"), ("dates", "[data-part=end-input]"),
    ];

    public static TheoryData<string, string, string> BoxesInEachBrowser => new(
        from browser in AllBrowsers.Names
        from box in Boxes
        select (browser, box.Field, box.Input));

    [Theory, MemberData(nameof(BoxesInEachBrowser))]
    public async Task ReadOnlyField_ShouldKeepItsValueButTakeFocus_WhenTheUserTriesToEditIt(string browser,
        string field, string box)
    {
        var page = await OpenAsync(browser, "/read-only");
        var input = page.GetByTestId(field).Locator(box);
        var value = await input.InputValueAsync();

        await input.FocusAsync();
        await Expect(input).ToBeFocusedAsync();
        await input.PressAsync("End");
        await input.PressAsync("ArrowUp");
        await input.PressSequentiallyAsync("9");
        await input.PressAsync("Tab");

        await Expect(input).ToHaveValueAsync(value);
        await Expect(input).ToHaveAttributeAsync("readonly", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadOnlyAutocomplete_ShouldKeepItsListClosed_WhenTheKeysWouldOpenIt(string browser)
    {
        var page = await OpenAsync(browser, "/read-only");
        var town = page.GetByRole(AriaRole.Combobox, new() { Name = "Town" });
        await Expect(town).ToHaveAttributeAsync("data-ready", "");

        await town.PressAsync("ArrowDown");

        await Expect(town).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByRole(AriaRole.Listbox)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReadOnlyPickers_ShouldTurnOffTheirButtonsAndHideClear_WhenReadOnly(string browser)
    {
        var page = await OpenAsync(browser, "/read-only");

        foreach (var field in new[] { "color", "dates" })
        {
            foreach (var button in await page.GetByTestId(field).GetByRole(AriaRole.Button).AllAsync())
            {
                await Expect(button).ToBeDisabledAsync();
            }
        }

        await Expect(page.Locator("[data-part=clear]")).ToHaveCountAsync(0);
    }
}
