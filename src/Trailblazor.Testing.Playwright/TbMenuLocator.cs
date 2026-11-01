using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbMenu</c>, found by its trigger's name.</summary>
public sealed class TbMenuLocator(IPage page, string trigger)
{
    private ILocator Trigger => page.GetByRole(AriaRole.Button, new() { Name = trigger, Exact = true })
        .And(page.Locator("[aria-haspopup=menu]"));

    /// <summary>Opens the menu and clicks the item with this name, through the submenus named before it, as in
    /// <c>ChooseAsync("Export", "PDF")</c>. Waits until the menu has closed. Throws for a disabled item. On a
    /// statically rendered page the menu opens natively, and only its links work.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        // The menu opens natively before its script attaches, but its items' clicks reach the server only after.
        if (await Trigger.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        }

        var menu = page.Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
        if (!await menu.IsVisibleAsync())
        {
            await Trigger.ClickAsync();
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
            list = page.Locator($"[id='{await opener.GetAttributeAsync("aria-controls")}']");
        }

        var item = Item(list, path[^1]);
        await ThrowIfDisabledAsync(item, path[^1]);
        await item.ClickAsync();
        await Assertions.Expect(menu).ToBeHiddenAsync();
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
