namespace Trailblazor;

// Icons the components draw themselves, so they work without an icon pack.
internal static class BuiltInIcons
{
    public static Icon Close { get; } =
        new("<path d=\"M6.4 5 5 6.4 10.6 12 5 17.6 6.4 19l5.6-5.6 5.6 5.6 1.4-1.4-5.6-5.6L19 6.4 17.6 5 12 10.6z\" />");
}
