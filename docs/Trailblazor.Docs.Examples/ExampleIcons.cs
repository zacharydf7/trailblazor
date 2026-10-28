namespace Trailblazor.Docs.Examples;

// Simple shapes drawn for the examples until the Material icon pack exists.
public static class ExampleIcons
{
    public static Icon Plus { get; } = new("<path d=\"M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z\" />");

    public static Icon Check { get; } = new("<path d=\"M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z\" />");

    public static Icon Close { get; } =
        new("<path d=\"M6.4 5 5 6.4 10.6 12 5 17.6 6.4 19l5.6-5.6 5.6 5.6 1.4-1.4-5.6-5.6L19 6.4 17.6 5 12 10.6z\" />");

    public static Icon Trash { get; } = new("<path d=\"M9 3h6l1 2h4v2H4V5h4zM6 9h12l-1 12H7z\" />");
}
