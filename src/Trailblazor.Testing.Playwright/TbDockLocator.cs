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

    // The frame around the dock's area, which holds the strips of auto-hidden panels beside it.
    private ILocator Frame => Root.Locator("xpath=..");

    private ILocator StripButtons => Frame.Locator(":scope > [data-part=strip] > [data-part=strip-button]");

    private ILocator StripButton(string panel) =>
        StripButtons.And(Frame.GetByRole(AriaRole.Button, new() { Name = panel, Exact = true }));

    // Waits until clicks reach the server, which data-ready marks, and gives the panel's tab. A tab behind the group
    // that fills the dock can't be clicked, so this throws rather than let the click wait until it times out.
    private async Task<ILocator> ReachableAsync(string panel)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        // A covered tab is hidden from screen readers, so only IncludeHidden finds it by role.
        var covered = Root.Locator(":scope > .tb-dock__group--covered .tb-dock__tab")
            .And(Root.GetByRole(AriaRole.Tab, new() { Name = panel, Exact = true, IncludeHidden = true }));
        if (await covered.CountAsync() > 0)
        {
            throw new InvalidOperationException(
                $"The panel \"{panel}\" is behind the group that fills the dock; restore the groups first.");
        }

        // A panel on a strip has no tab until it slides out.
        if (await StripButton(panel).CountAsync() > 0)
        {
            await RevealAsync(panel);
        }

        return Tab(panel);
    }

    // The row of the window that holds a panel, once the dock's script has attached.
    private async Task<ILocator> WindowRowAsync(string panel)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        // A panel on a strip is in no window, so it isn't slid out to look for one.
        if (await StripButton(panel).CountAsync() > 0)
        {
            throw new InvalidOperationException($"The panel \"{panel}\" isn't in a floating window.");
        }

        var tab = await ReachableAsync(panel);
        var row = tab.Locator("xpath=ancestor::*[@data-part='group'][1]")
            .And(Root.Locator(":scope > .tb-dock__group--floating:not(.tb-dock__group--revealed)"));
        return await row.CountAsync() > 0 ? row
            : throw new InvalidOperationException($"The panel \"{panel}\" isn't in a floating window.");
    }

    // Presses arrow keys on a window's Move button or grip, ten percent at a time with Shift and then one at a time.
    // The dock counts each press once the app has the layout.
    private async Task PressAsync(ILocator handle, params (string Key, int Percent)[] moves)
    {
        var presses = moves.SelectMany(move => Enumerable.Repeat($"Shift+{move.Key}", Math.Abs(move.Percent) / 10)
            .Concat(Enumerable.Repeat(move.Key, Math.Abs(move.Percent) % 10))).ToList();
        await handle.FocusAsync();
        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            foreach (var press in presses)
            {
                await handle.Page.Keyboard.PressAsync(press);
            }

            return presses.Count > 0;
        }, presses.Count);
    }

    private ILocator Separator(string separator) => Root
        .Locator(":scope > .tb-dock__separator, :scope > .tb-dock__slide-separator")
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

    /// <summary>The titles of the panels that show, one for each group, each floating window and the auto-hidden
    /// panel that is slid out, in reading order.
    /// While a group fills the dock, its panel shows and each window's.</summary>
    public async Task<IReadOnlyList<string>> ShowingAsync() =>
        await Root.Locator(":scope > .tb-dock__group:not(.tb-dock__group--covered) .tb-dock__tab[aria-selected=true]")
            .Locator("[data-part=title]").AllTextContentsAsync();

    /// <summary>Clicks the tab of the panel with this title and waits until it shows. Throws for a panel behind
    /// the group that fills the dock, as every helper that clicks does.</summary>
    public async Task SelectAsync(string panel)
    {
        var tab = await ReachableAsync(panel);
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
        var tab = await ReachableAsync(panel);
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
        var tab = await ReachableAsync(panel);
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
            ? throw new ArgumentOutOfRangeException(parameter, side, "Use Start, End, Top or Bottom.")
            : side.ToString().ToLowerInvariant();

    /// <summary>The titles of the panels in floating windows, window by window, oldest window first.</summary>
    public async Task<IReadOnlyList<string>> FloatingAsync() => await Root.Locator(
        ":scope > .tb-dock__group--floating:not(.tb-dock__group--revealed) .tb-dock__tab [data-part=title]")
        .AllTextContentsAsync();

    /// <summary>The titles of the panels put away on the strips along the dock's edges: the top strip's, then those
    /// of the start, the end and the bottom strips.</summary>
    public async Task<IReadOnlyList<string>> AutoHiddenAsync() =>
        await StripButtons.Locator("[data-part=title]").AllTextContentsAsync();

    /// <summary>Puts the panel with this title away on the strip along this edge of the dock, with "Auto-hide" in
    /// its group's menu. Throws for a panel that is on that strip already, and for <c>Before</c> or <c>After</c>.
    /// </summary>
    public async Task AutoHideAsync(string panel, DockPosition edge) =>
        await ChooseAsync(panel, $"[data-move-hide={Side(edge, nameof(edge))}]",
            $"The panel \"{panel}\" is on that strip already.");

    /// <summary>Slides the auto-hidden panel with this title out over the dock, with its button on its strip, and
    /// waits until it is out. A panel that is out already stays out. Throws for a panel that isn't on a strip.
    /// </summary>
    public async Task RevealAsync(string panel)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var button = StripButton(panel);
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException($"The panel \"{panel}\" isn't on a strip.");
        }

        if (await button.GetAttributeAsync("aria-expanded") != "true")
        {
            await Commit.CountedAsync(Root, "data-commits", async () =>
            {
                await button.ClickAsync();
                return true;
            });
        }

        await Assertions.Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
    }

    /// <summary>Slides the slid-out panel back to its strip, with Escape on its tab, and waits until it is back.
    /// Throws when no panel is slid out.</summary>
    public async Task ConcealAsync()
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var tab = Root.Locator(":scope > .tb-dock__group--revealed .tb-dock__tab");
        if (await tab.CountAsync() == 0)
        {
            throw new InvalidOperationException("No auto-hidden panel is slid out.");
        }

        await tab.FocusAsync();
        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            await tab.PressAsync("Escape");
            return true;
        });
        await Assertions.Expect(tab).ToHaveCountAsync(0);
    }

    /// <summary>Takes the panel with this title out into a floating window of its own, with "Float" in its group's
    /// menu. Throws for a panel that has a window to itself already.</summary>
    public async Task FloatAsync(string panel) =>
        await ChooseAsync(panel, "[data-move=float]", $"The panel \"{panel}\" floats already.");

    /// <summary>Docks the floating panel with this title as the showing tab of the largest group, with "Dock" in its
    /// window's menu. Throws for a panel that is docked already.</summary>
    public async Task DockAsync(string panel) =>
        await ChooseAsync(panel, "[data-move=dock]", $"The panel \"{panel}\" is docked already.");

    /// <summary>Opens the panel with this title in a browser window of its own, with "Open in new window" in its
    /// group's menu, and gives the panel in that window. Its <c>Page</c> is the new window's page, where the panel's
    /// content is found as on any page, and closing that page brings the panel back as closing the window does.
    /// Throws when the dock's <c>AllowPopOut</c> is off.</summary>
    public async Task<ILocator> PopOutAsync(string panel)
    {
        var window = await Root.Page.RunAndWaitForPopupAsync(() => ChooseAsync(panel, "[data-move=pop-out]",
            "The dock doesn't open panels in windows of their own. Turn on its AllowPopOut."));
        var popped = window.Locator("[data-part=panel][data-popped]");
        await popped.WaitForAsync();
        return popped;
    }

    /// <summary>The titles of the panels that show in browser windows of their own, in the order their windows
    /// opened.</summary>
    public async Task<IReadOnlyList<string>> PoppedOutAsync()
    {
        var titles = new List<string>();
        await foreach (var popped in PoppedPanelsAsync())
        {
            titles.Add((await popped.GetAttributeAsync("aria-label"))!);
        }

        return titles;
    }

    /// <summary>Brings the panel with this title back to the dock from its own browser window, with that window's
    /// Return button, and waits until the dock has it again. The window closes. Throws for a panel that isn't in a
    /// window of its own.</summary>
    public async Task ReturnAsync(string panel)
    {
        await foreach (var popped in PoppedPanelsAsync())
        {
            if (await popped.GetAttributeAsync("aria-label") == panel)
            {
                await Commit.CountedAsync(Root, "data-commits", async () =>
                {
                    await popped.Locator("[data-part=return]").ClickAsync();
                    return true;
                });
                return;
            }
        }

        throw new InvalidOperationException($"The panel \"{panel}\" isn't in a browser window of its own.");
    }

    // This dock's panels in the browser windows that its page opened, each of which holds one. A dock's panel ids
    // start with the same prefix as the id of its hint for closable tabs, which tells its panels from another dock's.
    private async IAsyncEnumerable<ILocator> PoppedPanelsAsync()
    {
        var hint = (await Root.Locator(":scope > [id$='-close-hint']").GetAttributeAsync("id"))!;
        var own = $"{hint[..^"close-hint".Length]}panel-";
        foreach (var window in Root.Page.Context.Pages)
        {
            var popped = window.Locator($"[data-part=panel][data-popped][id^='{own}']");
            if (!window.IsClosed && await window.OpenerAsync() == Root.Page && await popped.CountAsync() == 1)
            {
                yield return popped;
            }
        }
    }

    /// <summary>The box of the floating window that holds the panel with this title, in percent of the dock: where
    /// it starts, from the side where text starts and from the top, and its width and height. Throws for a panel
    /// that isn't in a window.</summary>
    public async Task<(double X, double Y, double Width, double Height)> WindowAsync(string panel)
    {
        var box = await (await WindowRowAsync(panel)).EvaluateAsync<double[]>(
            "row => ['x', 'y', 'w', 'h'].map(side => Number(row.style.getPropertyValue(`--tb-dock-${side}`)))");
        return (box[0], box[1], box[2], box[3]);
    }

    /// <summary>Moves the window that holds the panel with this title by this many percent of the dock, to the right
    /// and down when the numbers are positive, with the arrow keys on its Move button, and waits until the app has
    /// the layout. The window stays inside the dock. Throws for a panel that isn't in a window.</summary>
    public async Task MoveWindowAsync(string panel, int right, int down)
    {
        var row = await WindowRowAsync(panel);
        await PressAsync(row.Locator(":scope > [data-part=window-move] > [data-part=trigger]"),
            (right < 0 ? "ArrowLeft" : "ArrowRight", right), (down < 0 ? "ArrowUp" : "ArrowDown", down));
    }

    /// <summary>Resizes the window that holds the panel with this title by this many percent of the dock, wider and
    /// taller when the numbers are positive, with the arrow keys on its grip, and waits until the app has the
    /// layout. The window stays inside the dock and no smaller than <c>MinGroupSize</c>. Throws for a panel that
    /// isn't in a window.</summary>
    public async Task ResizeWindowAsync(string panel, int wider, int taller)
    {
        var row = await WindowRowAsync(panel);
        var group = await row.GetAttributeAsync("data-group");
        // The grip is in the corner where the window's box ends, which is on the left where text runs right to left,
        // so there the left arrow widens.
        var rightToLeft = await Root.EvaluateAsync<bool>("dock => getComputedStyle(dock).direction === 'rtl'");
        await PressAsync(Root.Locator($":scope > [data-part=window-grip][data-group='{group}'] > [data-part=trigger]"),
            (wider > 0 != rightToLeft ? "ArrowRight" : "ArrowLeft", wider),
            (taller < 0 ? "ArrowUp" : "ArrowDown", taller));
    }

    /// <summary>Shows the panel with this title and has its group fill the dock, with "Maximize" in its group's
    /// menu. Throws when the group is the dock's only one, or fills the dock already.</summary>
    public async Task MaximizeAsync(string panel) => await ChooseAsync(panel, "[data-move=maximize]",
        $"The group of \"{panel}\" is the dock's only one, or fills the dock already.");

    /// <summary>Shows every group again, with the Restore button of the group that fills the dock, and waits until
    /// the app's <c>LayoutChanged</c> has run. Throws when no group fills the dock.</summary>
    public async Task RestoreAsync()
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var button = Root.Locator(":scope > .tb-dock__group[data-maximized] [data-part=restore]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException("No group fills the dock.");
        }

        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            await button.ClickAsync();
            return true;
        });
    }

    /// <summary>Right-clicks the tab of the panel with this title and clicks the item with this name in the menu
    /// that opens, through the submenus named before it, as in
    /// <c>ChooseFromTabMenuAsync("Notes", "Move to group", "Group 2: Map")</c>. The menu acts on that panel whether or
    /// not it shows. Waits until the app's <c>LayoutChanged</c> has run.</summary>
    public async Task ChooseFromTabMenuAsync(string panel, params string[] path)
    {
        var tab = await ReachableAsync(panel);
        // The tab's group can go as the item is chosen, and its menu with it, so the wait is on the dock's own count.
        await Commit.CountedAsync(Root, "data-commits", async () =>
        {
            await TbContextMenuLocator.Named(scope, null).On(tab).ChooseAsync(path);
            return true;
        });
    }

    /// <summary>The share, in percent, that the part before the separator with this name has of the space it and
    /// the part after it take together. For a slid-out panel's inner edge, which is named like a separator, as in
    /// "Resize Notes", it is the share of the dock the panel takes.</summary>
    public async Task<int> SizeAsync(string separator) => int.Parse(
        await Separator(separator).GetAttributeAsync("aria-valuenow") ?? "", CultureInfo.InvariantCulture);

    /// <summary>Moves the separator with this name with its keys, as a keyboard user would, until the part before it
    /// has this percent of the space it and the part after it take together, and waits until the app has the sizes.
    /// For a slid-out panel's inner edge, the percent is the share of the dock the panel takes. A percent outside the
    /// separator's <c>aria-valuemin</c> and <c>aria-valuemax</c> throws: they are how far <c>MinGroupSize</c> lets it
    /// go at the dock's current size, and for the edge never less than 10 or more than 90. The separator's own value,
    /// if a smaller dock has left it out of reach, counts as a limit until the separator moves.</summary>
    public async Task ResizeAsync(string separator, int percent)
    {
        // The keys work, and the separator states its limits, once the dock's script has attached.
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var bar = Separator(separator);
        var least = int.Parse(await bar.GetAttributeAsync("aria-valuemin") ?? "", CultureInfo.InvariantCulture);
        var most = int.Parse(await bar.GetAttributeAsync("aria-valuemax") ?? "", CultureInfo.InvariantCulture);
        if (percent < least || percent > most)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent,
                $"\"{separator}\" goes from {least}% to {most}% at the dock's current size.");
        }

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
