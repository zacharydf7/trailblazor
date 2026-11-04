namespace Trailblazor.Docs.Examples;

// Copies a block of code and says how it went, naming the file or snippet, since the browser copies silently.
public static class CodeCopier
{
    public static async Task CopyAsync(IBrowserService browser, IAnnouncer announcer, string label, string code) =>
        announcer.Polite(await browser.CopyTextAsync(code) ? $"Copied {label}." : $"Couldn't copy {label}.");
}
