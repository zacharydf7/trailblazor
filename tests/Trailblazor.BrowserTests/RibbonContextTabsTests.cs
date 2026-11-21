using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class RibbonContextTabsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Tab(IPage page, string title) =>
        page.GetByRole(AriaRole.Tab, new() { Name = title, Exact = true });

    // The docs example, with the table selected so that its two tabs show.
    private async Task<IPage> OpenWithTheTableSelectedAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/examples/ribbon", options);
        await Expect(page.Tb().Ribbon("Trail journal").Ribbon).ToHaveAttributeAsync("data-ready", "");
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Select the table" }).CheckAsync();
        await Expect(Tab(page, "Table layout")).ToBeVisibleAsync();
        return page;
    }

    // The color of the tab's text, the color and width of the line above it, and where its text starts and ends.
    private static Task<string[]> DrawnAsync(ILocator tab) => tab.EvaluateAsync<string[]>("""
        tab => {
            const style = getComputedStyle(tab);
            const text = document.createRange();
            text.selectNodeContents(tab);
            const box = text.getBoundingClientRect();
            return [style.color, style.borderTopColor, style.borderTopWidth, String(Math.round(box.top)),
                String(Math.round(box.bottom)), String(Math.round(tab.getBoundingClientRect().height))];
        }
        """);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextTab_ShouldBeDrawnInItsColorWithALineAboveItAndDescribedByItsContext_WhenShownInEitherTheme(
        string browser)
    {
        var page = await OpenWithTheTableSelectedAsync(browser);

        foreach (var theme in new[] { "light", "dark" })
        {
            await SetThemeAsync(page, theme);
            // The tab that isn't selected, beside a plain one that isn't selected either.
            var context = await DrawnAsync(Tab(page, "Table layout"));
            var plain = await DrawnAsync(Tab(page, "Proof"));
            var accent = await page.EvaluateAsync<string>("""
                () => {
                    const probe = document.body.appendChild(document.createElement('div'));
                    probe.style.color = 'var(--tb-color-secondary-text)';
                    const color = getComputedStyle(probe).color;
                    probe.remove();
                    return color;
                }
                """);

            Assert.Equal((accent, accent, "2px"), (context[0], context[1], context[2]));
            Assert.NotEqual(plain[0], context[0]);
            // The line takes no room from the text, which sits where a plain tab's does.
            Assert.Equal(plain[3..], context[3..]);
        }

        await Expect(Tab(page, "Table layout")).ToHaveAccessibleDescriptionAsync("Table tools");
        await Expect(Tab(page, "Proof")).ToHaveAccessibleDescriptionAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextTab_ShouldKeepItsLine_WhenForcedColorsAreOn(string browser)
    {
        var page = await OpenWithTheTableSelectedAsync(browser, new() { ForcedColors = ForcedColors.Active });

        var context = await DrawnAsync(Tab(page, "Table layout"));

        Assert.Equal((await SystemColorAsync(page, "Highlight"), "2px"), (context[1], context[2]));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextTabs_ShouldAppearBeAnnouncedAndGiveTheSelectionBack_WhenTheTableIsSelectedAndLetGo(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/ribbon");
        var ribbon = page.Tb().Ribbon("Trail journal");
        await Expect(ribbon.Ribbon).ToHaveAttributeAsync("data-ready", "");
        await ribbon.SelectAsync("Proof");
        var table = page.GetByRole(AriaRole.Checkbox, new() { Name = "Select the table" });

        await table.CheckAsync();

        // The example selects the table's first tab itself.
        await Expect(Tab(page, "Table design")).ToHaveAttributeAsync("aria-selected", "true");
        await page.Tb().Announcements.ExpectAnnouncedAsync("Table tools: Table design, Table layout tabs available");
        await ribbon.ClickAsync("Shade the rows");
        await Expect(page.GetByText("Last command: Shade the rows.")).ToBeVisibleAsync();
        await table.UncheckAsync();
        await Expect(Tab(page, "Table design")).ToHaveCountAsync(0);
        // Proof was selected before the table's tab, and is again.
        await Expect(Tab(page, "Proof")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Toolbar, new() { Name = "Proof" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextTab_ShouldHandItsFocusToTheTabSelectedAgain_WhenItGoesAwayWithFocus(string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-context");
        await Expect(page.Tb().Ribbon("Sketchbook").Ribbon).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Sketchbook" }))
            .ToHaveAttributeAsync("data-ready", "");
        await Tab(page, "Design").ClickAsync();
        await Expect(Tab(page, "Design")).ToHaveAttributeAsync("aria-selected", "true");
        await Tab(page, "Design").FocusAsync();

        await page.Keyboard.PressAsync("x");

        // Home was selected before the table's tab; View is only the tab beside it.
        await Expect(Tab(page, "Design")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Home")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab(page, "Home")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextTab_ShouldHandFocusToTheTabTheAppSelects_WhenTheAppSelectsOneAsItGoesAway(string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-context");
        await Expect(page.Tb().Ribbon("Sketchbook").Ribbon).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Sketchbook" }))
            .ToHaveAttributeAsync("data-ready", "");
        await Tab(page, "Design").ClickAsync();
        await Expect(Tab(page, "Design")).ToHaveAttributeAsync("aria-selected", "true");
        await Tab(page, "Design").FocusAsync();

        await page.Keyboard.PressAsync("y");

        // Insert is neither Home, the tab selected before, nor View, the tab beside the one that went away.
        await Expect(Tab(page, "Design")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Insert")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab(page, "Insert")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextTab_ShouldHandFocusToTheTabTheAppSelects_WhenTheRibbonIsBoundByIndex(string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-context");
        await Expect(page.Tb().Ribbon("Ledger").Ribbon).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Ledger" }))
            .ToHaveAttributeAsync("data-ready", "");
        await Tab(page, "Totals").ClickAsync();
        await Expect(Tab(page, "Totals")).ToHaveAttributeAsync("aria-selected", "true");
        await Tab(page, "Totals").FocusAsync();

        await page.Keyboard.PressAsync("z");

        // Rows is neither Sheet, the tab selected before, nor Columns, the tab beside the one that went away.
        await Expect(Tab(page, "Totals")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Rows")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab(page, "Rows")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldLeaveDeleteToThePage_WhenItCannotCloseButNamesKeysOfItsOwn(string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-context");
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Drawers" }))
            .ToHaveAttributeAsync("data-ready", "");
        await Expect(Tab(page, "Pens")).ToHaveAttributeAsync("aria-keyshortcuts", "Control+P");

        var taken = await Tab(page, "Pens").EvaluateAsync<bool>("""
            tab => !tab.dispatchEvent(new KeyboardEvent('keydown', { key: 'Delete', bubbles: true, cancelable: true }))
            """);

        Assert.False(taken);
    }
}
