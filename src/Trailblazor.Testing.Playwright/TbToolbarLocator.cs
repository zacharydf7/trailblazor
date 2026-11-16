using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbToolbar</c>, found by its <c>AriaLabel</c>, or the only one in the scope.</summary>
public sealed class TbToolbarLocator(ILocator scope, string? name)
{
    /// <summary>The toolbar.</summary>
    public ILocator Toolbar => Scope.ByRole(scope, AriaRole.Toolbar, name).And(scope.Locator(".tb-toolbar"));

    /// <summary>Clicks the toolbar's button with this name, as a user does, once the toolbar has fitted its buttons,
    /// and waits until the app's handler has run. A button that moved into the overflow menu is chosen from that menu
    /// instead. Throws for a disabled button, a name
    /// the toolbar doesn't have, or a static page, where the buttons do nothing.</summary>
    public async Task ClickAsync(string button)
    {
        await Toolbar.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        if (await Toolbar.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page the toolbar's buttons do nothing; render the page "
                + "interactively to use them.");
        }

        await Assertions.Expect(Toolbar).ToHaveAttributeAsync("data-ready", "");
        // The overflow menu's button, shown once a button moves into the menu, isn't one of the toolbar's own.
        var shown = Toolbar.GetByRole(AriaRole.Button, new() { Name = button, Exact = true })
            .And(Toolbar.Locator(":not([data-part=overflow] *)"));
        if (await shown.CountAsync() > 0)
        {
            if (await shown.IsDisabledAsync())
            {
                throw new InvalidOperationException($"The toolbar button \"{button}\" is disabled.");
            }

            await Commit.ClickedAsync(shown);
            return;
        }

        var overflow = Toolbar.Locator("[data-part=overflow][data-shown]");
        if (await overflow.CountAsync() == 0)
        {
            throw new InvalidOperationException($"The toolbar has no button named \"{button}\".");
        }

        await new TbMenuLocator(overflow, overflow.Locator("[data-part=trigger]")).ChooseAsync(button);
    }
}
