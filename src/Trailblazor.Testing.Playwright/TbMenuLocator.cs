using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbMenu</c>, found by its trigger's name.</summary>
public sealed class TbMenuLocator(ILocator scope, string trigger)
{
    private ILocator Trigger => scope.GetByRole(AriaRole.Button, new() { Name = trigger, Exact = true })
        .And(scope.Locator("[aria-haspopup=menu]"));

    /// <summary>Opens the menu and clicks the item with this name, through the submenus named before it, as in
    /// <c>ChooseAsync("Export", "PDF")</c>. Waits until the menu has closed or, for a <c>KeepOpen</c> item, until it
    /// is checked or unchecked. Throws for a disabled item. On a statically rendered page the menu opens natively, and
    /// only its links work.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        // The menu opens natively before its script attaches, but its items' clicks reach the server only after.
        var scripted = await Trigger.GetAttributeAsync("data-static") is null;
        if (scripted)
        {
            await Scope.ReadyAsync(scope, Trigger);
        }

        var menu = Scope.Document(scope).Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
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
        if (await item.GetAttributeAsync("data-keep-open") is null)
        {
            await item.ClickAsync();
            await Assertions.Expect(menu).ToBeHiddenAsync();
            return;
        }

        // A radio item is checked once chosen, and a checkbox item toggles.
        var radio = await item.GetAttributeAsync("role") == "menuitemradio";
        var checkedAfter = radio || await item.GetAttributeAsync("aria-checked") != "true" ? "true" : "false";
        await item.ClickAsync();
        await Assertions.Expect(item).ToHaveAttributeAsync("aria-checked", checkedAfter);
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
