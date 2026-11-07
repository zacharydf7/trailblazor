using System.Text.RegularExpressions;
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

    private async Task<IPage> OpenStatesAsync(string browser)
    {
        var page = await OpenAsync(browser, "/autocomplete-states");
        await Expect(Field(page, "Town")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldClose_WhenOpenedFromCodeAndClickedElsewhere(string browser)
    {
        var page = await OpenStatesAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Open the list" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();

        await page.GetByText("Elsewhere").ClickAsync();

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeHiddenAsync();
        await Expect(Readout(page, "Open")).ToHaveTextAsync("False");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldClose_WhenOpenedFromCodeAndEscapeIsPressed(string browser)
    {
        var page = await OpenStatesAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Open the list" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeHiddenAsync();
        await Expect(Readout(page, "Open")).ToHaveTextAsync("False");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldChooseTheOption_WhenOpenedFromCodeAndAnOptionIsClicked(string browser)
    {
        var page = await OpenStatesAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Open the list" }).ClickAsync();

        await page.GetByRole(AriaRole.Option, new() { Name = "Pine Bluff" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeHiddenAsync();
        await Expect(Readout(page, "Town chosen")).ToHaveTextAsync("Pine Bluff");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldStayClosed_WhenAButtonElsewhereTogglesItShut(string browser)
    {
        var page = await OpenStatesAsync(browser);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Toggle the list" });
        await toggle.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();

        await toggle.ClickAsync();

        // Both readouts come from one render, so this reads the state the second click left, not one before it.
        await Expect(Readout(page, "Toggles")).ToHaveTextAsync("2");
        await Expect(Readout(page, "Open")).ToHaveTextAsync("False");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldStayOpen_WhenAPressThatOpensItIsReleasedAfterAnEarlierClickElsewhere(string browser)
    {
        var page = await OpenStatesAsync(browser);
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Town" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Open the list" }).ClickAsync();
        await Expect(listbox).ToBeVisibleAsync();
        await page.GetByText("Elsewhere").ClickAsync();
        await Expect(listbox).ToBeHiddenAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Open on press" }).HoverAsync();
        await page.Mouse.DownAsync();
        await Expect(listbox).ToBeVisibleAsync();
        await page.Mouse.UpAsync();
        // A close would be waiting on a timer, which fires before this one.
        await page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve))");

        await Expect(listbox).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldStayOpen_WhenADragFromTheFieldEndsOutsideIt(string browser)
    {
        var page = await OpenStatesAsync(browser);
        var town = Field(page, "Town");
        await town.PressSequentiallyAsync("pine");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();
        var box = (await town.BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y - 10);
        await page.Mouse.UpAsync();
        // A close would be waiting on a timer, which fires before this one.
        await page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve))");

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();
        await Expect(town).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldNotActivateAnOption_WhenAnArrowIsPressedAfterTheFieldTurnedReadOnly(string browser)
    {
        var page = await OpenStatesAsync(browser);
        var town = Field(page, "Town");
        await town.PressSequentiallyAsync("pine");
        await Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Expect(Readout(page, "Town chosen")).ToHaveTextAsync("Pine Bluff");
        await page.GetByRole(AriaRole.Button, new() { Name = "Make read-only" }).ClickAsync();
        await Expect(town).ToHaveAttributeAsync("readonly", "");

        await town.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(town).Not.ToHaveAttributeAsync("aria-activedescendant", new Regex("."));
    }

    private static ILocator Readout(IPage page, string name) =>
        page.GetByRole(AriaRole.Status, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldOpenJustBelowTheField_WhenTheUserTypes(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Trailhead" });

        await Field(page, "Trailhead").PressSequentiallyAsync("ridge");
        await Expect(listbox).ToHaveAttributeAsync("data-side", "bottom");

        var control = page.Locator("[data-part=control]").Filter(new() { Has = Field(page, "Trailhead") });
        var controlBox = await control.BoundingBoxAsync();
        var listBox = await listbox.BoundingBoxAsync();
        Assert.Equal(controlBox!.Y + controlBox.Height + 4, listBox!.Y, tolerance: 1);
        Assert.Equal(controlBox.X, listBox.X, tolerance: 1);
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
        // The list is a closed popup until the user types, so it is found while hidden.
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Trailhead", IncludeHidden = true });

        // The searching state lasts only as long as the search, so a watcher notes it rather than a poll racing it.
        await listbox.EvaluateAsync("""
            listbox => {
                window.seen = new Set();
                new MutationObserver(() => {
                    if (listbox.getAttribute('aria-busy') === 'true') {
                        window.seen.add(listbox.textContent.trim());
                    }
                }).observe(listbox, { attributes: true, childList: true, subtree: true, characterData: true });
            }
            """);

        await Field(page, "Trailhead").PressSequentiallyAsync("lake");

        await Expect(listbox.GetByRole(AriaRole.Option)).ToHaveTextAsync(["Lakeshore Lake country"]);
        await Expect(listbox).Not.ToHaveAttributeAsync("aria-busy", "true");
        Assert.Contains("Searching…", await page.EvaluateAsync<string[]>("() => [...window.seen]"));
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
    public async Task Listbox_ShouldStayOpenWithNothingActive_WhenItsMessageIsClickedOrArrowedTo(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var region = Field(page, "Region");
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Region" });
        await region.PressSequentiallyAsync("zzz");
        await Expect(listbox.GetByText("No matches")).ToBeVisibleAsync();

        // Playwright won't click something aria-disabled unless told to, and a person still can.
        await listbox.GetByText("No matches").ClickAsync(new() { Force = true });
        await Expect(listbox).ToBeVisibleAsync();
        await region.PressAsync("ArrowDown");

        await Expect(listbox).ToBeVisibleAsync();
        Assert.Null(await region.GetAttributeAsync("aria-activedescendant"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Listbox_ShouldWidenPastTheField_WhenASuggestionIsLong(string browser)
    {
        var page = await OpenFieldsAsync(browser);
        var region = Field(page, "Region");

        await region.PressSequentiallyAsync("the far");
        var listbox = page.GetByRole(AriaRole.Listbox, new() { Name = "Region" });
        await Expect(listbox.GetByRole(AriaRole.Option, new() { Name = "The far north" })).ToBeVisibleAsync();

        var control = page.Locator("[data-part=control]").Filter(new() { Has = region });
        Assert.True((await listbox.BoundingBoxAsync())!.Width > (await control.BoundingBoxAsync())!.Width);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ChooseAsync_ShouldTypeWaitAndChoose_WhenGivenTheTextAndTheOption(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");

        await page.Tb().Autocomplete("Trailhead").ChooseAsync("summit", "Summit Gate High peaks");

        await Expect(page.GetByText("Starting at:")).ToHaveTextAsync("Starting at: Summit Gate");
    }
}
