using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbInsertDivider</c>, found by its <c>Label</c>.</summary>
public sealed class TbInsertDividerLocator(ILocator scope, string label)
{
    /// <summary>The divider.</summary>
    public ILocator Divider => scope.Locator(".tb-insert-divider").Filter(new()
    {
        Has = scope.Page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true }),
    });

    /// <summary>Presses the insert button, which raises <c>OnInsert</c>, once the page is interactive. Throws for a
    /// disabled divider, or on a static page, where the button does nothing.</summary>
    public async Task InsertAsync()
    {
        await Divider.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        if (await Divider.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page the insert button does nothing; render the page "
                + "interactively to use it.");
        }

        await Assertions.Expect(Divider).ToHaveAttributeAsync("data-ready", "");
        var insert = Divider.Locator("[data-part=insert]");
        if (await insert.IsDisabledAsync())
        {
            throw new InvalidOperationException("This insert divider is disabled.");
        }

        await insert.ClickAsync();
    }
}
