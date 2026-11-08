using System.Text;

namespace Trailblazor;

/// <summary>Rich text as paragraphs of runs, each run with one set of <see cref="RichTextFormats"/>. It is the content
/// of a rich text editor as .NET sees it: <see cref="Parse(string?)"/> reads any HTML into it, keeping only text,
/// blocks, line breaks, formats and safe links, and <see cref="ToHtml"/> writes it as one canonical HTML form.
/// </summary>
public sealed class RichTextDocument
{
    private static readonly (RichTextFormats Format, string Tag)[] Tags =
    [
        (RichTextFormats.Bold, "strong"), (RichTextFormats.Italic, "em"), (RichTextFormats.Underline, "u"),
        (RichTextFormats.Strikethrough, "s"), (RichTextFormats.Subscript, "sub"), (RichTextFormats.Superscript, "sup"),
        (RichTextFormats.Code, "code"),
    ];

    /// <summary>Creates a document from its paragraphs. With no paragraphs, it has one empty paragraph. A list item
    /// nested more than one level deeper than the list item before it moves up to one level deeper, or to level 0 when
    /// no list item comes right before it, as a list can only nest one level at a time.</summary>
    /// <param name="paragraphs">The paragraphs, in order.</param>
    public RichTextDocument(IEnumerable<RichTextParagraph> paragraphs)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        var list = new List<RichTextParagraph>();
        var previousLevel = -1;
        foreach (var paragraph in paragraphs)
        {
            var listItem = RichTextParagraph.IsListItem(paragraph.Kind);
            list.Add(listItem && paragraph.Level > previousLevel + 1
                ? new RichTextParagraph(paragraph.Runs, paragraph.Kind, previousLevel + 1)
                : paragraph);
            previousLevel = listItem ? list[^1].Level : -1;
        }

