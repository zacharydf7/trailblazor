using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbOverlay</c>, found by its <c>aria-label</c> or as the only one open.</summary>
public sealed class TbOverlayLocator(ILocator scope, string? name)
{
    /// <summary>The overlay, which is on the page only while it is open.</summary>
    public ILocator Overlay => name is null ? scope.Locator(".tb-overlay")
        : scope.GetByLabel(name, new() { Exact = true }).And(scope.Locator(".tb-overlay"));

    /// <summary>Whether the overlay is open, which is when it is on the page.</summary>
    public async Task<bool> IsOpenAsync() => await Overlay.CountAsync() > 0;

    /// <summary>Clicks the overlay outside its content, as a user does, once the page is interactive. That raises its
    /// <c>OnClick</c> and closes an overlay with <c>CloseOnClick</c>, and the call waits until the <c>OnClick</c> has
    /// run and, with <c>CloseOnClick</c>, the overlay has closed. It throws for a closed overlay, or one on a static
    /// page.</summary>
    public async Task ClickAsync()
    {
        var overlay = await OpenOverlayAsync();
        await Commit.CountedAsync(overlay, "data-clicks", async () =>
        {
            // A corner of the scrim, which the content in the middle doesn't cover.
            await overlay.Locator("[data-part=scrim]").ClickAsync(new() { Position = new() { X = 2, Y = 2 } });
            return true;
        });
    }

    /// <summary>Presses Escape, which closes an overlay with <c>CloseOnClick</c>, and waits until it has closed. It
    /// throws for a closed overlay; for one that Escape doesn't close, the wait for its Escape handling times out.
    /// </summary>
    public async Task PressEscapeAsync()
    {
        var overlay = await OpenOverlayAsync();
        // The layer script marks the overlay ready once Escape is wired to close it, which it is only with
        // CloseOnClick.
        await Assertions.Expect(overlay).ToHaveAttributeAsync("data-ready", "");
        await overlay.Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(overlay).ToHaveCountAsync(0);
    }

    // Waits for an overlay the app has just opened, and for a prerendered one to become interactive, since the page
    // replaces it then and a click on the prerendered copy would be lost.
    private async Task<ILocator> OpenOverlayAsync()
    {
        await Assertions.Expect(Overlay.First).ToBeAttachedAsync();
        var count = await Overlay.CountAsync();
        if (count > 1)
        {
            throw new InvalidOperationException($"{count} overlays are open; name the one to use with its aria-label.");
        }

        if (await Overlay.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page the overlay does nothing; render the page "
                + "interactively to use it.");
        }

        await Assertions.Expect(Overlay).ToHaveAttributeAsync("data-clicks", new Regex(@"^\d+$"));
        return Overlay;
    }
}
