using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbLink</c>, found by its accessible name.</summary>
public sealed class TbLinkLocator(ILocator scope, ILocator link)
{
    /// <summary>The link, or the button a link without an address renders.</summary>
    public ILocator Link => link.And(scope.Locator(".tb-link"));

    /// <summary>Clicks the link, as a user does, once the page is interactive, and waits until the app has run its
    /// <c>OnClick</c>, until the page the link goes to replaces this one, or until a dialog opens. On a static page, a
    /// link with an address is followed without the wait. Throws for a disabled link, or for a link without an address
    /// on a static page, where it does nothing.</summary>
    public async Task ClickAsync()
    {
        await Link.WaitForAsync();
        if (await Link.GetAttributeAsync("data-static") is not null)
        {
            await ThrowIfDisabledAsync();
            if (await Link.GetAttributeAsync("href") is null)
            {
                throw new InvalidOperationException("On a static page a link without an address does nothing; render "
                    + "the page interactively to use it.");
            }

            await Link.ClickAsync();
            return;
        }

        // A prerendered link is replaced once the page is interactive, and would lose the click's OnClick.
        await Assertions.Expect(Link).ToHaveAttributeAsync("data-clicks", new Regex(@"^\d+$"));
        await ThrowIfDisabledAsync();
        await Commit.ClickedAsync(Link);
    }

    private async Task ThrowIfDisabledAsync()
    {
        if (await Link.IsDisabledAsync() || await Link.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException("The link is disabled, so a user can't click it.");
        }
    }
}