        Paragraphs = list.Count > 0 ? list : [new RichTextParagraph([])];
    }

    /// <summary>A document with one empty paragraph.</summary>
    public static RichTextDocument Empty { get; } = new([]);

    /// <summary>The paragraphs, in order. There is always at least one.</summary>
    public IReadOnlyList<RichTextParagraph> Paragraphs { get; }

    /// <summary>Whether the document has no text: one empty paragraph.</summary>
    public bool IsEmpty => Paragraphs is [{ Runs.Count: 0, Kind: RichTextParagraphKind.Paragraph }];

    /// <summary>Reads HTML into a document, keeping what <see cref="RichTextSchema.Default"/> allows.</summary>
    /// <inheritdoc cref="Parse(string?, RichTextSchema)" path="/remarks"/>
    /// <param name="html">The HTML. Null or empty gives an empty document.</param>
    public static RichTextDocument Parse(string? html) => Parse(html, RichTextSchema.Default);

    /// <summary>Reads HTML into a document, keeping only what the schema allows.</summary>
    /// <remarks>Any HTML is accepted, and only text, blocks, line breaks, formats and links survive. Headings, block
    /// quotes, list items and rules keep their kind; other blocks, such as table cells, become paragraphs. Formats
    /// come from tags (<c>b</c>, <c>strong</c>, <c>i</c>, <c>em</c>, <c>u</c>, <c>s</c>, <c>sub</c>, <c>sup</c>,
    /// <c>code</c> and similar) and from inline styles (font weight, font style, text decoration, vertical alignment
    /// and monospace fonts), as Word, Google Docs and browsers write them. Word's list paragraphs read as lists.
    /// Everything else, including scripts, attributes, images and links with a scheme the schema doesn't allow,
    /// becomes plain text or is dropped, so the result is safe to render. Tabs and no-break spaces read as plain
    /// spaces. A few rare cases can still read differently from a browser, such as tags misnested beyond a simple
    /// reorder and character references whose names HTML 4 lacks, which stay as written.</remarks>
    /// <param name="html">The HTML. Null or empty gives an empty document.</param>
    /// <param name="schema">What to keep. Formats it doesn't allow are dropped from the text that has them, and blocks
    /// it doesn't allow give way to an allowed block around them or a plain paragraph.</param>
    public static RichTextDocument Parse(string? html, RichTextSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return string.IsNullOrEmpty(html) ? Empty : new(RichTextHtmlReader.Read(html, schema));
    }

    /// <summary>Writes the document as canonical HTML: a <c>&lt;p&gt;</c> for each paragraph
    /// (<c>&lt;p&gt;&lt;br&gt;&lt;/p&gt;</c> when it's empty), <c>&lt;h1&gt;</c> to <c>&lt;h3&gt;</c> for headings,
    /// neighboring quote paragraphs in one <c>&lt;blockquote&gt;</c>, list items in <c>&lt;ul&gt;</c> and
    /// <c>&lt;ol&gt;</c> with each nested list inside the item before it, and <c>&lt;hr&gt;</c> for a rule. Inside a
    /// paragraph, <c>&lt;br&gt;</c> is a line break, a link is the outermost tag, the other formats follow in the order
    /// of <see cref="RichTextFormats"/>, and <c>&amp;nbsp;</c> stands for each space HTML would otherwise collapse. An
    /// empty document is <c>""</c>. Parsing the result with a schema that allows its formats, blocks and link schemes
    /// gives back the same document.</summary>
    public string ToHtml()
    {
        if (IsEmpty)
        {
            return "";
        }

        var html = new StringBuilder();
        // The lists open around the current item, outermost first; each item's <li> stays open for a nested list.
        var lists = new List<string>();
        var inQuote = false;
        foreach (var paragraph in Paragraphs)
        {
            var listItem = RichTextParagraph.IsListItem(paragraph.Kind);
            CloseLists(html, lists, listItem ? paragraph.Level + 1 : 0);
            if (inQuote && paragraph.Kind != RichTextParagraphKind.Quote)
            {
                html.Append("</blockquote>");
                inQuote = false;
            }

            if (listItem)
            {
                var tag = paragraph.Kind == RichTextParagraphKind.NumberedItem ? "ol" : "ul";
                if (lists.Count == paragraph.Level + 1 && lists[^1] != tag)
                {
                    CloseLists(html, lists, paragraph.Level);
                }

                if (lists.Count == paragraph.Level + 1)
                {
                    html.Append("</li>");
                }
                else
                {
                    html.Append('<').Append(tag).Append('>');
                    lists.Add(tag);
                }

                html.Append("<li>");
                WriteParagraph(html, paragraph);
                continue;
            }

            if (paragraph.Kind == RichTextParagraphKind.Quote && !inQuote)
            {
                html.Append("<blockquote>");
                inQuote = true;
            }

            var block = paragraph.Kind switch
            {
                RichTextParagraphKind.Heading1 => "h1",
                RichTextParagraphKind.Heading2 => "h2",
                RichTextParagraphKind.Heading3 => "h3",
                RichTextParagraphKind.Rule => null,
                _ => "p",
            };
            if (block is null)
            {
                html.Append("<hr>");
                continue;
            }

            html.Append('<').Append(block).Append('>');
            WriteParagraph(html, paragraph);
            html.Append("</").Append(block).Append('>');
        }

        CloseLists(html, lists, 0);
        if (inQuote)
        {
            html.Append("</blockquote>");
        }

        return html.ToString();
    }

    // Closes the lists deeper than the depth given, each with its last item.
    private static void CloseLists(StringBuilder html, List<string> lists, int depth)
    {
        while (lists.Count > depth)
        {
            html.Append("</li></").Append(lists[^1]).Append('>');
            lists.RemoveAt(lists.Count - 1);
        }
    }

    /// <summary>The document's text without formats, with a <c>'\n'</c> between paragraphs and for each line
    /// break.</summary>
    public string ToPlainText() => string.Join('\n', Paragraphs.Select(paragraph => paragraph.Text));

    private static void WriteParagraph(StringBuilder html, RichTextParagraph paragraph)
    {
        var text = paragraph.Text;
        var offset = 0;
        string? openLink = null;
        foreach (var run in paragraph.Runs)
        {
            var tags = Tags.Where(tag => run.Formats.HasFlag(tag.Format)).Select(tag => tag.Tag).ToList();
            // One link stays one <a> across its differently formatted runs, so it's one stop for the keyboard.
            if (run.Link != openLink)
            {
                CloseLink();
                if (run.Link is not null)
                {
                    html.Append("<a href=\"").Append(run.Link.Replace("&", "&amp;").Replace("\"", "&quot;"))
                        .Append("\">");
                }

                openLink = run.Link;
            }

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

        CloseLink();
        // A paragraph's last line break shows nothing, so an empty paragraph or a trailing break needs one more.
        if (text.Length == 0 || text[^1] == '\n')
        {
            html.Append("<br>");
        }

        void CloseLink()
        {
            if (openLink is not null)
            {
                html.Append("</a>");
                openLink = null;
            }
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
