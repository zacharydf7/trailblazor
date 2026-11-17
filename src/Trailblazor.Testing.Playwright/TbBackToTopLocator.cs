using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The page's <c>TbBackToTop</c>.</summary>
public sealed class TbBackToTopLocator(ILocator scope)
{
    /// <summary>The button's root, which has <c>data-shown</c> while the button shows.</summary>
    public ILocator BackToTop => scope.Locator(".tb-back-to-top");

    /// <summary>The button.</summary>
    public ILocator Button => BackToTop.Locator("[data-part=button]");

    /// <summary>Whether the button shows, which it does once the page has scrolled past its threshold.</summary>
    public async Task<bool> IsShownAsync() => await BackToTop.GetAttributeAsync("data-shown") is not null;

    /// <summary>Presses the button once it shows, and waits until the app has handled the press.</summary>
    public async Task ClickAsync()
    {
        await Assertions.Expect(BackToTop).ToHaveAttributeAsync("data-shown", "");
        await Commit.ClickedAsync(Button);
    }
}
