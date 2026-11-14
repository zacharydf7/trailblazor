namespace Trailblazor.Docs.Examples;

public sealed record Guide(string Path, string Title, string Summary)
{
    /// <summary>The types the guide shows an API table for, so a cross-reference elsewhere can link to
    /// them.</summary>
    public IReadOnlyList<Type> Api { get; init; } = [];

    /// <summary>Whether it's a recipe, which the site lists apart from the guides.</summary>
    public bool IsRecipe => Path.StartsWith("recipes/", StringComparison.Ordinal);
}

// The docs site's guides, in the order it lists them.
public static class GuideCatalog
{
    public static IReadOnlyList<Guide> All { get; } =
    [
        new("guides/getting-started", "Getting started",
            "Add Trailblazor to an app, check that it works, and choose a render mode."),
        new("guides/forms", "Forms and validation",
            "Check what people enter, show server errors, list errors in a summary, and word every message well."),
        new("guides/accessibility", "Accessibility",
            "What components do for assistive technology, what your app must do, and what has and hasn't been tested."),
        new("guides/theming", "Theming", "Light and dark, palettes, density, and changing any design token."),
        new("guides/palettes", "Palettes", "Every built-in palette on real components, in light and dark."),
        new("guides/spacing", "Spacing",
            "Pad, space and push apart your own markup with tb-* classes on the components' spacing scale."),
        new("guides/browser", "Browser",
            "Copy to the clipboard, download and save files, pick colors, scroll, and follow the viewport from C#.")
        {
            Api = [typeof(IBrowserService)],
        },
        new("guides/announcements", "Announcements",
            "Tell screen reader users about changes they can't see, and clear the messages once heard.")
        {
            Api = [typeof(IAnnouncer)],
        },
        new("guides/rich-text", "Rich text",
            "Clean up HTML from anywhere, show it safely, and read its blocks, formats and links from C#.")
        {
            Api = [typeof(RichTextDocument), typeof(RichTextParagraph), typeof(RichTextRun), typeof(RichTextSchema)],
        },
        new("guides/icons", "Icons", "Draw Material Symbols or your own SVG icons, and find the one you need."),
        new("guides/localization", "Localization",
            "Change the words components say and show, or translate them all for each user's culture.")
        {
            Api = [typeof(ITbLocalizer)],
        },
        new("guides/right-to-left", "Right-to-left and culture",
            "Mirror layouts and arrow keys for right-to-left languages, and show numbers and dates in each culture."),
        new("guides/testing", "Testing",
            "Test pages built with Trailblazor in bUnit and Playwright, and find components the way users do."),
        new("guides/migrating-tests", "Migrating tests",
            "Move tests over from another component library: what replaces its providers, mocks, state reads and "
                + "class selectors."),
        new("guides/wrapping", "Wrapping components",
            "Give a component your app's defaults in a wrapper of your own, and pass everything else through."),
        new("guides/analyzers", "Analyzers",
            "As your app builds, catch an unnamed control or field, an untitled dialog, or a value that snaps back "
                + "after the user changes it."),
        new("guides/telemetry", "Telemetry",
            "See which features people use and which they never touch, in the dashboard you already have.")
        {
            Api = [typeof(TelemetrySettings), typeof(ITelemetryTags), typeof(TelemetryCatalogEntryAttribute)],
        },
        new("recipes/filter-and-grid", "A filter bar with a data grid",
            "Narrow a long list from a server, with fields above a grid that loads a page at a time."),
        new("recipes/grid-from-database", "A data grid from a database",
            "Turn what a grid asks for into an EF Core query, and reload the grid after the data changes."),
        new("recipes/edit-in-dialog", "Edit in a dialog",
            "Open a form in a dialog from a row, and save what comes back."),
        new("recipes/confirm-and-delete", "Confirm and delete",
            "Ask before deleting, offer an undo, and keep focus somewhere useful afterward."),
        new("recipes/settings-page", "A settings page", "Split a long form into tabs that save together."),
        new("recipes/master-detail", "Master and detail", "Pick an item from a tree and read about it beside the tree."),
        new("recipes/sign-up", "A sign-up form",
            "Collect a few required answers, check them, and confirm what happened."),
    ];
}
