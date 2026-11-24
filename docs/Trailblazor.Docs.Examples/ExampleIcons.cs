namespace Trailblazor.Docs.Examples;

// Simple shapes drawn for the examples, since the docs site loads the Material icon pack only on its icon pages.
public static class ExampleIcons
{
    public static Icon Plus { get; } = new("<path d=\"M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z\" />");

    public static Icon Gear { get; } = new("<path fill-rule=\"evenodd\" d=\""
        + "M10.5 2h3l.5 2.6 1.7.7 2.2-1.5 2.1 2.1-1.5 2.2.7 1.7 2.6.5v3l-2.6.5-.7 1.7 1.5 2.2-2.1 "
        + "2.1-2.2-1.5-1.7.7-.5 2.6h-3l-.5-2.6-1.7-.7-2.2 1.5-2.1-2.1 "
        + "1.5-2.2-.7-1.7-2.6-.5v-3l2.6-.5.7-1.7-1.5-2.2 2.1-2.1 2.2 1.5 1.7-.7zM12 8.5a3.5 3.5 0 1 0 "
        + "0 7 3.5 3.5 0 0 0 0-7z\" />");

    public static Icon More { get; } =
        new("<circle cx=\"12\" cy=\"5\" r=\"2\" /><circle cx=\"12\" cy=\"12\" r=\"2\" /><circle cx=\"12\" "
            + "cy=\"19\" r=\"2\" />");

    public static Icon Check { get; } = new("<path d=\"M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z\" />");

    public static Icon Close { get; } =
        new("<path d=\"M6.4 5 5 6.4 10.6 12 5 17.6 6.4 19l5.6-5.6 5.6 5.6 1.4-1.4-5.6-5.6L19 6.4 17.6 5 12 10.6z\" />");

    public static Icon Trash { get; } = new("<path d=\"M9 3h6l1 2h4v2H4V5h4zM6 9h12l-1 12H7z\" />");

    public static Icon Bell { get; } =
        new("<path d=\"M12 22a2 2 0 0 0 2-2h-4a2 2 0 0 0 2 2z"
            + "m6-6v-5a6 6 0 0 0-5-5.9V4h-2v1.1A6 6 0 0 0 6 11v5l-2 2v1h16v-1z\" />");

    public static Icon Search { get; } = new("<path d=\"M10 3a7 7 0 0 1 5.6 11.2l5.1 5.1-1.4 1.4-5.1-5.1A7 7 0 1 1 10 3zm0 2a5 5 0 1 0 0 10 5 5 0 0 0 0-10z\" />");

    public static Icon Filter { get; } = new("<path d=\"M3 5h18l-7 8v6l-4 2v-8z\" />");

    public static Icon Folder { get; } = new("<path d=\"M3 5h7l2 2h9v12H3z\" />");

    public static Icon Trail { get; } = new("<path d=\"M12 3 4 21h3l5-11 5 11h3z\" />");

    public static Icon Minus { get; } = new("<path d=\"M5 11h14v2H5z\" />");

    public static Icon ArrowUp { get; } = new("<path d=\"M11 20V7.8l-5.6 5.6L4 12l8-8 8 8-1.4 1.4L13 7.8V20z\" />");

    public static Icon ChevronDown { get; } = new("<path d=\"M7.4 8.6 12 13.2l4.6-4.6L18 10l-6 6-6-6z\" />");
}
