using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbMenubar</c>, found by its <c>AriaLabel</c>, or the only one in the scope. Its menus are named by
/// their labels.</summary>
public sealed class TbMenubarLocator(ILocator scope, string? name)
{
    /// <summary>The bar.</summary>
    public ILocator Menubar => Scope.ByRole(scope, AriaRole.Menubar, name).And(scope.Locator(".tb-menubar"));

    private ILocator Triggers => Menubar.Locator(":scope > .tb-menu > [data-part=trigger]");

    /// <summary>The names of the bar's menus, in order.</summary>
    public async Task<IReadOnlyList<string>> MenusAsync()
    {
        await Menubar.WaitForAsync();
        return [.. (await Triggers.AllInnerTextsAsync()).Select(text => text.Trim())];
    }

    /// <summary>The bar's menu with this name, to open it or choose its items, as in
    /// <c>Menu("Edit").ChooseAsync("Undo")</c>.</summary>
    public TbMenuLocator Menu(string name) => new(Menubar,
        Menubar.GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true }).And(Triggers));
}
