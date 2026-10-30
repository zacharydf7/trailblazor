namespace Trailblazor.Docs.Examples;

public sealed record Guide(string Path, string Title, string Summary);

// The docs site's guides, in the order it lists them.
public static class GuideCatalog
{
    public static IReadOnlyList<Guide> All { get; } =
    [
        new("guides/getting-started", "Getting started",
            "Add Trailblazor to an app, render your first component, and choose a render mode."),
        new("guides/theming", "Theming", "Light and dark, palettes, density, and changing any design token."),
        new("guides/palettes", "Palettes", "Every built-in palette on real components, in light and dark."),
        new("guides/browser", "Browser",
            "Copy to the clipboard, download and save files, pick colors, scroll, and follow the viewport from C#."),
        new("guides/announcements", "Announcements",
            "Tell screen reader users about changes they can't see, and clear the messages once heard."),
        new("guides/testing", "Testing",
            "Test pages built with Trailblazor in bUnit and Playwright, and find components the way users do."),
        new("roadmap", "Roadmap", "What has shipped, what is being built now, and which components come next."),
    ];
}
