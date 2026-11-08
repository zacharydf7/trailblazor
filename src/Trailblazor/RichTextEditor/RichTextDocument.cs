using System.Text;

namespace Trailblazor;

/// <summary>Rich text as paragraphs of runs, each run with one set of <see cref="RichTextFormats"/>. It is the content
/// of a rich text editor as .NET sees it: <see cref="Parse(string?)"/> reads any HTML into it, keeping only text,
/// paragraphs, line breaks and formats, and <see cref="ToHtml"/> writes it as one canonical HTML form.</summary>
public sealed class RichTextDocument
{
    private static readonly (RichTextFormats Format, string Tag)[] Tags =
    [
        (RichTextFormats.Bold, "strong"), (RichTextFormats.Italic, "em"), (RichTextFormats.Underline, "u"),
        (RichTextFormats.Strikethrough, "s"), (RichTextFormats.Subscript, "sub"), (RichTextFormats.Superscript, "sup"),
    ];

    /// <summary>Creates a document from its paragraphs. With no paragraphs, it has one empty paragraph.</summary>
    /// <param name="paragraphs">The paragraphs, in order.</param>
    public RichTextDocument(IEnumerable<RichTextParagraph> paragraphs)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        var list = paragraphs.ToList();
        Paragraphs = list.Count > 0 ? list : [new RichTextParagraph([])];
    }

    /// <summary>A document with one empty paragraph.</summary>
    public static RichTextDocument Empty { get; } = new([]);

    /// <summary>The paragraphs, in order. There is always at least one.</summary>
    public IReadOnlyList<RichTextParagraph> Paragraphs { get; }

    /// <summary>Whether the document has no text: one empty paragraph.</summary>
    public bool IsEmpty => Paragraphs is [{ Runs.Count: 0 }];

    /// <summary>Reads HTML into a document, keeping every format.</summary>
    /// <inheritdoc cref="Parse(string?, RichTextFormats)" path="/remarks"/>
    /// <param name="html">The HTML. Null or empty gives an empty document.</param>
    public static RichTextDocument Parse(string? html) => Parse(html, RichTextHtmlReader.AllFormats);

    /// <summary>Reads HTML into a document, keeping only the formats given.</summary>
    /// <remarks>Any HTML is accepted, and only text, paragraphs, line breaks and formats survive. Blocks such as
    /// paragraphs, headings, list items and table cells become paragraphs. Formats come from tags (<c>b</c>,
    /// <c>strong</c>, <c>i</c>, <c>em</c>, <c>u</c>, <c>s</c>, <c>sub</c>, <c>sup</c> and similar) and from inline
    /// styles (font weight, font style, text decoration and vertical alignment), as Word, Google Docs and browsers
    /// write them. Everything else, including scripts, attributes, links and images, becomes plain text or is dropped,
    /// so the result is safe to render. Tabs and no-break spaces read as plain spaces. A few rare cases can still read
    /// differently from a browser, such as tags misnested beyond a simple reorder and character references whose
    /// names HTML 4 lacks, which stay as written.</remarks>
    /// <param name="html">The HTML. Null or empty gives an empty document.</param>
    /// <param name="formats">The formats to keep. Others are dropped from the text that has them.</param>
    public static RichTextDocument Parse(string? html, RichTextFormats formats) =>
        string.IsNullOrEmpty(html) ? Empty : new(RichTextHtmlReader.Read(html, formats));

    /// <summary>Writes the document as canonical HTML: a <c>&lt;p&gt;</c> for each paragraph
    /// (<c>&lt;p&gt;&lt;br&gt;&lt;/p&gt;</c> when it's empty), <c>&lt;br&gt;</c> for line breaks, a tag for each format,
    /// outermost first in the order of <see cref="RichTextFormats"/>, and <c>&amp;nbsp;</c> for each space HTML would
    /// otherwise collapse. An empty document is <c>""</c>. Parsing the result gives back the same document.</summary>
    public string ToHtml()
    {
        if (IsEmpty)
        {
            return "";
        }

        var html = new StringBuilder();
        foreach (var paragraph in Paragraphs)
        {
            html.Append("<p>");
            WriteParagraph(html, paragraph);
            html.Append("</p>");
        }

        return html.ToString();
    }

    /// <summary>The document's text without formats, with a <c>'\n'</c> between paragraphs and for each line
    /// break.</summary>
    public string ToPlainText() => string.Join('\n', Paragraphs.Select(paragraph => paragraph.Text));

    private static void WriteParagraph(StringBuilder html, RichTextParagraph paragraph)
    {
        var text = paragraph.Text;
        var offset = 0;
        foreach (var run in paragraph.Runs)
        {
            var tags = Tags.Where(tag => run.Formats.HasFlag(tag.Format)).Select(tag => tag.Tag).ToList();
            foreach (var tag in tags)
            {
                html.Append('<').Append(tag).Append('>');
            }

            for (var i = 0; i < run.Text.Length; i++)
            {
                WriteCharacter(html, text, offset + i);
            }

            for (var i = tags.Count - 1; i >= 0; i--)
            {
                html.Append("</").Append(tags[i]).Append('>');
            }

            offset += run.Text.Length;
        }

        // A paragraph's last line break shows nothing, so an empty paragraph or a trailing break needs one more.
        if (text.Length == 0 || text[^1] == '\n')
        {
            html.Append("<br>");
        }
    }

    private static void WriteCharacter(StringBuilder html, string text, int i)
    {
        switch (text[i])
        {
            case '\n':
                html.Append("<br>");
                break;
            case '&':
                html.Append("&amp;");
                break;
            case '<':
                html.Append("&lt;");
                break;
            case '>':
                html.Append("&gt;");
                break;
            case ' ' when Collapses(text, i):
                html.Append("&nbsp;");
                break;
            default:
                html.Append(text[i]);
                break;
        }
    }

    // HTML drops a space at a line's start or end, or after another space.
    private static bool Collapses(string text, int i) =>
        i == 0 || i == text.Length - 1 || text[i - 1] is ' ' or '\n' || text[i + 1] == '\n';
}
