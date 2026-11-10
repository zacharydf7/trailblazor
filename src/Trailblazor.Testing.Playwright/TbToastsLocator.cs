using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The toasts on a page, from <c>IToastService</c>.</summary>
public sealed class TbToastsLocator(ILocator scope)
{
    /// <summary>Waits for a toast whose text contains this and returns it, to read it or click its action. When
    /// several match, as when the same action ran twice, this returns the newest.</summary>
    public async Task<ILocator> WaitForAsync(string text)
    {
        // A toast that is leaving stays on the page while it fades; newer toasts come later on the page.
        var toast = scope.Locator("[data-part=toast]:not([data-leaving])").Filter(new() { HasText = text }).Last;
        await Assertions.Expect(toast).ToBeVisibleAsync();
        return toast;
    }
}
