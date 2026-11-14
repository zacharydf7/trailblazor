using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbContextMenu</c>, found by its menu's <c>AriaLabel</c> or from an element it opens on.</summary>
public sealed class TbContextMenuLocator
{
    private readonly ILocator _scope;
    private readonly ILocator _target;
    private readonly ILocator _at;
    private readonly bool _named;

    internal TbContextMenuLocator(ILocator scope, ILocator target, ILocator at, bool named) =>
        (_scope, _target, _at, _named) = (scope, target, at, named);

    internal static TbContextMenuLocator Named(ILocator scope, string? name)
    {
        var target = name is null ? scope.Locator(".tb-context-menu[data-part=target]")
            : scope.Locator($".tb-context-menu[data-part=target] > [role=menu][aria-label=\"{Css(name)}\"]")
                .Locator("xpath=parent::*");
        return new(scope, target, target, name is not null);
    }

    /// <summary>The same context menu, opened on the element inside it with this <c>data-context-key</c>, such as a
    /// row's id: <c>page.Tb().ContextMenu().On("ridge-loop").ChooseAsync("Delete")</c>.</summary>
    public TbContextMenuLocator On(string key) =>
        new(_scope, _target, _target.Locator($"[data-context-key=\"{Css(key)}\"]").First, _named);

    /// <summary>The same context menu, opened on this element inside it, such as
    /// <c>page.GetByText("Ridge loop")</c>. With no name given, it's the context menu around the element.</summary>
    public TbContextMenuLocator On(ILocator element) => _named ? new(_scope, _target, element, _named)
        : new(_scope, element.Locator("xpath=ancestor-or-self::*[contains(concat(' ', normalize-space(@class), ' '), "
            + "' tb-context-menu ')][1]"), element, _named);

    /// <summary>Right-clicks the element and clicks the menu item with this name, through the submenus named before
    /// it, as in <c>ChooseAsync("Move to", "Archive")</c>. Waits until the menu has closed or, for a <c>KeepOpen</c>
    /// item, until it is checked or unchecked. Throws for a disabled context menu or item, and on a statically rendered
    /// page, where the browser's own menu opens instead.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        if (await _target.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException(
                "This context menu is on a static page, where the browser's own menu opens; render it on an "
                + "interactive page.");
        }

        if (await _target.GetAttributeAsync("data-disabled") is not null)
        {
            throw new InvalidOperationException("This context menu is disabled.");
        }

        await Assertions.Expect(_target).ToHaveAttributeAsync("data-ready", "");
        // The element can render after the area turns ready, as rows the app loads do.
        try
        {
            await Assertions.Expect(_at).Not.ToHaveCountAsync(0);
        }
        catch (PlaywrightException exception)
        {
            throw new InvalidOperationException(
                "The context menu has no element to open on; check the key or element given to On.", exception);
        }

        var menu = _target.Locator(":scope > [role=menu]");
        // A data-open left from the last opening would pass the wait below before this opening's items are drawn.
        if (await menu.IsVisibleAsync())
        {
            await menu.PressAsync("Escape");
            await Assertions.Expect(menu).ToBeHiddenAsync();
        }

        await Assertions.Expect(menu).Not.ToHaveAttributeAsync("data-open", "");
        await _at.ClickAsync(new() { Button = MouseButton.Right });
        await Assertions.Expect(menu).ToHaveAttributeAsync("data-open", "");
        await TbMenuLocator.ChooseFromAsync(_scope, menu, path);
    }

    private static string Css(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
