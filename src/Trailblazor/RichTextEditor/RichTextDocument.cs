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
        (RichTextFormats.Code, "code"), (RichTextFormats.Highlight, "mark"),
    ];

    /// <summary>Creates a document from its paragraphs. With no paragraphs, it has one empty paragraph. A list item
    /// nested more than one level deeper than the list item before it moves up to one level deeper, or to level 0 when
    /// no list item comes right before it, as a list can only nest one level at a time. Neighboring table cells make
    /// one table, and a cell at row 0, column 0 starts a new table; a cell whose row differs from the cell before it
    /// starts a new row. Each table's rows and columns are renumbered from 0 in order, and a short row is padded at its
    /// end with empty cells (header cells when the rest of the row is), so every table is a full grid.</summary>
    /// <param name="paragraphs">The paragraphs, in order.</param>
    public RichTextDocument(IEnumerable<RichTextParagraph> paragraphs)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        var list = new List<RichTextParagraph>();
        var rows = new List<List<RichTextParagraph>>();
        var previousLevel = -1;
        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Cell is { } cell)
            {
                var previous = rows.Count > 0 ? rows[^1][^1].Cell! : null;
                if (previous is not null && cell is { Row: 0, Column: 0 })
                {
                    AddTable(list, rows);
                    previous = null;
                }

                if (cell.Row != previous?.Row)
                {
                    rows.Add([]);
                }

                rows[^1].Add(paragraph);
                previousLevel = -1;
                continue;
            }

            AddTable(list, rows);
            var listItem = RichTextParagraph.IsListItem(paragraph.Kind);
            list.Add(listItem && paragraph.Level > previousLevel + 1
                ? new RichTextParagraph(paragraph.Runs, paragraph.Kind, previousLevel + 1)
                : paragraph);
            previousLevel = listItem ? list[^1].Level : -1;
        }

        AddTable(list, rows);
        Paragraphs = list.Count > 0 ? list : [new RichTextParagraph([])];
    }

    // Adds a table's cells, numbered in order and padded to a full grid, then empties the rows for the next table.
    private static void AddTable(List<RichTextParagraph> paragraphs, List<List<RichTextParagraph>> rows)
    {
        var width = rows.Count > 0 ? rows.Max(row => row.Count) : 0;
        for (var row = 0; row < rows.Count; row++)
        {
            var header = rows[row].TrueForAll(cell => cell.Cell!.Header);
            for (var column = 0; column < width; column++)
            {
                var cell = column < rows[row].Count ? rows[row][column] : null;
                var place = new RichTextTableCell(row, column, cell?.Cell!.Header ?? header);
                paragraphs.Add(cell is not null && cell.Cell == place ? cell
                    : new RichTextParagraph(cell?.Runs ?? [], place));
            }
        }

        rows.Clear();
    }

    /// <summary>A document with one empty paragraph.</summary>
    public static RichTextDocument Empty { get; } = new([]);

    /// <summary>The paragraphs, in order. There is always at least one.</summary>
    public IReadOnlyList<RichTextParagraph> Paragraphs { get; }

    /// <summary>The mentions in the document, in order, each with the trigger it was typed with and the app's id for
    /// what it mentions. The ids come from the HTML, so check them as you would any other input.</summary>
    public IReadOnlyList<Mention> Mentions =>
        [.. Paragraphs.SelectMany(paragraph => paragraph.Runs).Select(run => run.Mention).OfType<Mention>()];

    /// <summary>Whether the document has no text: one empty paragraph.</summary>
    public bool IsEmpty => Paragraphs is [{ Runs.Count: 0, Kind: RichTextParagraphKind.Paragraph }];

    /// <summary>Reads HTML into a document, keeping what <see cref="RichTextSchema.Default"/> allows.</summary>
    /// <inheritdoc cref="Parse(string?, RichTextSchema)" path="/remarks"/>
    /// <param name="html">The HTML. Null or empty gives an empty document.</param>
    public static RichTextDocument Parse(string? html) => Parse(html, RichTextSchema.Default);

    /// <summary>Reads HTML into a document, keeping only what the schema allows.</summary>
    /// <remarks>Any HTML is accepted, and only text, blocks, line breaks, formats and links survive. Headings, block
    /// quotes, list items, rules, images and table cells keep their kind; other blocks become paragraphs. An image
    /// inside a paragraph ends it, and the text after the image starts another of the same kind. A table cell keeps
    /// only text: blocks inside it become line breaks, a table inside it adds only its text, and an image inside it
    /// is dropped. A cell spanning several rows or columns takes up a single cell, and text inside a table but outside
    /// its cells goes before the table, as a browser shows it. Formats come from tags (<c>b</c>, <c>strong</c>,
    /// <c>i</c>, <c>em</c>, <c>u</c>, <c>s</c>, <c>sub</c>, <c>sup</c>, <c>code</c> and similar) and from inline
    /// styles (font weight, font style, text decoration, vertical alignment and monospace fonts), as Word, Google Docs
    /// and browsers write them. Word's list paragraphs read as lists. Where the schema allows mentions, a
    /// <c>span</c> with a <c>data-mention</c> attribute reads as one: its text is one run, the attribute's value is
    /// the item's id, and <c>data-trigger</c> is its trigger when its text starts with it; otherwise the whole text
    /// is the item's. Everything else, including scripts, attributes, images the schema doesn't allow and links
    /// with a scheme it doesn't allow, becomes plain text or is dropped, so the result is safe to render. Tabs and
    /// no-break spaces read as plain spaces. A few rare cases can still read
    /// differently from a browser, such as tags misnested beyond a simple reorder, character references whose names
    /// HTML 4 lacks (which stay as written), and stray text in a table that a browser joins to the line just before the
    /// table.</remarks>
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
    /// <c>&lt;ol&gt;</c> with each nested list inside the item before it, <c>&lt;hr&gt;</c> for a rule,
    /// <c>&lt;img&gt;</c> with its <c>src</c> and <c>alt</c> for an image, and a <c>&lt;table&gt;</c> for each table,
    /// with its first row in a <c>&lt;thead&gt;</c> of <c>&lt;th scope="col"&gt;</c> cells when they're all header
    /// cells and the other rows in a <c>&lt;tbody&gt;</c>. Inside a paragraph or cell, <c>&lt;br&gt;</c> is a
    /// line break, a link is the outermost tag, the other formats follow in the order of
    /// <see cref="RichTextFormats"/>, a mention is a <c>&lt;span&gt;</c> inside its formats with its id in
    /// <c>data-mention</c> and its trigger in <c>data-trigger</c>, and <c>&amp;nbsp;</c> stands for each space HTML
    /// would otherwise collapse. An empty document is <c>""</c>. Parsing the result with a schema that allows its
    /// formats, blocks, link schemes, image schemes and mentions gives back the same document.</summary>
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
        for (var i = 0; i < Paragraphs.Count; i++)
        {
            var paragraph = Paragraphs[i];
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

            if (paragraph.Image is { } image)
            {
                html.Append("<img src=\"").Append(Attribute(image.Source)).Append('"');
                if (image.Alt is not null)
                {
                    html.Append(" alt=\"").Append(Attribute(image.Alt)).Append('"');
                }

                html.Append('>');
                continue;
            }

            if (paragraph.Cell is not null)
            {
                var rows = TableRows(Paragraphs, i);
                WriteTable(html, rows);
                i += rows.Sum(row => row.Count) - 1;
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

    // The rows of the table whose first cell is at the index given, which run to the next table or another block.
    internal static List<List<RichTextParagraph>> TableRows(IReadOnlyList<RichTextParagraph> paragraphs, int start)
    {
        var end = start + 1;
        while (end < paragraphs.Count && paragraphs[end].Cell is { } next && next is not { Row: 0, Column: 0 })
        {
            end++;
        }

        return [.. paragraphs.Skip(start).Take(end - start).GroupBy(cell => cell.Cell!.Row)
            .Select(row => row.ToList())];
    }

    // A first row of header cells is the table's head, so its cells head the columns.
    private static void WriteTable(StringBuilder html, List<List<RichTextParagraph>> rows)
    {
        var head = rows[0].TrueForAll(cell => cell.Cell!.Header);
        html.Append("<table>");
        if (head)
        {
            html.Append("<thead>");
            WriteRow(html, rows[0], head: true);
            html.Append("</thead>");
        }

        if (rows.Count > (head ? 1 : 0))
        {
            html.Append("<tbody>");
            foreach (var row in rows.Skip(head ? 1 : 0))
            {
                WriteRow(html, row, head: false);
            }

            html.Append("</tbody>");
        }

        html.Append("</table>");
    }

    private static void WriteRow(StringBuilder html, IEnumerable<RichTextParagraph> cells, bool head)
    {
        html.Append("<tr>");
        foreach (var cell in cells)
        {
            var tag = cell.Cell!.Header ? "th" : "td";
            html.Append('<').Append(tag).Append(head ? " scope=\"col\"" : "").Append('>');
            WriteParagraph(html, cell);
            html.Append("</").Append(tag).Append('>');
        }

        html.Append("</tr>");
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

    /// <summary>Reads markdown into a document, keeping what <see cref="RichTextSchema.Default"/> allows.</summary>
    /// <inheritdoc cref="FromMarkdown(string?, RichTextSchema)" path="/remarks"/>
    /// <param name="markdown">The markdown. Null or empty gives an empty document.</param>
    public static RichTextDocument FromMarkdown(string? markdown) => FromMarkdown(markdown, RichTextSchema.Default);

    /// <summary>Reads markdown into a document, keeping what the schema allows, as <see cref="Parse(string?,
    /// RichTextSchema)"/> does for HTML.</summary>
    /// <remarks>It reads headings (<c>#</c> to <c>###</c>, deeper ones as <c>###</c>, or text underlined with
    /// <c>=</c> or <c>-</c>), paragraphs, <c>&gt;</c> quotes, <c>-</c>, <c>*</c>, <c>+</c> and <c>1.</c> list items
    /// nested by indentation, rules, an image alone on its line, fenced code as code text, pipe tables (whose first row
    /// is the header row unless its cells are all empty), and <c>**bold**</c>,
    /// <c>*italic*</c>, <c>~~strikethrough~~</c>, <c>`code`</c>, <c>[links](url)</c> and <c>&lt;url&gt;</c> inside a
    /// paragraph. Anything else, such as HTML, stays as text. A link or image the schema doesn't allow becomes text or
    /// is dropped, as in HTML.</remarks>
    /// <param name="markdown">The markdown. Null or empty gives an empty document.</param>
    /// <param name="schema">What to keep: the formats, blocks, and link and image schemes.</param>
    public static RichTextDocument FromMarkdown(string? markdown, RichTextSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return Parse(RichTextMarkdown.Read(markdown).ToHtml(), schema);
    }

    /// <summary>Writes the document as markdown that <see cref="FromMarkdown(string?)"/> reads back the same, apart
    /// from what markdown can't hold: underline, subscript, superscript, highlighted and colored text is written
    /// plain, and so is text with a mark of the app's own; a mention is written as its text; empty paragraphs, line
    /// breaks at a paragraph's end and spaces at a line's start or end are left out; line breaks in a heading or in
    /// code that shares its paragraph become spaces; a
    /// space or line break at a format's edge moves outside it; and
    /// in a table, line breaks in a cell become spaces, header cells outside a first row of header cells become plain
    /// cells, a first row of empty header cells is left out, and code with an odd number of backslashes right before
    /// a pipe reads back with one more.
    /// </summary>
    public string ToMarkdown() => RichTextMarkdown.Write(this);

    /// <summary>The document's text without formats, with a <c>'\n'</c> between paragraphs and for each line
    /// break.</summary>
    public string ToPlainText() => string.Join('\n', Paragraphs.Select(paragraph => paragraph.Text));

    private static void WriteParagraph(StringBuilder html, RichTextParagraph paragraph)
    {
        var text = paragraph.Text;
        var offset = 0;
        string? openLink = null;
        Tone? openColor = null;
        foreach (var run in paragraph.Runs)
        {
            // The app's own marks go inside the formats' tags, sorted by name.
            var tags = Tags.Where(tag => run.Formats.HasFlag(tag.Format)).Select(tag => (Open: tag.Tag, Close: tag.Tag))
                .Concat(run.Marks.Select(RichTextMark.TagOf)).ToList();
            // One link stays one <a> across its differently formatted runs, so it's one stop for the keyboard.
            if (run.Link != openLink)
            {
                CloseColor();
                CloseLink();
                if (run.Link is not null)
                {
                    html.Append("<a href=\"").Append(Attribute(run.Link)).Append("\">");
                }

                openLink = run.Link;
            }

            // The color goes around the format tags, so an underline or a strikethrough takes it too, and one color
            // stays one <span> across its differently formatted runs, inside the link.
            if (run.Color != openColor)
            {
                CloseColor();
                if (run.Color is { } color)
                {
                    html.Append("<span data-tb-color=\"").Append(RichTextRun.ColorName(color)).Append("\">");
                }

                openColor = run.Color;
            }

            foreach (var tag in tags)
            {
                html.Append('<').Append(tag.Open).Append('>');
            }

            if (run.Mention is { } mention)
            {
                html.Append("<span data-mention=\"").Append(Attribute(mention.Item.Id ?? ""))
                    .Append("\" data-trigger=\"").Append(Attribute(mention.Trigger)).Append("\">");
            }

            for (var i = 0; i < run.Text.Length; i++)
            {
                WriteCharacter(html, text, offset + i);
            }

            if (run.Mention is not null)
            {
                html.Append("</span>");
            }

            for (var i = tags.Count - 1; i >= 0; i--)
            {
                html.Append("</").Append(tags[i].Close).Append('>');
            }

            offset += run.Text.Length;
        }

        CloseColor();
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

        void CloseColor()
        {
            if (openColor is not null)
            {
                html.Append("</span>");
                openColor = null;
            }
        }
    }

    private static string Attribute(string value) => value.Replace("&", "&amp;").Replace("\"", "&quot;");

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
