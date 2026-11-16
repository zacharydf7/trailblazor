using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTabs</c>, found by its accessible name or a locator, or the only one in the scope.</summary>
public sealed class TbTabsLocator(ILocator scope, ILocator tabs)
{
    // An id goes on the tabs around the list, so the list is also looked for inside the match.
    private ILocator List => tabs.Locator("xpath=descendant-or-self::*[@role='tablist'][1]")
        .And(scope.Locator("[role=tablist]"));

    /// <summary>The helpers inside the panel of the tab with this name, such as
    /// <c>page.Tb().Tabs().Panel("Route").TextField("Start")</c>, so a field that every panel has can be told apart.
    /// Only the selected tab's panel is visible, so select the tab before using its fields.</summary>
    public TrailblazorPage Panel(string tab) => new(PanelElement(tab));

    /// <summary>The panel of the tab with this name, to assert on what it shows.</summary>
    public ILocator PanelElement(string tab) => List.Locator("xpath=../*[@role='tabpanel']")
        .And(scope.GetByRole(AriaRole.Tabpanel, new() { Name = tab, Exact = true, IncludeHidden = true }));

    /// <summary>The tabs' texts, in order.</summary>
    public async Task<IReadOnlyList<string>> TabsAsync() =>
        [.. (await List.GetByRole(AriaRole.Tab).AllTextContentsAsync()).Select(text => text.Trim())];

    /// <summary>The selected tab's text.</summary>
    public async Task<string?> SelectedAsync()
    {
        var selected = List.Locator("[role=tab][aria-selected=true]");
        return await selected.CountAsync() == 0 ? null : (await selected.TextContentAsync())?.Trim();
    }

    /// <summary>Clicks the tab with this name and waits until it is selected and the app has run its callbacks, such
    /// as <c>ActiveKeyChanged</c> and the tab's <c>OnClick</c>.</summary>
    public async Task SelectAsync(string tab)
    {
        // Clicks on the tabs reach the server only once the page is interactive, which data-ready marks.
        await Assertions.Expect(List).ToHaveAttributeAsync("data-ready", "");
        var button = List.GetByRole(AriaRole.Tab, new() { Name = tab, Exact = true });
        await Commit.WaitAsync(List, async () =>
        {
            await button.ClickAsync();
            return true;
        });
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-selected", "true");
    }

    /// <summary>Clicks the add button that follows the tab list and waits until the app's <c>OnAdd</c> has run.
    /// </summary>
    public async Task AddAsync()
    {
        await Assertions.Expect(List).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(List, async () =>
        {
            await List.Locator("xpath=../*[@class='tb-tabs__actions']").Locator("[data-part=add]").ClickAsync();
            return true;
        });
    }

    /// <summary>Clicks the close mark of the tab with this name and waits until the tab's <c>OnClose</c> has run.
    /// </summary>
    public async Task CloseAsync(string tab)
    {
        await Assertions.Expect(List).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(List, async () =>
        {
            await List.GetByRole(AriaRole.Tab, new() { Name = tab, Exact = true }).Locator("[data-part=close]")
                .ClickAsync();
            return true;
        });
    }
}
