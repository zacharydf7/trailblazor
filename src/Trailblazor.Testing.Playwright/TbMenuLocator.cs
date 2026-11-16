using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbMenu</c>, found by its trigger's name or a locator.</summary>
public sealed class TbMenuLocator(ILocator scope, ILocator trigger)
{
    private ILocator Trigger => trigger.And(scope.Locator("[aria-haspopup=menu]"));

    /// <summary>Opens the menu and clicks the item with this name, through the submenus named before it, as in
    /// <c>ChooseAsync("Export", "PDF")</c>. Waits until the app has run the item's handler, or until a dialog opens,
    /// in case the handler awaits the dialog's answer. Throws for a disabled item. On a statically rendered page the
    /// menu opens natively, and only its links work.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        await ChooseFromAsync(scope, await OpenMenuAsync(), path);
    }

    /// <summary>Opens the menu, as clicking its trigger does, and waits until its items show. An open menu is left as
    /// it is. On a statically rendered page the menu opens natively.</summary>
    public Task OpenAsync() => OpenMenuAsync();

    /// <summary>Closes the menu by pressing Escape once for each open submenu and once for the menu, then waits until
    /// it's hidden. A closed menu is left as it is.</summary>
    public async Task CloseAsync()
    {
        var menu = await MenuAsync();
        // Each Escape closes the topmost open list, so it stops as soon as none is open: once the menu closes, focus is
        // back on the page and another Escape would reach it.
        const string OpenLists =
            "menu => menu.matches(':popover-open') + menu.querySelectorAll(':popover-open').length";
        for (var open = await menu.EvaluateAsync<int>(OpenLists); open > 0; open--)
        {
            if (await menu.EvaluateAsync<int>(OpenLists) == 0)
            {
                break;
            }

            await scope.Page.Keyboard.PressAsync("Escape");
        }

        await Assertions.Expect(menu).ToBeHiddenAsync();
    }

    /// <summary>Whether the checkbox or radio item with this name, through the submenus named before it, is checked.
    /// The items are on the page whether the menu is open or not, so this reads it without opening the menu.</summary>
    public async Task<bool> CheckedAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var list = await MenuAsync();
        foreach (var name in path[..^1])
        {
            var opener = await OwnAsync(list, Hidden(list, AriaRole.Menuitem, name)
                .And(list.Locator("[aria-haspopup=menu]")), name, "submenu");
            list = Scope.Document(scope).Locator($"[id='{await opener.GetAttributeAsync("aria-controls")}']");
        }

        var item = await OwnAsync(list, Hidden(list, AriaRole.Menuitemcheckbox, path[^1])
            .Or(Hidden(list, AriaRole.Menuitemradio, path[^1])), path[^1], "checkbox or radio item");
        return await item.GetAttributeAsync("aria-checked") == "true";
    }

    // An item in a closed list, which is hidden until the list opens.
    private static ILocator Hidden(ILocator list, AriaRole role, string name) =>
        list.GetByRole(role, new() { Name = name, Exact = true, IncludeHidden = true });

    // The one candidate whose nearest menu is this list, since a submenu's items sit inside the list that holds it.
    private static async Task<ILocator> OwnAsync(ILocator list, ILocator candidates, string name, string kind)
    {
        var id = await list.GetAttributeAsync("id");
        var own = new List<ILocator>();
        foreach (var candidate in await candidates.AllAsync())
        {
            if (await candidate.EvaluateAsync<string?>("item => item.parentElement.closest('[role=menu]')?.id") == id)
            {
                own.Add(candidate);
            }
        }

        return own.Count switch
        {
            1 => own[0],
            0 => throw new InvalidOperationException($"The menu has no {kind} named \"{name}\"."),
            _ => throw new InvalidOperationException($"The menu has {own.Count} {kind}s named \"{name}\"."),
        };
    }

    /// <summary>Whether the menu is showing.</summary>
    public async Task<bool> IsOpenAsync() => await (await MenuAsync()).IsVisibleAsync();

    private async Task<ILocator> MenuAsync() =>
        Scope.Document(scope).Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");

    private async Task<ILocator> OpenMenuAsync()
    {
        // The menu opens natively before its script attaches, but its items' clicks reach the server only after.
        var scripted = await Trigger.GetAttributeAsync("data-static") is null;
        if (scripted)
        {
            await Scope.ReadyAsync(scope, Trigger);
        }

        var menu = await MenuAsync();
        if (!await menu.IsVisibleAsync())
        {
            // A data-open left from the last open would pass the wait below before this open's items are drawn.
            if (scripted)
            {
                await Assertions.Expect(menu).Not.ToHaveAttributeAsync("data-open", "");
            }

            await Trigger.ClickAsync();
        }

        // The menu redraws its items as it opens and sets data-open with them; a static menu has no script to set it.
        if (scripted)
        {
            await Assertions.Expect(menu).ToHaveAttributeAsync("data-open", "");
        }

        return menu;
    }

    // Clicks the item at the end of the path in an open menu, opening the submenus named before it.
    internal static async Task ChooseFromAsync(ILocator scope, ILocator menu, string[] path)
    {
        var list = menu;
        foreach (var name in path[..^1])
        {
            var opener = list.GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true })
                .And(list.Locator("[aria-haspopup=menu]"));
            await ThrowIfDisabledAsync(opener, name);
            if (await opener.GetAttributeAsync("aria-expanded") != "true")
            {
                await opener.ClickAsync();
            }

            await Assertions.Expect(opener).ToHaveAttributeAsync("aria-expanded", "true");
            list = Scope.Document(scope).Locator($"[id='{await opener.GetAttributeAsync("aria-controls")}']");
        }

        var item = Item(list, path[^1]);
        await ThrowIfDisabledAsync(item, path[^1]);
        // The menu closes in the browser as the item is clicked, before the app's handler has run, so the wait follows
        // the menu's count of finished handlers.
        await Commit.CountedAsync(menu, "data-commits", async () =>
        {
            await item.ClickAsync();
            return true;
        });
    }

    // A checkbox or radio item is chosen the same way as a plain one.
    private static ILocator Item(ILocator list, string name) =>
        list.GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true })
            .Or(list.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = name, Exact = true }))
            .Or(list.GetByRole(AriaRole.Menuitemradio, new() { Name = name, Exact = true }));

    private static async Task ThrowIfDisabledAsync(ILocator item, string name)
    {
        if (await item.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The menu item \"{name}\" is disabled.");
        }
    }
}
