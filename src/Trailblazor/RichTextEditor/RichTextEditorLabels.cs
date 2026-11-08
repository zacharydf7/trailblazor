namespace Trailblazor;

/// <summary>The words a <see cref="TbRichTextEditor"/> shows and says to screen readers.</summary>
public sealed record RichTextEditorLabels
{
    /// <summary>Names the toolbar. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorToolbar"/>.</summary>
    public string? Toolbar { get; init; }

    /// <summary>Names the bold button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorBold"/>.</summary>
    public string? Bold { get; init; }

    /// <summary>Names the italic button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorItalic"/>.</summary>
    public string? Italic { get; init; }

    /// <summary>Names the underline button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorUnderline"/>.</summary>
    public string? Underline { get; init; }

    /// <summary>Names the strikethrough button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorStrikethrough"/>.</summary>
    public string? Strikethrough { get; init; }

    /// <summary>Names the subscript button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorSubscript"/>.</summary>
    public string? Subscript { get; init; }

    /// <summary>Names the superscript button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorSuperscript"/>.</summary>
    public string? Superscript { get; init; }
}
