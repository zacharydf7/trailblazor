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

    public static Icon Code { get; } =
        new($"<path {Lines} stroke-width=\"2\" d=\"M8 7l-5 5 5 5M16 7l5 5-5 5M14 5l-4 14\" />");

    // A marker's tip over the stroke it leaves.
    public static Icon Highlight { get; } =
        new($"<path {Lines} stroke-width=\"1.8\" d=\"M15 3.5l4.5 4.5-8 8H7V11.5z\" />"
            + $"<path {Lines} stroke-width=\"3\" d=\"M5 20.5h14\" />");

    // A capital A over a bar, as word processors draw the text color.
    public static Icon TextColor { get; } =
        new($"<path {Lines} stroke-width=\"2\" d=\"M6.5 16 12 3.5 17.5 16M8.5 11.5h7\" />"
            + $"<path {Lines} stroke-width=\"3\" d=\"M5 20.5h14\" />");

    // Angle brackets in a page, set apart from Code's bare brackets.
    public static Icon Source { get; } = new($"<path {Lines} stroke-width=\"1.8\" d=\"M5 3.5h14v17H5z\" />"
        + $"<path {Lines} stroke-width=\"2\" d=\"M10 9l-3 3 3 3M14 9l3 3-3 3\" />");

    // Two rounded links of a chain, joined on the diagonal.
    public static Icon Link { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0"
        + "-5.7-5.7l-1 1M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1\" />");

    // A line between two short lines of text.
    public static Icon HorizontalRule { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M4 12h16\" />"
        + $"<path {Lines} stroke-width=\"1.5\" d=\"M7 6.5h10M7 17.5h10\" />");

    // A "T" struck through on the diagonal.
    public static Icon ClearFormatting { get; } =
        new($"<path {Lines} stroke-width=\"2\" d=\"M5 6V4.5h11V6M10.5 4.5l-2.5 15M4 4l16 16\" />");

    public static Icon Heading1 { get; } = Heading("M16.5 13.5l2-1.5v7");

    public static Icon Heading2 { get; } = Heading("M16 13.6a1.8 1.8 0 1 1 3.3 1.1c-.7.9-3.3 3.3-3.3 3.3h3.6");

    public static Icon Heading3 { get; } = Heading("M16.2 12.5h3.3l-2 2.4a2 2 0 1 1-1.6 3.4");

    public static Icon Quote { get; } = new($"<path {Lines} stroke-width=\"1.8\" "
        + "d=\"M7 17c1.7-1.2 2.5-3 2.5-5.5V7H5v4.5h4.5M16.5 17c1.7-1.2 2.5-3 2.5-5.5V7h-4.5v4.5H19\" />");

    public static Icon BulletedList { get; } =
        new($"<path {Lines} stroke-width=\"3\" d=\"M5 7h.01M5 12h.01M5 17h.01\" />"
        + $"<path {Lines} stroke-width=\"2\" d=\"M9 7h11M9 12h11M9 17h11\" />");

    public static Icon NumberedList { get; } = new($"<path {Lines} stroke-width=\"1.3\" d=\"M4 5.5l1-.6v3.6M3.6 10.6"
        + "a.9.9 0 1 1 1.7.6l-1.7 1.8h1.9M3.7 15.4h1.6l-.9 1.1a1 1 0 1 1-.8 1.6\" />"
        + $"<path {Lines} stroke-width=\"2\" d=\"M9 7h11M9 12h11M9 17h11\" />");

    public static Icon Outdent { get; } = Indenting("M7 9.5 4.5 12 7 14.5");

    public static Icon Indent { get; } = Indenting("M4.5 9.5 7 12l-2.5 2.5");

    // A frame with a sun and a hill in it.
    public static Icon Image { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M5 5h14v14H5z\" />"
        + $"<path {Lines} stroke-width=\"2\" d=\"m5 17 4.5-4.5 3 3 2-2L19 18\" />"
        + $"<circle {Lines} stroke-width=\"2\" cx=\"15\" cy=\"9\" r=\"1.5\" />");

    // A grid of three columns, its top row drawn as a header.
    public static Icon Table { get; } =
        new($"<path {Lines} stroke-width=\"2\" d=\"M4 5h16v14H4zM4 9.5h16M9.3 5v14M14.7 5v14\" />");

    // A magnifying glass.
    public static Icon Find { get; } = new($"<circle {Lines} stroke-width=\"2\" cx=\"10.5\" cy=\"10.5\" r=\"5.5\" />"
        + $"<path {Lines} stroke-width=\"2\" d=\"m15 15 5 5\" />");

    // A capital A and a small a, drawn as lines.
    public static Icon MatchCase { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M3 18 7.5 6 12 18M4.9 14h5.2"
        + "M20 11v7M20 14.5a3 3 0 1 1-6 0 3 3 0 0 1 6 0\" />");

    // A small a and b over a line with turned-up ends, which marks off one word.
    public static Icon WholeWord { get; } = new($"<path {Lines} stroke-width=\"2\" d=\"M10 8v6M10 11a2.5 2.5 0 1 1-5 0 "
        + "2.5 2.5 0 0 1 5 0M14 5v9M14 11.5a2.5 2.5 0 1 0 5 0 2.5 2.5 0 0 0-5 0M3 17v2.5h18V17\" />");

    // A small table with three dots beside it, for its options.
    public static Icon TableMenu { get; } =
        new($"<path {Lines} stroke-width=\"2\" d=\"M4 5h11v11H4zM4 9.5h11M9.5 5v11\" />"
            + $"<path {Lines} stroke-width=\"2.4\" d=\"M19 9v.01M19 13v.01M19 17v.01\" />");

    // An "H" with a small level number beside it.
    private static Icon Heading(string number) =>
        new($"<path {Lines} stroke-width=\"2\" d=\"M4 5v14M13 5v14M4 12h9\" />"
        + $"<path {Lines} stroke-width=\"1.5\" d=\"{number}\" />");

    // Lines of text, the middle ones indented, with an arrow saying which way they move.
    private static Icon Indenting(string arrow) =>
        new($"<path {Lines} stroke-width=\"2\" d=\"M4 6h16M11 10h9M11 14h9M4 18h16{arrow}\" />");
}
