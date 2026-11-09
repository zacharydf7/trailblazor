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

    /// <summary>Names the button that inserts a horizontal line. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorHorizontalRule"/>.</summary>
    public string? HorizontalRule { get; init; }

    /// <summary>Names the clear formatting button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorClearFormatting"/>.</summary>
    public string? ClearFormatting { get; init; }

    /// <summary>Names the link button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorLink"/>.</summary>
    public string? Link { get; init; }

    /// <summary>Titles the link dialog for text without a link. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorAddLink"/>.</summary>
    public string? AddLink { get; init; }

    /// <summary>Titles the link dialog for a link. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorEditLink"/>.</summary>
    public string? EditLink { get; init; }

    /// <summary>Labels the link dialog's field for the text to insert. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorLinkText"/>.</summary>
    public string? LinkText { get; init; }

    /// <summary>Labels the link dialog's field for the URL. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorLinkAddress"/>.</summary>
    public string? LinkAddress { get; init; }

    /// <summary>The message for a URL whose scheme isn't allowed, with <c>{0}</c> for the allowed schemes. Leave it
    /// null for the app's text for <see cref="TbStrings.RichTextEditorLinkNotAllowed"/>.</summary>
    public string? LinkNotAllowed { get; init; }

    /// <summary>The message for a URL with a scheme where <see cref="TbRichTextEditor.LinkSchemes"/> allows only
    /// relative links. Leave it null for the app's text for <see cref="TbStrings.RichTextEditorRelativeLinkOnly"/>.
    /// </summary>
    public string? RelativeLinkOnly { get; init; }

    /// <summary>The link dialog's button that sets the link. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorApplyLink"/>.</summary>
    public string? ApplyLink { get; init; }

    /// <summary>The link dialog's button that takes the link off. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorRemoveLink"/>.</summary>
    public string? RemoveLink { get; init; }

    /// <summary>The link dialog's link to the URL. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorOpenLink"/>.</summary>
    public string? OpenLink { get; init; }
}
