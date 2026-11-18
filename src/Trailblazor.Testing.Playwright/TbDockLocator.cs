using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbDockManager</c>, found by its accessible name, or the only one in the scope. Its panels are named
/// by their titles and its separators by their names, such as "Resize Trails". Each action waits until the app's
/// <c>LayoutChanged</c> has run.</summary>
public sealed class TbDockLocator(ILocator scope, string? name)
{
    private ILocator Root => Scope.ByRole(scope, AriaRole.Group, name).And(scope.Locator(".tb-dock"));

    // The dock's own tabs, not those of tabs inside a panel.
    private ILocator Tabs => Root.Locator(":scope > .tb-dock__group .tb-dock__tab");

    private ILocator Tab(string panel) =>
        Tabs.And(Root.GetByRole(AriaRole.Tab, new() { Name = panel, Exact = true }));

    private ILocator Separator(string separator) => Root.Locator(":scope > .tb-dock__separator")
        .And(Root.GetByRole(AriaRole.Separator, new() { Name = separator, Exact = true }));

    /// <summary>The helpers inside the panel with this title, such as
    /// <c>page.Tb().Dock().Panel("Notes").TextField("Title")</c>, so a field that several panels have can be told
    /// apart. Only a showing panel is visible, so select the panel before using its fields.</summary>
    public TrailblazorPage Panel(string panel) => new(PanelElement(panel));

    /// <summary>The panel with this title, to assert on what it shows.</summary>
    public ILocator PanelElement(string panel) => Root.Locator(":scope > .tb-dock__panel")
        .And(Root.GetByRole(AriaRole.Tabpanel, new() { Name = panel, Exact = true, IncludeHidden = true }));

    /// <summary>The titles of every panel's tab, group by group in reading order.</summary>
    public async Task<IReadOnlyList<string>> PanelsAsync() =>
        await Tabs.Locator("[data-part=title]").AllTextContentsAsync();

    /// <summary>The titles of the panels that show, one for each group, in reading order.</summary>
    public async Task<IReadOnlyList<string>> ShowingAsync() =>
        await Tabs.And(Root.Locator("[aria-selected=true]")).Locator("[data-part=title]").AllTextContentsAsync();

    /// <summary>Clicks the tab of the panel with this title and waits until it shows.</summary>
    public async Task SelectAsync(string panel)
    {
        // Clicks reach the server only once the page is interactive, which data-ready marks.
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var tab = Tab(panel);
        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            await tab.ClickAsync();
            return true;
        });
        await Assertions.Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
    }

    /// <summary>Clicks the close mark on the tab of the panel with this title and waits until the tab is gone. Throws
    /// for a panel that can't be closed.</summary>
    public async Task CloseAsync(string panel)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var tab = Tab(panel);
        if (await tab.GetAttributeAsync("aria-keyshortcuts") is null)
        {
            throw new InvalidOperationException($"The panel \"{panel}\" can't be closed.");
        }

        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            await tab.Locator("[data-part=close]").ClickAsync();
            return true;
        });
        await Assertions.Expect(tab).ToHaveCountAsync(0);
    }

    /// <summary>The share, in percent, that the part before the separator with this name has of the space it and
    /// the part after it take together.</summary>
    public async Task<int> SizeAsync(string separator) => int.Parse(
        await Separator(separator).GetAttributeAsync("aria-valuenow") ?? "", CultureInfo.InvariantCulture);

    /// <summary>Moves the separator with this name with its keys, as a keyboard user would, until the part before it
    /// has this percent of the space it and the part after it take together, and waits until the app has the sizes.
    /// A percent that <c>MinGroupSize</c> doesn't allow fails.</summary>
    public async Task ResizeAsync(string separator, int percent)
    {
        // The keys work once the dock's script has attached.
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var bar = Separator(separator);
        var presses = await SeparatorKeys.PressesAsync(bar, percent - await SizeAsync(separator));
        await bar.FocusAsync();
        // The dock counts each press once the app has its sizes.
        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            foreach (var press in presses)
            {
                await bar.Page.Keyboard.PressAsync(press);
            }

            return presses.Count > 0;
        }, presses.Count);
        await Assertions.Expect(bar).ToHaveAttributeAsync("aria-valuenow",
            percent.ToString(CultureInfo.InvariantCulture));
    }
}
