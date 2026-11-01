using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTabs</c>, found by its accessible name, or the only one on the page.</summary>
public sealed class TbTabsLocator(IPage page, string? name)
{
    private ILocator List => name is null ? page.GetByRole(AriaRole.Tablist)
        : page.GetByRole(AriaRole.Tablist, new() { Name = name, Exact = true });

    /// <summary>Clicks the tab with this name and waits until it is selected.</summary>
    public async Task SelectAsync(string tab)
    {
        // The tabs' clicks reach the server once the page is interactive, which data-ready follows.
        await Assertions.Expect(List).ToHaveAttributeAsync("data-ready", "");
        var button = List.GetByRole(AriaRole.Tab, new() { Name = tab, Exact = true });
        await button.ClickAsync();
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-selected", "true");
    }
}
