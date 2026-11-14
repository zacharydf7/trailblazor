using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTabs</c>, found by its accessible name, or the only one in the scope.</summary>
public sealed class TbTabsLocator(ILocator scope, string? name)
{
    // With no name, only visible tab lists count, so tabs nested in a hidden panel don't make the lookup ambiguous.
    private ILocator List => Scope.ByRole(scope, AriaRole.Tablist, name);

    /// <summary>The helpers inside the panel of the tab with this name, such as
    /// <c>page.Tb().Tabs().Panel("Route").TextField("Start")</c>, so a field that every panel has can be told apart.
    /// Only the selected tab's panel is visible, so select the tab before using its fields.</summary>
    public TrailblazorPage Panel(string tab) => new(List.Locator("xpath=../*[@role='tabpanel']")
        .And(scope.GetByRole(AriaRole.Tabpanel, new() { Name = tab, Exact = true, IncludeHidden = true })));

    /// <summary>The tabs' texts, in order.</summary>
    public async Task<IReadOnlyList<string>> TabsAsync() =>
        [.. (await List.GetByRole(AriaRole.Tab).AllTextContentsAsync()).Select(text => text.Trim())];

    /// <summary>The selected tab's text.</summary>
    public async Task<string?> SelectedAsync()
    {
        var selected = List.Locator("[role=tab][aria-selected=true]");
        return await selected.CountAsync() == 0 ? null : (await selected.TextContentAsync())?.Trim();
    }

    /// <summary>Clicks the tab with this name and waits until it is selected.</summary>
    public async Task SelectAsync(string tab)
    {
        // Clicks on the tabs reach the server only once the page is interactive, which data-ready marks.
        await Assertions.Expect(List).ToHaveAttributeAsync("data-ready", "");
        var button = List.GetByRole(AriaRole.Tab, new() { Name = tab, Exact = true });
        await button.ClickAsync();
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
