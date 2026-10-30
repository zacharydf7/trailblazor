namespace Trailblazor;

// Icons the components draw themselves, so they work without an icon pack.
internal static class BuiltInIcons
{
    public static Icon Close { get; } =
        new("<path d=\"M6.4 5 5 6.4 10.6 12 5 17.6 6.4 19l5.6-5.6 5.6 5.6 1.4-1.4-5.6-5.6L19 6.4 17.6 5 12 10.6z\" />");

    public static Icon Calendar { get; } =
        new("<path d=\"M7 2h2v2h6V2h2v2h3v18H4V4h3zM6 10v10h12V10zm0-2h12V6H6zm2 4h3v3H8z\" />");

    public static Icon Palette { get; } = new("<path d=\"M12 3a9 9 0 1 0 0 18 2 2 0 0 0 1.5-3.3 2 2 0 0 1 "
        + "1.5-3.3h2.2A4.8 4.8 0 0 0 22 9.6C21.4 5.8 17.2 3 12 3zM7 12.5a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm3-4a1.5 "
        + "1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm4.5 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm3 4a1.5 1.5 0 1 1 0-3 1.5 1.5 0 "
        + "0 1 0 3z\" />");

    public static Icon Eyedropper { get; } = new("<path d=\"M19.6 4.4a2.2 2.2 0 0 0-3.1 0l-2.4 2.4-1.4-1.4-1.4 "
        + "1.4 1.4 1.4L5 15.9V19h3.1l7.6-7.7 1.4 1.4 1.4-1.4-1.4-1.4 2.5-2.4a2.2 2.2 0 0 0 0-3.1zM7.3 17H7v-.3l7.3-7.3"
        + ".3.3z\" />");

    public static Icon ChevronDown { get; } = new("<path d=\"M7.4 8.6 12 13.2l4.6-4.6L18 10l-6 6-6-6z\" />");

    // Points right; flip it in right-to-left text.
    public static Icon ChevronEnd { get; } = new("<path d=\"M8.6 16.6 13.2 12 8.6 7.4 10 6l6 6-6 6z\" />");

    // Points left; flip it in right-to-left text.
    public static Icon ChevronStart { get; } = new("<path d=\"M15.4 7.4 10.8 12l4.6 4.6L14 18l-6-6 6-6z\" />");

    // A chevron against a bar, pointing left and right; flip them in right-to-left text.
    public static Icon PageFirst { get; } = new("<path d=\"M18.4 16.6 13.8 12l4.6-4.6L17 6l-6 6 6 6zM6 6h2v12H6z\" />");

    public static Icon PageLast { get; } = new("<path d=\"M5.6 7.4 10.2 12l-4.6 4.6L7 18l6-6-6-6zM16 6h2v12h-2z\" />");

    public static Icon Minus { get; } = new("<path d=\"M5 11h14v2H5z\" />");

    public static Icon Check { get; } = new("<path d=\"M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z\" />");

    public static Icon Info { get; } = new("<path d=\"M11 7h2v2h-2zm0 4h2v6h-2zm1-9a10 10 0 1 0 0 20 10 10 0 0 0 0-20z"
        + "m0 18a8 8 0 1 1 0-16 8 8 0 0 1 0 16z\" />");

    public static Icon Maximize { get; } = new("<path d=\"M4 4h16v16H4zm2 2v12h12V6z\" />");

    public static Icon Restore { get; } = new("<path d=\"M8 4h12v12h-2V6H8zM4 8h12v12H4zm2 2v8h8v-8z\" />");

    public static Icon Plus { get; } = new("<path d=\"M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z\" />");

    public static Icon More { get; } = new("<path d=\"M6 10a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 "
        + "0-4zm6 0a2 2 0 1 0 0 4 2 2 0 0 0 0-4z\" />");

    public static Icon Move { get; } = new("<path d=\"M12 2 8.5 5.5 9.9 6.9 11 5.8V11H5.8l1.1-1.1-1.4-1.4L2 12l3.5 "
        + "3.5 1.4-1.4L5.8 13H11v5.2l-1.1-1.1-1.4 1.4L12 22l3.5-3.5-1.4-1.4-1.1 1.1V13h5.2l-1.1 1.1 1.4 1.4L22 12l-3.5"
        + "-3.5-1.4 1.4 1.1 1.1H13V5.8l1.1 1.1 1.4-1.4z\" />");

    // Dots toward the bottom-right corner; flip it in right-to-left text.
    public static Icon ResizeGrip { get; } = new("<path d=\"M18 18h2v2h-2zm0-4h2v2h-2zm-4 4h2v2h-2zm0-4h2v2h-2zm4-4h2"
        + "v2h-2zm-8 8h2v2h-2z\" />");
}
