namespace Trailblazor;

// The toolbar's letters are drawn as lines, since visible <text> that a button's name lacks fails WCAG 2.5.3.
internal static class RichTextEditorIcons
{
    private const string Lines = "fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";

    public static Icon Bold { get; } =
        new($"<path {Lines} stroke-width=\"2.8\" d=\"M7 5h6a3.5 3.5 0 0 1 0 7H7zM7 12h7a3.5 3.5 0 0 1 0 7H7z\" />");

    public static Icon Italic { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M10 5h8M6 19h8M15 5 9 19\" />");

    public static Icon Underline { get; } =
        new($"<path {Lines} stroke-width=\"2.2\" d=\"M7 4v7a5 5 0 0 0 10 0V4M6 20.5h12\" />");

    public static Icon Strikethrough { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M16.5 7.5C15.7 5.9 14 5 12 5 "
        + "9.5 5 7.5 6.4 7.5 8.5c0 4.5 9.5 2.5 9.5 7 0 2.1-2 3.5-4.7 3.5-2.1 0-3.9-1-4.8-2.6M4 12h16\" />");

    public static Icon Subscript { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M4 6l8 10M12 6l-8 10\" />"
        + $"<path {Lines} stroke-width=\"1.5\" d=\"M15 15.5a1.75 1.75 0 1 1 3.2 1c-.6.8-3.2 3-3.2 3h3.5\" />");

    public static Icon Superscript { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M4 8l8 10M12 8l-8 10\" />"
        + $"<path {Lines} stroke-width=\"1.5\" d=\"M15 4.5a1.75 1.75 0 1 1 3.2 1c-.6.8-3.2 3-3.2 3h3.5\" />");
}
