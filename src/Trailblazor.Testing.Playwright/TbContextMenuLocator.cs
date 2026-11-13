using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The <c>TbContextMenu</c> that opens on an element, found from the element.</summary>
public sealed class TbContextMenuLocator(ILocator scope, ILocator at)
{
    // The nearest context menu around the element, as a nested one handles its own area.
    private ILocator Target => at.Locator(
        "xpath=ancestor-or-self::*[contains(concat(' ', normalize-space(@class), ' '), ' tb-context-menu ')][1]");

    /// <summary>Right-clicks the element and clicks the menu item with this name, through the submenus named before
    /// it, as in <c>ChooseAsync("Move to", "Archive")</c>. Waits until the menu has closed or, for a <c>KeepOpen</c>
    /// item, until it is checked or unchecked. Throws for a disabled context menu or item, and on a statically rendered
    /// page, where the browser's own menu opens instead.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        if (await Target.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException(
                "This context menu is on a static page, where the browser's own menu opens; render it on an "
                + "interactive page.");
        }

        if (await Target.GetAttributeAsync("data-disabled") is not null)
        {
            throw new InvalidOperationException("This context menu is disabled.");
        }

        await Assertions.Expect(Target).ToHaveAttributeAsync("data-ready", "");
        var menu = Target.Locator(":scope > [role=menu]");
        // A data-open left from the last opening would pass the wait below before this opening's items are drawn.
        if (await menu.IsVisibleAsync())
        {
            await menu.PressAsync("Escape");
            await Assertions.Expect(menu).ToBeHiddenAsync();
        }

        await Assertions.Expect(menu).Not.ToHaveAttributeAsync("data-open", "");
        await at.ClickAsync(new() { Button = MouseButton.Right });
        await Assertions.Expect(menu).ToHaveAttributeAsync("data-open", "");
        await TbMenuLocator.ChooseFromAsync(scope, menu, path);
    }
}
