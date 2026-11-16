using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbAlert</c>, found by its text.</summary>
public sealed class TbAlertLocator(ILocator scope, string text)
{
    /// <summary>The alert.</summary>
    public ILocator Alert => scope.Locator(".tb-alert").Filter(new() { HasText = text });

    /// <summary>The alert's message, without its title, with runs of whitespace collapsed.</summary>
    public Task<string> TextAsync() => Notices.MessageAsync(Alert);

    /// <summary>Waits until the page is interactive, then presses the close button, which raises <c>OnClose</c>. Only
    /// an alert with an <c>OnClose</c> has the button, so this throws for one without, and on a static page, where the
    /// button does nothing.</summary>
    public Task CloseAsync() => Notices.CloseAsync(Alert);
}

/// <summary>A <c>TbBanner</c>, found by its name.</summary>
public sealed class TbBannerLocator(ILocator scope, string name)
{
    /// <summary>The banner's region.</summary>
    public ILocator Banner => scope.GetByRole(AriaRole.Region, new() { Name = name, Exact = true })
        .And(scope.Locator("section.tb-banner"));

    /// <summary>The banner's message, without its title, with runs of whitespace collapsed.</summary>
    public Task<string> TextAsync() => Notices.MessageAsync(Banner);

    /// <summary>Waits until the page is interactive, then presses the close button, which raises <c>OnClose</c>. Only
    /// a banner with an <c>OnClose</c> has the button, so this throws for one without, and on a static page, where the
    /// button does nothing.</summary>
    public Task CloseAsync() => Notices.CloseAsync(Banner);
}

internal static class Notices
{
    // A banner's title sits inside its message part, so the text is read from a copy with the title removed.
    private const string Message = "notice => { const message = notice.querySelector('[data-part=message]')"
        + ".cloneNode(true); message.querySelector('[data-part=title]')?.remove(); return message.textContent; }";

    public static async Task<string> MessageAsync(ILocator notice) =>
        Regex.Replace(await notice.EvaluateAsync<string>(Message), @"\s+", " ").Trim();

    public static async Task CloseAsync(ILocator notice)
    {
        if (await notice.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page the close button does nothing; render the page "
                + "interactively to close it.");
        }

        var close = notice.Locator("[data-part=close]");
        if (await close.CountAsync() == 0)
        {
            throw new InvalidOperationException("There's no close button; it shows only when OnClose is set.");
        }

        // A prerendered notice is replaced once the page is interactive, and its button would lose the click.
        await Assertions.Expect(notice).ToHaveAttributeAsync("data-ready", "");
        await Commit.ClickedAsync(close);
    }
}
