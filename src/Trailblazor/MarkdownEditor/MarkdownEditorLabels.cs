namespace Trailblazor;

/// <summary>The words a markdown editor's toolbar and preview show and say to screen readers. Each one left null uses
/// the app's text, the same as the rich text editor's for its buttons.</summary>
public sealed record MarkdownEditorLabels
{
    /// <summary>Names the toolbar, for the app's text for <see cref="TbStrings.RichTextEditorToolbar"/>.</summary>
    public string? Toolbar { get; init; }

    /// <summary>Names the bold button, for the app's text for <see cref="TbStrings.RichTextEditorBold"/>.</summary>
    public string? Bold { get; init; }

    /// <summary>Names the italic button, for the app's text for <see cref="TbStrings.RichTextEditorItalic"/>.</summary>
    public string? Italic { get; init; }

    /// <summary>Names the strikethrough button, for the app's text for
    /// <see cref="TbStrings.RichTextEditorStrikethrough"/>.</summary>
    public string? Strikethrough { get; init; }

    /// <summary>Names the code button, for the app's text for <see cref="TbStrings.RichTextEditorCode"/>.</summary>
    public string? Code { get; init; }

    /// <summary>Names the link button, for the app's text for <see cref="TbStrings.RichTextEditorLink"/>.</summary>
    public string? Link { get; init; }

    /// <summary>Names the first heading button, for the app's text for
    /// <see cref="TbStrings.RichTextEditorHeading1"/>.</summary>
    public string? Heading1 { get; init; }

    /// <summary>Names the second heading button, for the app's text for
    /// <see cref="TbStrings.RichTextEditorHeading2"/>.</summary>
    public string? Heading2 { get; init; }

    /// <summary>Names the third heading button, for the app's text for
    /// <see cref="TbStrings.RichTextEditorHeading3"/>.</summary>
    public string? Heading3 { get; init; }

    /// <summary>Names the quote button, for the app's text for <see cref="TbStrings.RichTextEditorQuote"/>.</summary>
    public string? Quote { get; init; }

    /// <summary>Names the bulleted list button, for the app's text for
    /// <see cref="TbStrings.RichTextEditorBulletedList"/>.</summary>
    public string? BulletedList { get; init; }

    /// <summary>Names the numbered list button, for the app's text for
    /// <see cref="TbStrings.RichTextEditorNumberedList"/>.</summary>
    public string? NumberedList { get; init; }

    /// <summary>Names the horizontal line button, for the app's text for
    /// <see cref="TbStrings.RichTextEditorHorizontalRule"/>.</summary>
    public string? HorizontalRule { get; init; }

    /// <summary>Names the Preview button and the preview, for the app's text for
    /// <see cref="TbStrings.MarkdownEditorPreview"/>.</summary>
    public string? Preview { get; init; }
}
