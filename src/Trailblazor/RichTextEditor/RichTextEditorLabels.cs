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

    /// <summary>Names the code button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorCode"/>.</summary>
    public string? Code { get; init; }

    /// <summary>Names the heading 1 button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorHeading1"/>.</summary>
    public string? Heading1 { get; init; }

    /// <summary>Names the heading 2 button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorHeading2"/>.</summary>
    public string? Heading2 { get; init; }

    /// <summary>Names the heading 3 button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorHeading3"/>.</summary>
    public string? Heading3 { get; init; }

    /// <summary>Names the quote button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorQuote"/>.</summary>
    public string? Quote { get; init; }

    /// <summary>Names the bulleted list button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorBulletedList"/>.</summary>
    public string? BulletedList { get; init; }

    /// <summary>Names the numbered list button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorNumberedList"/>.</summary>
    public string? NumberedList { get; init; }

    /// <summary>Names the outdent button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorOutdent"/>.</summary>
    public string? Outdent { get; init; }

    /// <summary>Names the indent button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorIndent"/>.</summary>
    public string? Indent { get; init; }
}
