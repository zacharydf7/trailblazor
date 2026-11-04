namespace Trailblazor.Docs.Examples;

// Copies code or a link and says how it went, naming what it copied, since the browser copies silently.
public static class CodeCopier
{
    public static async Task CopyAsync(IBrowserService browser, IAnnouncer announcer, string label, string code) =>
        announcer.Polite(await browser.CopyTextAsync(code) ? $"Copied {label}." : $"Couldn't copy {label}.");
}
