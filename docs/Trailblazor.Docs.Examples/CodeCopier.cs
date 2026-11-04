namespace Trailblazor.Docs.Examples;

// Copies code or a link and says how it went, naming what it copied, since the browser copies silently. Null code
// means the text couldn't be fetched, such as a page that didn't load.
public static class CodeCopier
{
    public static async Task CopyAsync(IBrowserService browser, IAnnouncer announcer, string label, string? code) =>
        announcer.Polite(code is not null && await browser.CopyTextAsync(code)
            ? $"Copied {label}." : $"Couldn't copy {label}.");
}
