using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class AutocompleteTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Field(IPage page, string name) =>
        page.GetByRole(AriaRole.Combobox, new() { Name = name, Exact = true });

    private async Task<IPage> OpenFieldsAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");
        await Expect(Field(page, "Trailhead")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldSearchAndChooseWithTheKeys_WhenTheUserTypes(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var trailhead = Field(page, "Trailhead");
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Trailhead" });

        await trailhead.PressSequentiallyAsync("ridge");
        await Expect(listbox).ToBeVisibleAsync();
        await Expect(listbox.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(trailhead).ToHaveAttributeAsync("aria-activedescendant",
            (await listbox.GetByRole(AriaRole.Option).First.GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("Enter");

        await Expect(listbox).ToBeHiddenAsync();
        await Expect(trailhead).ToHaveValueAsync("Pine Hollow");
        await Expect(trailhead).ToBeFocusedAsync();
        await Expect(page.GetByText("Starting at:")).ToHaveTextAsync("Starting at: Pine Hollow");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldShowThatItIsSearching_WhileTheSearchRuns(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Trailhead" });

        await Field(page, "Trailhead").PressSequentiallyAsync("lake");

        await Expect(listbox).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(listbox.Locator("[data-part=status]")).ToHaveTextAsync("Searching…");
        await Expect(listbox.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Lakeshore Lake country"]);
        await Expect(listbox).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldCloseAndPutBackTheChosenText_WhenEscapeIsPressedAndTheFieldIsLeft(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var trailhead = Field(page, "Trailhead");
        await page.Tb().Autocomplete("Trailhead").ChooseAsync("cedar", "Cedar Flats River valley");

        await trailhead.FillAsync("nowhere");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Trailhead" })).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(trailhead).ToHaveValueAsync("Cedar Flats");
        await Expect(page.GetByText("Starting at:")).ToHaveTextAsync("Starting at: Cedar Flats");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldTakeTypedTextAsTheValue_WhenFreeTextIsEnteredOrLeft(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var weight = Field(page, "Pack weight (kg)");

        await weight.FillAsync("8.25");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByText("Packing:")).ToHaveTextAsync("Packing: 8.25 kg");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Pack weight (kg)" })).ToBeHiddenAsync();
        await weight.FillAsync("9");
        // The weight is the page's last field, so focus goes back to leave it.
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(page.GetByText("Packing:")).ToHaveTextAsync("Packing: 9 kg");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldKeepFocusInTheInput_WhenAnOptionIsClicked(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var weight = Field(page, "Pack weight (kg)");
        await weight.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await page.GetByRole(AriaRole.Option, new() { Name = "7.5" }).ClickAsync();

        await Expect(weight).ToBeFocusedAsync();
        await Expect(page.GetByText("Packing:")).ToHaveTextAsync("Packing: 7.5 kg");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldTypeWaitAndChoose_WhenGivenTheTextAndTheOption(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");

        await page.Tb().Autocomplete("Trailhead").ChooseAsync("summit", "Summit Gate High peaks");

        await Expect(page.GetByText("Starting at:")).ToHaveTextAsync("Starting at: Summit Gate");
    }
}
