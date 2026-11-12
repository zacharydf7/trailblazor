using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTabs</c>, found by its accessible name, or the only one in the scope.</summary>
public sealed class TbTabsLocator(ILocator scope, string? name)
{
    private ILocator List => name is null ? scope.GetByRole(AriaRole.Tablist)
        : scope.GetByRole(AriaRole.Tablist, new() { Name = name, Exact = true });

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
