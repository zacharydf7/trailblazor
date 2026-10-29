namespace Trailblazor;

// Icons the components draw themselves, so they work without an icon pack.
internal static class BuiltInIcons
{
    public static Icon Close { get; } =
        new("<path d=\"M6.4 5 5 6.4 10.6 12 5 17.6 6.4 19l5.6-5.6 5.6 5.6 1.4-1.4-5.6-5.6L19 6.4 17.6 5 12 10.6z\" />");

    public static Icon ChevronDown { get; } = new("<path d=\"M7.4 8.6 12 13.2l4.6-4.6L18 10l-6 6-6-6z\" />");

    public static Icon Minus { get; } = new("<path d=\"M5 11h14v2H5z\" />");

    public static Icon Plus { get; } = new("<path d=\"M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z\" />");
}
