using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class CommandPaletteTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcut_ShouldOpenThePaletteWithItsSearchFocused_WhenPressedAnywhere(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var opener = page.GetByRole(AriaRole.Button, new() { Name = "Open commands" });
        await opener.FocusAsync();

        await page.Keyboard.PressAsync("ControlOrMeta+K");

        var search = page.GetByRole(AriaRole.Dialog, new() { Name = "Command palette" })
            .GetByRole(AriaRole.Combobox, new() { Name = "Command palette" });
        await Expect(search).ToBeFocusedAsync();
        await Expect(search).ToHaveAttributeAsync("data-ready", "");
        await page.Keyboard.PressAsync("ControlOrMeta+K");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(opener).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveTheActiveCommandAndRunIt_WhenTheArrowsAndEnterArePressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var search = await OpenPaletteAsync(page);

        await search.PressSequentiallyAsync("tab");
        await Expect(page.GetByRole(AriaRole.Option).Locator("[data-part=label]"))
            .ToHaveTextAsync(["New tab", "Close tab"]);
        await Expect(search).ToHaveAttributeAsync("aria-activedescendant",
            (await page.GetByRole(AriaRole.Option).First.GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByRole(AriaRole.Option, new() { Name = "Close tab" })).ToHaveAttributeAsync(
            "aria-selected", "true");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("chosen")).ToHaveTextAsync("Close tab");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Open commands" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldSkipADisabledCommandAndWrapAround_WhenTheArrowsMovePastIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var search = await OpenPaletteAsync(page);

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(search).ToHaveAttributeAsync("aria-activedescendant",
            (await page.GetByRole(AriaRole.Option, new() { Name = "Settings" }).GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("ArrowUp");

        await Expect(page.GetByRole(AriaRole.Option, new() { Name = "Trails" })).ToHaveAttributeAsync(
            "aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMakeTheBestMatchActiveAgain_WhenTheSearchChanges(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var search = await OpenPaletteAsync(page);
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");

        await search.PressSequentiallyAsync("tr");
        await Expect(page.GetByRole(AriaRole.Option).First).ToHaveAttributeAsync("aria-selected", "true");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("chosen")).ToHaveTextAsync("Trails");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Click_ShouldKeepFocusInTheSearch_WhenADisabledCommandIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var search = await OpenPaletteAsync(page);

        // Playwright waits for an aria-disabled element to turn enabled, but a user can still press it.
        await page.GetByRole(AriaRole.Option, new() { Name = "Delete everything" }).ClickAsync(new() { Force = true });

        await Expect(search).ToBeFocusedAsync();
        await Expect(page.GetByTestId("chosen")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Click_ShouldRunTheCommandAndKeepFocusInTheSearchUntilThen_WhenAnOptionIsPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var search = await OpenPaletteAsync(page);

        await page.GetByRole(AriaRole.Option, new() { Name = "Trails" }).HoverAsync();
        await Expect(page.GetByRole(AriaRole.Option, new() { Name = "Trails" })).ToHaveAttributeAsync(
            "aria-selected", "true");
        await Expect(search).ToBeFocusedAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Trails" }).ClickAsync();

        await Expect(page.GetByTestId("chosen")).ToHaveTextAsync("Trails");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldCloseThePaletteAndReturnFocus_WhenPressedInTheSearch(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await OpenPaletteAsync(page);

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Open commands" })).ToBeFocusedAsync();
        await Expect(page.GetByTestId("chosen")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldOpenSearchAndRunACommand_WhenThePageIsStillLoading(string browser)
    {
        // Doesn't wait for the page to turn interactive, so the helpers' own waits are what this checks.
        var page = await OpenStaticAsync(browser, "/command-palette");
        var palette = page.Tb().CommandPalette("Command palette");

        await palette.OpenAsync();
        await palette.SearchAsync("tab");
        Assert.Equal(["New tab", "Close tab"], await palette.ResultsAsync());
        await palette.ChooseAsync("Close tab");

        Assert.Equal("Close tab", await page.GetByTestId("chosen").TextContentAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await palette.OpenAsync();
            await palette.ChooseAsync("Delete everything");
        });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldReadAndRunTemplatedCommands_WhenTheLabelHasAnApostropheAndThereIsNoShortcut(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        var picks = page.Tb().CommandPalette("Today's picks");
        await Assert.ThrowsAsync<InvalidOperationException>(picks.OpenAsync);

        await page.GetByRole(AriaRole.Button, new() { Name = "Open picks" }).ClickAsync();
        await picks.SearchAsync("pine");
        Assert.Equal(["Pine loop new"], await picks.ResultsAsync());
        await picks.ChooseAsync("Pine loop new");

        Assert.Equal("Pine loop", await page.GetByTestId("chosen").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcut_ShouldFollowTheNewKeys_WhenTheAppChangesThem(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Use Alt+J" }).ClickAsync();
        await Expect(page.Locator("[data-command-palette='Command palette']"))
            .ToHaveAttributeAsync("data-shortcut", "Alt+J");
        await page.Tb().CommandPalette("Command palette").OpenAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("ControlOrMeta+K");

        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldPressTheAppsShortcut_WhenTheAppSetsItAsThePageTurnsInteractive(string browser)
    {
        var page = await OpenStaticAsync(browser, "/command-palette?keys=Alt%2BJ");

        await page.Tb().CommandPalette("Command palette").OpenAsync();

        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Command palette" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Shortcut_ShouldLeaveTheNewListenersMark_WhenTheOldListenerIsDisposedAfterIt(string browser)
    {
        var page = await OpenReadyAsync(browser);

        // .NET may dispose a replaced listener after its replacement attached, so the order is forced here.
        var marks = await page.EvaluateAsync<string?[]>("""
            async () => {
                const palette = await import('/_content/Trailblazor/command-palette.js');
                const marker = document.createElement('span');
                const old = palette.attachShortcut(marker, 'Alt+Q');
                const replacement = palette.attachShortcut(marker, 'Alt+W');
                old.dispose();
                const kept = marker.dataset.ready;
                replacement.dispose();
                return [kept, marker.dataset.ready ?? null];
            }
            """);

        Assert.Equal(["Alt+W", null], marks);
    }

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/command-palette");
        await Expect(page.Locator("[data-command-palette='Command palette']")).ToHaveAttributeAsync("data-ready",
            "Mod+K");
        return page;
    }

    // Opened from its button, so focus has a place to return to.
    private static async Task<ILocator> OpenPaletteAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Open commands" }).ClickAsync();
        var search = page.GetByRole(AriaRole.Combobox, new() { Name = "Command palette" });
        await Expect(search).ToHaveAttributeAsync("data-ready", "");
        await Expect(search).ToBeFocusedAsync();
        return search;
    }
}
