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

    /// <summary>Names the text color menu's button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorTextColor"/>.</summary>
    public string? TextColor { get; init; }

    /// <summary>The text color menu's item that takes the color off. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorDefaultColor"/>.</summary>
    public string? DefaultColor { get; init; }

    /// <summary>Names the primary tone's color in the text color menu. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorColorPrimary"/>.</summary>
    public string? ColorPrimary { get; init; }

    /// <summary>Names the secondary tone's color in the text color menu. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorColorSecondary"/>.</summary>
    public string? ColorSecondary { get; init; }

    /// <summary>Names the neutral tone's color in the text color menu. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorColorNeutral"/>.</summary>
    public string? ColorNeutral { get; init; }

    /// <summary>Names the success tone's color in the text color menu. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorColorSuccess"/>.</summary>
    public string? ColorSuccess { get; init; }

    /// <summary>Names the warning tone's color in the text color menu. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorColorWarning"/>.</summary>
    public string? ColorWarning { get; init; }

    /// <summary>Names the danger tone's color in the text color menu. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorColorDanger"/>.</summary>
    public string? ColorDanger { get; init; }

    /// <summary>Names the info tone's color in the text color menu. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorColorInfo"/>.</summary>
    public string? ColorInfo { get; init; }

    /// <summary>Names the highlight button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorHighlight"/>.</summary>
    public string? Highlight { get; init; }

    /// <summary>Names the slash command that turns a heading, quote or list item back into a plain paragraph. Leave
    /// it null for the app's text for <see cref="TbStrings.RichTextEditorParagraph"/>.</summary>
    public string? Paragraph { get; init; }

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

    /// <summary>Read out as mention suggestions come in, with <c>{0}</c> for how many. Leave it null for the app's
    /// text for <see cref="TbStrings.MentionInputSuggestions"/>.</summary>
    public string? MentionSuggestions { get; init; }

    /// <summary>Read out when the search for mention suggestions fails. Leave it null for the app's text for
    /// <see cref="TbStrings.MentionInputSearchFailed"/>.</summary>
    public string? MentionSearchFailed { get; init; }

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

    /// <summary>Names the link dialog's cancel button. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogCancel"/>.</summary>
    public string? CancelLink { get; init; }

    /// <summary>The message for a value longer than <c>MaxLength</c>, with <c>{0}</c> for the editor's label and
    /// <c>{1}</c> for the limit. Leave it null for the app's text for <see cref="TbStrings.TextFieldTooLong"/>.
    /// </summary>
    public string? TooLong { get; init; }

    /// <summary>What screen readers hear for the counter without a limit, with <c>{0}</c> for the count. Leave it null
    /// for the app's text for <see cref="TbStrings.TextFieldCount"/>.</summary>
    public string? Count { get; init; }

    /// <summary>What screen readers hear for the counter with a limit, with <c>{0}</c> for the count and <c>{1}</c> for
    /// the limit. Leave it null for the app's text for <see cref="TbStrings.TextFieldCountOfLimit"/>.</summary>
    public string? CountOfLimit { get; init; }

    /// <summary>What's announced as the text nears the limit, with <c>{0}</c> for the characters left. Leave it null
    /// for the app's text for <see cref="TbStrings.TextFieldCharactersLeft"/>.</summary>
    public string? CharactersLeft { get; init; }

    /// <summary>Names the HTML source button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorSource"/>.</summary>
    public string? Source { get; init; }

    /// <summary>Names the find and replace button and its bar. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorFindAndReplace"/>.</summary>
    public string? FindAndReplace { get; init; }

    /// <summary>Names the field for the text to find, and is its placeholder. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorFind"/>.</summary>
    public string? Find { get; init; }

    /// <summary>Says which match is the current one, with <c>{0}</c> for its number and <c>{1}</c> for how many there
    /// are. Leave it null for the app's text for <see cref="TbStrings.RichTextEditorFindCount"/>.</summary>
    public string? FindCount { get; init; }

    /// <summary>Says that the text to find isn't in the editor. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorFindNoResults"/>.</summary>
    public string? FindNoResults { get; init; }

    /// <summary>Names the button that goes to the previous match. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorFindPrevious"/>.</summary>
    public string? FindPrevious { get; init; }

    /// <summary>Names the button that goes to the next match. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorFindNext"/>.</summary>
    public string? FindNext { get; init; }

    /// <summary>Names the button that makes the search tell capital letters from small ones. Leave it null for the
    /// app's text for <see cref="TbStrings.RichTextEditorMatchCase"/>.</summary>
    public string? MatchCase { get; init; }

    /// <summary>Names the button that makes the search find whole words only. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorWholeWord"/>.</summary>
    public string? WholeWord { get; init; }

    /// <summary>Names the button that closes the find bar. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorCloseFind"/>.</summary>
    public string? CloseFind { get; init; }

    /// <summary>Names the field for the text that replaces a match, and is its placeholder. Leave it null for the
    /// app's text for <see cref="TbStrings.RichTextEditorReplaceWith"/>.</summary>
    public string? ReplaceWith { get; init; }

    /// <summary>Names the button that replaces the current match. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorReplace"/>.</summary>
    public string? Replace { get; init; }

    /// <summary>Names the button that replaces every match. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorReplaceAll"/>.</summary>
    public string? ReplaceAll { get; init; }

    /// <summary>Says how many matches were replaced, with <c>{0}</c> for the number. Leave it null for the app's text
    /// for <see cref="TbStrings.RichTextEditorReplaced"/>.</summary>
    public string? Replaced { get; init; }

    /// <summary>Names the toolbar that shows over selected text. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorFloatingToolbar"/>.</summary>
    public string? FloatingToolbar { get; init; }

    /// <summary>Names the Image button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImage"/>.</summary>
    public string? Image { get; init; }

    /// <summary>Names the Table button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorTable"/>.</summary>
    public string? Table { get; init; }

    /// <summary>Names the table menu's button. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorTableMenu"/>.</summary>
    public string? TableMenu { get; init; }

    /// <summary>The table menu's Add row above command. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorAddRowAbove"/>.</summary>
    public string? AddRowAbove { get; init; }

    /// <summary>The table menu's Add row below command. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorAddRowBelow"/>.</summary>
    public string? AddRowBelow { get; init; }

    /// <summary>The table menu's Add column left command. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorAddColumnLeft"/>.</summary>
    public string? AddColumnLeft { get; init; }

    /// <summary>The table menu's Add column right command. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorAddColumnRight"/>.</summary>
    public string? AddColumnRight { get; init; }

    /// <summary>The table menu's Delete row command. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorDeleteRow"/>.</summary>
    public string? DeleteRow { get; init; }

    /// <summary>The table menu's Delete column command. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorDeleteColumn"/>.</summary>
    public string? DeleteColumn { get; init; }

    /// <summary>The table menu's Delete table command. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorDeleteTable"/>.</summary>
    public string? DeleteTable { get; init; }

    /// <summary>The table menu's Header row checkbox. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorHeaderRow"/>.</summary>
    public string? HeaderRow { get; init; }

    /// <summary>The image dialog's title. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorInsertImage"/>.</summary>
    public string? InsertImage { get; init; }

    /// <summary>Labels the image dialog's file picker. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImageFile"/>.</summary>
    public string? ImageFile { get; init; }

    /// <summary>Labels the image dialog's alt text field. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImageAlt"/>.</summary>
    public string? ImageAlt { get; init; }

    /// <summary>The help under the image dialog's alt text field. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImageAltHelp"/>.</summary>
    public string? ImageAltHelp { get; init; }

    /// <summary>Labels the image dialog's Decorative checkbox. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImageDecorative"/>.</summary>
    public string? Decorative { get; init; }

    /// <summary>The help under the image dialog's Decorative checkbox. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImageDecorativeHelp"/>.</summary>
    public string? DecorativeHelp { get; init; }

    /// <summary>The image dialog's button that stores the image and puts it in. Leave it null for the app's text for
    /// <see cref="TbStrings.RichTextEditorApplyImage"/>.</summary>
    public string? ApplyImage { get; init; }

    /// <summary>Names the image dialog's cancel button. Leave it null for the app's text for <see
    /// cref="TbStrings.DialogCancel"/>.</summary>
    public string? CancelImage { get; init; }

    /// <summary>The image dialog's message when the upload fails. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImageFailed"/>.</summary>
    public string? ImageFailed { get; init; }

    /// <summary>The image dialog's message when the upload returns an address <see
    /// cref="TbRichTextEditor.ImageSchemes"/> doesn't allow. Leave it null for the app's text for <see
    /// cref="TbStrings.RichTextEditorImageNotAllowed"/>.</summary>
    public string? ImageNotAllowed { get; init; }
}
