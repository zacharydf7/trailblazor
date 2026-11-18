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

    /// <summary>Moves the tab of the panel with this title along its row by this many places, forward when the
    /// number is positive and back when it's negative, with "Move tab forward" and "Move tab back" in its group's
    /// menu. The menu acts on the group's showing panel, so each move helper selects the panel first. Throws when
    /// the tab reaches the start or end of its row before it has moved that far.</summary>
    public async Task MoveTabAsync(string panel, int places)
    {
        for (var moved = 0; moved < Math.Abs(places); moved++)
        {
            await ChooseAsync(panel, places > 0 ? "[data-move=forward]" : "[data-move=back]",
                $"The tab of \"{panel}\" is at the {(places > 0 ? "end" : "start")} of its row.");
        }
    }

    /// <summary>Moves the panel with this title to the end of the group with this number, counted from 1 in reading
    /// order, with "Move to group" in its group's menu, and waits until it shows there. Throws for the panel's own
    /// group or a number the dock doesn't have.</summary>
    public async Task MoveToGroupAsync(string panel, int group) =>
        await ChooseAsync(panel, $"[data-move-group='{group.ToString(CultureInfo.InvariantCulture)}']",
            $"The dock has no other group {group} to move \"{panel}\" to.");

    /// <summary>Gives the panel with this title a new group on this side of its own, sharing the space the group
    /// had, with "New group beside this one" in its group's menu. Throws for a panel that is alone in its group, and
    /// for <c>Before</c> or <c>After</c>.</summary>
    public async Task MoveToNewGroupAsync(string panel, DockPosition side) =>
        await ChooseAsync(panel, $"[data-move-beside={Side(side, nameof(side))}]",
            $"The panel \"{panel}\" is alone in its group, so it has no group to split.");

    /// <summary>Gives the panel with this title a new group along this edge of the dock, with "New group at the
    /// dock's edge" in its group's menu. Throws when it's the only panel docked, and for <c>Before</c> or
    /// <c>After</c>.</summary>
    public async Task MoveToEdgeAsync(string panel, DockPosition edge) =>
        await ChooseAsync(panel, $"[data-move-edge={Side(edge, nameof(edge))}]",
            $"The panel \"{panel}\" is the only one docked, so it has nowhere to move.");

    // Clicks an item of the menu of the panel's group, which acts on the group's showing panel, and waits until the
    // app has the layout.
    private async Task ChooseAsync(string panel, string hook, string missing)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var tab = Tab(panel);
        if (await tab.GetAttributeAsync("aria-selected") != "true")
        {
            await SelectAsync(panel);
        }

        var trigger = tab.Locator(
            "xpath=ancestor::*[@data-part='group'][1]/*[@data-part='group-menu']/*[@data-part='trigger']");
        // The items are on the page whether the menu is open or not.
        var item = Scope.Document(scope).Locator($"[id='{await trigger.GetAttributeAsync("aria-controls")}']")
            .Locator(hook);
        if (await item.CountAsync() == 0 || await item.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException(missing);
        }

        await new TbMenuLocator(scope, trigger).OpenAsync();
        var submenu = item.Locator(
            "xpath=ancestor::*[@role='menu'][1]/preceding-sibling::*[@data-part='submenu-trigger']");
        if (await submenu.CountAsync() > 0)
        {
            await submenu.ClickAsync();
            await Assertions.Expect(submenu).ToHaveAttributeAsync("aria-expanded", "true");
        }

        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            await item.ClickAsync();
            return true;
        });
    }

    private static string Side(DockPosition side, string parameter) =>
        side is DockPosition.Before or DockPosition.After
            ? throw new ArgumentOutOfRangeException(parameter, side, "A new group goes to the start, end, top or bottom.")
            : side.ToString().ToLowerInvariant();

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
