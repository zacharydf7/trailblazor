using System.Text;
using System.Text.RegularExpressions;

namespace Trailblazor;

// Reads and writes the markdown a RichTextDocument can hold: headings, paragraphs, quotes, bulleted and numbered lists
// nested by indentation, rules, images on a line of their own, fenced code as code text, pipe tables, and bold, italic,
// strikethrough, code and links inside a paragraph. Anything else, such as raw HTML, stays as text.
internal static partial class RichTextMarkdown
{
    private const int ListIndent = 4;

    // The formats markdown has no marks for, whose text is written plain.
    private const RichTextFormats Unwritten = RichTextFormats.Underline | RichTextFormats.Subscript
        | RichTextFormats.Superscript | RichTextFormats.Highlight | RichTextFormats.Color;

    public static RichTextDocument Read(string? markdown)
    {
        var paragraphs = new List<RichTextParagraph>();
        var lines = new List<string>();
        var kind = RichTextParagraphKind.Paragraph;
        var level = 0;
        // The indentation of each open list's markers, outermost first.
        var lists = new List<int>();
        string? fence = null;
        var code = new List<string>();
        // The rows of the table being read, the first being its header row, and its count of columns.
        List<List<string>>? table = null;
        var columns = 0;

        foreach (var raw in (markdown ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = raw.Replace("\t", "    ");
            var content = line.TrimStart(' ');
            var indent = line.Length - content.Length;
            if (fence is not null)
            {
                if (content.StartsWith(fence, StringComparison.Ordinal) && content.Trim(fence[0]).Trim().Length == 0)
                {
                    AddCode();
                }
                else
                {
                    code.Add(raw);
                }

                continue;
            }

            // A table's rows run to a blank line or another block.
            if (table is not null)
            {
                if (content.Length > 0 && !StartsBlock(content))
                {
                    table.Add(Cells(content));
                    continue;
                }

                AddTable();
            }

            if (content.Length == 0)
            {
                Flush();
                continue;
            }

            if (Fence().Match(content) is { Success: true } opening)
            {
                Flush();
                fence = opening.Value;
                continue;
            }

            if (content[0] == '>')
            {
                var quoted = content[1..].StartsWith(' ') ? content[2..] : content[1..];
                if (kind != RichTextParagraphKind.Quote || quoted.Trim().Length == 0)
                {
                    Flush();
                }

                lists.Clear();
                if (quoted.Trim().Length > 0)
                {
                    kind = RichTextParagraphKind.Quote;
                    lines.Add(quoted);
                }

                continue;
            }

            // A row of dashes between pipes, under a line with as many cells, makes that line a table's header row.
            if (kind == RichTextParagraphKind.Paragraph && lines.Count > 0 && Delimiter().IsMatch(content)
                && Cells(lines[^1]).Count == Cells(content).Count)
            {
                var header = Cells(lines[^1]);
                lines.RemoveAt(lines.Count - 1);
                Flush();
                lists.Clear();
                table = [header];
                columns = header.Count;
                continue;
            }

            // A line of = or - under a paragraph's text makes it a heading.
            if (kind == RichTextParagraphKind.Paragraph && lines.Count > 0 && Underline().IsMatch(content))
            {
                kind = content[0] == '=' ? RichTextParagraphKind.Heading1 : RichTextParagraphKind.Heading2;
                Flush();
                continue;
            }

            if (Rule().IsMatch(content))
            {
                Flush();
                lists.Clear();
                paragraphs.Add(new RichTextParagraph([], RichTextParagraphKind.Rule));
                continue;
            }

            if (Heading().Match(content) is { Success: true } heading)
            {
                Flush();
                lists.Clear();
                kind = heading.Groups[1].Length switch
                {
                    1 => RichTextParagraphKind.Heading1,
                    2 => RichTextParagraphKind.Heading2,
                    _ => RichTextParagraphKind.Heading3,
                };
                lines.Add(ClosingHashes().Replace(heading.Groups[2].Value, ""));
                Flush();
                continue;
            }

            if (Item().Match(content) is { Success: true } item)
            {
                Flush();
                while (lists.Count > 0 && lists[^1] > indent)
                {
                    lists.RemoveAt(lists.Count - 1);
                }

                if (lists.Count == 0 || lists[^1] < indent)
                {
                    lists.Add(indent);
                }

                kind = char.IsAsciiDigit(item.Groups[1].Value[0]) ? RichTextParagraphKind.NumberedItem
                    : RichTextParagraphKind.BulletedItem;
                level = lists.Count - 1;
                lines.Add(item.Groups[2].Value);
                continue;
            }

            if (lines.Count == 0 && Image().Match(content) is { Success: true } image
                && ImageOf(Unescape(image.Groups[1].Value), image.Groups[2].Value.Trim('<', '>')) is { } picture)
            {
                lists.Clear();
                paragraphs.Add(new RichTextParagraph(picture));
                continue;
            }

            // A line under an open paragraph, quote or list item continues it, as markdown's lazy lines do.
            if (lines.Count == 0)
            {
                kind = RichTextParagraphKind.Paragraph;
                lists.Clear();
            }

            lines.Add(content);
        }

        if (fence is not null)
        {
            AddCode();
        }

        if (table is not null)
        {
            AddTable();
        }

        Flush();
        return new RichTextDocument(paragraphs);

        void Flush()
        {
            if (lines.Count > 0)
            {
                paragraphs.Add(new RichTextParagraph(Inline(JoinLines(lines)), kind,
                    RichTextParagraph.IsListItem(kind) ? level : 0));
            }

            lines.Clear();
            kind = RichTextParagraphKind.Paragraph;
            level = 0;
        }

        // A header row of empty cells stands for none, since a markdown table must have one; extra cells are dropped.
        void AddTable()
        {
            var header = table![0].Exists(cell => cell.Length > 0);
            var row = 0;
            foreach (var cells in header ? table : table.Skip(1))
            {
                for (var column = 0; column < columns; column++)
                {
                    paragraphs.Add(new RichTextParagraph(column < cells.Count ? Inline(cells[column]) : [],
                        new RichTextTableCell(row, column, header && row == 0)));
                }

                row++;
            }

            table = null;
        }

        void AddCode()
        {
            if (code.Count > 0)
            {
                paragraphs.Add(new RichTextParagraph([new RichTextRun(string.Join('\n', code), RichTextFormats.Code)]));
            }

            code.Clear();
            fence = null;
            lists.Clear();
        }
    }

    public static string Write(RichTextDocument document)
    {
        var markdown = new StringBuilder();
        RichTextParagraph? previous = null;
        // Each open list's kind and the number its next item takes, outermost first.
        var counts = new List<(RichTextParagraphKind Kind, int Next)>();
        var all = document.Paragraphs;
        for (var index = 0; index < all.Count; index++)
        {
            var paragraph = all[index];
            // Markdown has no empty paragraph: blank lines only separate blocks. A table is written at its first cell.
            if ((paragraph.Kind is RichTextParagraphKind.Paragraph or RichTextParagraphKind.Quote
                && paragraph.Text.Trim(' ', '\n').Length == 0) || paragraph.Cell is { Row: > 0 } or { Column: > 0 })
            {
                continue;
            }

            var listItem = RichTextParagraph.IsListItem(paragraph.Kind);
            if (previous is not null)
            {
                markdown.Append(listItem && RichTextParagraph.IsListItem(previous.Kind) ? "\n"
                    : paragraph.Kind == RichTextParagraphKind.Quote && previous.Kind == RichTextParagraphKind.Quote
                        ? "\n>\n" : "\n\n");
            }

            if (!listItem)
            {
                counts.Clear();
            }

            previous = paragraph;
            switch (paragraph.Kind)
            {
                case RichTextParagraphKind.Rule:
                    markdown.Append("---");
                    break;
                case RichTextParagraphKind.TableCell:
                    WriteTable(markdown, RichTextDocument.TableRows(all, index));
                    break;
                case RichTextParagraphKind.Image:
                    markdown.Append("![").Append(Escape(paragraph.Image!.Alt ?? "", lineStart: false)).Append("](")
                        .Append(Destination(paragraph.Image.Source)).Append(')');
                    break;
                case RichTextParagraphKind.Heading1 or RichTextParagraphKind.Heading2 or RichTextParagraphKind.Heading3:
                    var hashes = paragraph.Kind - RichTextParagraphKind.Heading1 + 1;
                    markdown.Append('#', hashes).Append(' ');
                    WriteRuns(markdown, paragraph.Runs, lineBreak: null);
                    break;
                case RichTextParagraphKind.Quote:
                    markdown.Append("> ");
                    WriteRuns(markdown, paragraph.Runs, lineBreak: "> ");
                    break;
                case RichTextParagraphKind.BulletedItem or RichTextParagraphKind.NumberedItem:
                    var marker = ListMarker(counts, paragraph);
                    var indent = new string(' ', ListIndent * paragraph.Level);
                    markdown.Append(indent).Append(marker);
                    WriteRuns(markdown, paragraph.Runs, lineBreak: indent + new string(' ', marker.Length));
                    break;
                default:
                    if (paragraph.Runs is [{ Text: var text } code] && text.Contains('\n')
                        && (code.Formats & ~Unwritten) == RichTextFormats.Code)
                    {
                        var ticks = new string('`', Math.Max(3, LongestRun(text, '`') + 1));
                        markdown.Append(ticks).Append('\n').Append(text).Append('\n').Append(ticks);
                    }
                    else
                    {
                        WriteRuns(markdown, paragraph.Runs, lineBreak: "");
                    }

                    break;
            }
        }

        return markdown.ToString();
    }

    // A markdown table needs a header row, so a table without one gets a row of empty cells, which reads back as none.
    // A cell's line breaks become spaces, and its pipes are escaped, even in code, since a row is split at its pipes
    // before code spans are read. A pipe after an odd count of backslashes gets one more, or it would end the cell.
    private static void WriteTable(StringBuilder markdown, List<List<RichTextParagraph>> rows)
    {
        var head = rows[0].TrueForAll(cell => cell.Cell!.Header);
        var lines = rows.Select(row => row.Select(cell =>
        {
            var text = new StringBuilder();
            WriteRuns(text, cell.Runs, lineBreak: null);
            return PipeAfterBackslashes().Replace(text.ToString(),
                pipe => pipe.Groups[1].Value + (pipe.Groups[1].Length % 2 == 0 ? "\\|" : "\\\\|"));
        })).ToList();
        if (!head)
        {
            lines.Insert(0, rows[0].Select(_ => ""));
        }

        lines.Insert(1, rows[0].Select(_ => "---"));
        markdown.AppendJoin('\n', lines.Select(line => "| " + string.Join(" | ", line) + " |"));
    }

    // A list's items count up from 1, and a list of another kind at the same level starts again.
    private static string ListMarker(List<(RichTextParagraphKind Kind, int Next)> counts, RichTextParagraph item)
    {
        if (counts.Count > item.Level + 1)
        {
            counts.RemoveRange(item.Level + 1, counts.Count - item.Level - 1);
        }

        while (counts.Count < item.Level)
        {
            counts.Add((RichTextParagraphKind.BulletedItem, 1));
        }

        if (counts.Count == item.Level || counts[item.Level].Kind != item.Kind)
        {
            counts.RemoveRange(item.Level, counts.Count - item.Level);
            counts.Add((item.Kind, 1));
        }

        var number = counts[item.Level].Next;
        counts[item.Level] = (item.Kind, number + 1);
        return item.Kind == RichTextParagraphKind.NumberedItem ? $"{number}. " : "- ";
    }

    // Writes the runs with the fewest marks: a format stays open across runs that share it, a mark the next run keeps
    // opens outside the others, and the spaces at a run's edges go outside its marks, since markdown doesn't read a
    // mark with a space on its inner side as a mark. A null lineBreak writes breaks as spaces.
    private static void WriteRuns(StringBuilder markdown, IReadOnlyList<RichTextRun> runs, string? lineBreak)
    {
        // Markdown can't end a paragraph with a line break, so breaks at its end are left out.
        while (runs.Count > 0 && runs[^1].Text.TrimEnd(' ').EndsWith('\n'))
        {
            var last = runs[^1].Text.TrimEnd(' ').TrimEnd('\n');
            runs = last.Length == 0 ? runs.Take(runs.Count - 1).ToList()
                : [.. runs.Take(runs.Count - 1),
                    new RichTextRun(last, runs[^1].Formats, runs[^1].Link, runs[^1].Color)];
        }

        var open = new List<string>();
        string? openLink = null;
        var pending = "";
        var lineStart = true;
        for (var index = 0; index < runs.Count; index++)
        {
            var run = runs[index];
            var text = lineBreak is null ? run.Text.Replace('\n', ' ') : run.Text;
            var core = text.Trim(' ', '\n');
            var wanted = Marks(run.Formats);
            var following = index + 1 < runs.Count && runs[index + 1].Link == run.Link
                ? Marks(runs[index + 1].Formats) : [];
            wanted = [.. open.Where(wanted.Contains), .. wanted.Where(mark => !open.Contains(mark))
                .OrderBy(mark => following.Contains(mark) ? 0 : 1)];
            var kept = run.Link == openLink ? open.Zip(wanted).TakeWhile(pair => pair.First == pair.Second).Count() : 0;
            Close(kept);
            if (run.Link != openLink)
            {
                CloseLink();
            }

            // Spaces alone close what they don't share, and open nothing until the next text.
            if (core.Length == 0)
            {
                pending += text;
                continue;
            }

            var lead = text[..text.IndexOf(core, StringComparison.Ordinal)];
            var trail = text[(lead.Length + core.Length)..];

            Gap(pending + lead);

            if (run.Link is not null && openLink is null)
            {
                markdown.Append('[');
                openLink = run.Link;
            }

            foreach (var mark in wanted.Skip(kept))
            {
                markdown.Append(mark);
                open.Add(mark);
            }

            if (run.Formats.HasFlag(RichTextFormats.Code))
            {
                var code = core.Replace('\n', ' ');
                var ticks = new string('`', LongestRun(code, '`') + 1);
                var pad = code.StartsWith('`') || code.EndsWith('`') ? " " : "";
                markdown.Append(ticks).Append(pad).Append(code).Append(pad).Append(ticks);
            }
            else
            {
                var lines = core.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    if (i > 0)
                    {
                        markdown.Append("\\\n").Append(lineBreak);
                    }

                    markdown.Append(Escape(i > 0 ? lines[i].TrimStart(' ') : lines[i], lineStart || i > 0));
                }
            }

            lineStart = false;

            pending = trail;
        }

        Close(0);
        CloseLink();

        // Spaces and breaks between runs, outside any marks. Markdown drops spaces at a line's start, so they aren't
        // written there.
        void Gap(string gap)
        {
            foreach (var character in gap)
            {
                if (character == '\n' && lineBreak is not null)
                {
                    markdown.Append("\\\n").Append(lineBreak);
                    lineStart = true;
                }
                else if (!lineStart)
                {
                    markdown.Append(character);
                }
            }
        }

        void Close(int keep)
        {
            for (var i = open.Count - 1; i >= keep; i--)
            {
                markdown.Append(open[i]);
            }

            open.RemoveRange(keep, open.Count - keep);
        }

        void CloseLink()
        {
            if (openLink is not null)
            {
                Close(0);
                markdown.Append("](").Append(Destination(openLink)).Append(')');
                openLink = null;
            }
        }
    }

    private static List<string> Marks(RichTextFormats formats)
    {
        var marks = new List<string>();
        if (formats.HasFlag(RichTextFormats.Strikethrough))
        {
            marks.Add("~~");
        }

        if (formats.HasFlag(RichTextFormats.Bold))
        {
            marks.Add("**");
        }

        if (formats.HasFlag(RichTextFormats.Italic))
        {
            marks.Add("*");
        }

        return marks;
    }

    // Escapes what markdown would read as marks, and at a line's start what it would read as a block.
    private static string Escape(string text, bool lineStart)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (character is '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '~' or '!')
            {
                escaped.Append('\\');
            }

            escaped.Append(character);
        }

        var result = escaped.ToString();
        if (!lineStart)
        {
            return result;
        }

        // A backslash only escapes punctuation, so a number that would start a list escapes the mark after it.
        return OrderedStart().Match(result) is { Success: true } number ? result.Insert(number.Length - 1, "\\")
            : BlockStart().IsMatch(result) || Delimiter().IsMatch(result) ? "\\" + result : result;
    }

    // A quote, heading, fence, rule or list item starts a block of its own.
    private static bool StartsBlock(string content) => content[0] == '>' || Fence().IsMatch(content)
        || Heading().IsMatch(content) || Rule().IsMatch(content) || Item().IsMatch(content);

    // A table row's cells: the text between its pipes, without a pipe at either end, and with "\|" read as "|".
    private static List<string> Cells(string line)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        var text = line.Trim(' ');
        var ended = false;
        for (var i = text.StartsWith('|') ? 1 : 0; i < text.Length; i++)
        {
            ended = text[i] == '|';
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                cell.Append(text[i + 1] == '|' ? "" : "\\").Append(text[++i]);
            }
            else if (ended)
            {
                cells.Add(cell.ToString().Trim(' '));
                cell.Clear();
            }
            else
            {
                cell.Append(text[i]);
            }
        }

        if (!ended)
        {
            cells.Add(cell.ToString().Trim(' '));
        }

        return cells;
    }

    private static string Destination(string url) => url.Any(character => character is ' ' or '(' or ')' or '<' or '>')
        ? "<" + url.Replace("<", "%3C").Replace(">", "%3E") + ">"
        : url;

    private static int LongestRun(string text, char character)
    {
        int longest = 0, current = 0;
        foreach (var next in text)
        {
            current = next == character ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }

        return longest;
    }

    // A line ending in a backslash or two spaces breaks there; any other line ending is a space.
    private static string JoinLines(List<string> lines)
    {
        var text = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].TrimStart(' ');
            if (i == lines.Count - 1)
            {
                text.Append(line.TrimEnd(' '));
            }
            else if ((line.Length - line.TrimEnd('\\').Length) % 2 == 1)
            {
                text.Append(line[..^1]).Append('\n');
            }
            else
            {
                text.Append(line.TrimEnd(' ')).Append(line.EndsWith("  ", StringComparison.Ordinal) ? '\n' : ' ');
            }
        }

        return text.ToString();
    }

    private static List<RichTextRun> Inline(string text)
    {
        var runs = new List<RichTextRun>();
        ReadInline(text, RichTextFormats.None, null, runs);
        return runs;
    }

    private static void ReadInline(string text, RichTextFormats formats, string? link, List<RichTextRun> runs)
    {
        var plain = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            var character = text[i];
            if (character == '\\' && i + 1 < text.Length && IsPunctuation(text[i + 1]))
            {
                plain.Append(text[i + 1]);
                i += 2;
            }
            else if (character == '`' && CodeSpan(text, i) is var (code, codeEnd))
            {
                Emit();
                runs.Add(new RichTextRun(code, formats | RichTextFormats.Code, link));
                i = codeEnd;
            }
            else if (character == '!' && i + 1 < text.Length && text[i + 1] == '['
                && LinkAt(text, i + 1) is var (alt, _, imageEnd))
            {
                // An image inside a paragraph has no place in the document, so its alt text stands in for it.
                plain.Append(Unescape(alt));
                i = imageEnd;
            }
            else if (character == '[' && link is null && LinkAt(text, i) is var (label, url, linkEnd))
            {
                Emit();
                ReadInline(label, formats, Safe(url), runs);
                i = linkEnd;
            }
            else if (character == '<' && link is null && Autolink().Match(text, i) is { Success: true } auto
                && auto.Index == i)
            {
                Emit();
                runs.Add(new RichTextRun(auto.Groups[1].Value, formats, Safe(auto.Groups[1].Value)));
                i += auto.Length;
            }
            else if (character is '*' or '_' or '~' && Emphasis(text, i) is var (width, close))
            {
                Emit();
                var format = character == '~' ? RichTextFormats.Strikethrough
                    : width == 2 ? RichTextFormats.Bold : RichTextFormats.Italic;
                ReadInline(text[(i + width)..close], formats | format, link, runs);
                i = close + width;
            }
            else
            {
                plain.Append(character);
                i++;
            }
        }

        Emit();

        void Emit()
        {
            if (plain.Length > 0)
            {
                runs.Add(new RichTextRun(plain.ToString(), formats, link));
                plain.Clear();
            }
        }
    }

    // A run of backticks opens a code span that the next run of the same length closes.
    private static (string Code, int End)? CodeSpan(string text, int start)
    {
        var width = RunAt(text, start, '`');
        for (var i = start + width; i < text.Length;)
        {
            var run = RunAt(text, i, '`');
            if (run == width)
            {
                var code = text[(start + width)..i].Replace('\n', ' ');
                return (code.Length > 2 && code[0] == ' ' && code[^1] == ' ' ? code[1..^1] : code, i + width);
            }

            i += Math.Max(run, 1);
        }

        return null;
    }

    // Emphasis opens where its marks come before text, and closes at the next marks of the same width that come after
    // text. Three marks open bold and italic, the outer one being whichever closes last. An underscore only marks a
    // whole word, and a tilde needs two.
    private static (int Width, int Close)? Emphasis(string text, int start)
    {
        var mark = text[start];
        var run = RunAt(text, start, mark);
        var after = start + run < text.Length ? text[start + run] : ' ';
        var before = start > 0 ? text[start - 1] : ' ';
        if (char.IsWhiteSpace(after) || (mark == '_' && char.IsLetterOrDigit(before)))
        {
            return null;
        }

        var width = mark == '~' ? (run == 2 ? 2 : 0)
            : run < 3 ? run : CloserAt(text, start + run, mark, 0) is { Length: 2 } ? 1 : 2;
        return width == 0 ? null : CloserAt(text, start + width, mark, width) is { } closer
            ? (width, closer.Index + closer.Length - width) : null;
    }

    // The next run of marks that can close emphasis of this width: one of the same width, or of three or more. Width 0
    // takes any run.
    private static (int Index, int Length)? CloserAt(string text, int from, char mark, int width)
    {
        for (var i = from; i < text.Length;)
        {
            if (text[i] == '\\')
            {
                i += 2;
                continue;
            }

            if (text[i] == '`' && CodeSpan(text, i) is var (_, end))
            {
                i = end;
                continue;
            }

            var closing = RunAt(text, i, mark);
            if (closing == 0)
            {
                i++;
                continue;
            }

            var next = i + closing < text.Length ? text[i + closing] : ' ';
            if ((width == 0 || closing == width || (closing >= 3 && mark != '~')) && i > from
                && !char.IsWhiteSpace(text[i - 1])
                && (mark != '_' || !char.IsLetterOrDigit(next)))
            {
                return (i, closing);
            }

            i += closing;
        }

        return null;
    }

    // [label](destination "title"), with brackets inside the label balanced.
    private static (string Label, string Url, int End)? LinkAt(string text, int start)
    {
        var depth = 0;
        var i = start;
        for (; i < text.Length; i++)
        {
            if (text[i] == '\\')
            {
                i++;
            }
            else if (text[i] == '[')
            {
                depth++;
            }
            else if (text[i] == ']' && --depth == 0)
            {
                break;
            }
        }

        if (i + 1 >= text.Length || text[i + 1] != '(' || Target().Match(text, i + 1) is not { Success: true } target
            || target.Index != i + 1)
        {
            return null;
        }

        var url = target.Groups[1].Success ? target.Groups[1].Value : target.Groups[2].Value;
        return (text[(start + 1)..i], Unescape(url), i + 1 + target.Length);
    }

    private static RichTextImage? ImageOf(string alt, string source) =>
        Safe(source) is { } url ? new RichTextImage(url, alt) : null;

    // A link that would run script, or is empty, becomes plain text.
    private static string? Safe(string url)
    {
        var cleaned = RichTextSchema.CleanLink(url);
        return cleaned is null || RichTextSchema.RunsScript(RichTextSchema.SchemeOf(cleaned)) ? null : cleaned;
    }

    private static string Unescape(string text) => Escaped().Replace(text, "$1");

    // The ASCII punctuation a backslash escapes.
    private static bool IsPunctuation(char character) =>
        character is (>= '!' and <= '/') or (>= ':' and <= '@') or (>= '[' and <= '`') or (>= '{' and <= '~');

    private static int RunAt(string text, int start, char character)
    {
        var end = start;
        while (end < text.Length && text[end] == character)
        {
            end++;
        }

        return end - start;
    }

    [GeneratedRegex(@"^(`{3,}(?![^`]*`)|~{3,})")]
    private static partial Regex Fence();

    [GeneratedRegex(@"^(?:=+|-+) *$")]
    private static partial Regex Underline();

    [GeneratedRegex(@"^([-*_])(?: *\1){2,} *$")]
    private static partial Regex Rule();

    [GeneratedRegex(@"^(#{1,6})(?: +(.*?))? *$")]
    private static partial Regex Heading();

    [GeneratedRegex(@"(?:^| +)#+ *$")]
    private static partial Regex ClosingHashes();

    [GeneratedRegex(@"^([-*+]|\d{1,9}[.)])(?: +(.*)|$)")]
    private static partial Regex Item();

    [GeneratedRegex(@"^!\[((?:\\.|[^\]\\])*)\]\(\s*(<[^>]*>|[^\s)]+)(?:\s+""[^""]*"")?\s*\)\s*$")]
    private static partial Regex Image();

    [GeneratedRegex(@"\(\s*(?:<([^>\n]*)>|((?:\\.|[^\s()\\]|\((?:\\.|[^\s()\\])*\))+))"
        + @"(?:\s+(?:""[^""]*""|'[^']*'))?\s*\)")]
    private static partial Regex Target();

    [GeneratedRegex(@"<((?:https?|mailto|ftp):[^\s<>]+)>")]
    private static partial Regex Autolink();

    [GeneratedRegex(@"\\([!-/:-@\[-`{-~])")]
    private static partial Regex Escaped();

    [GeneratedRegex(@"^(?:#|>|[-+*] |[-+*]$|=+ *$|-+ *$|`{3}|~{3})")]
    private static partial Regex BlockStart();

    [GeneratedRegex(@"^\d{1,9}[.)](?= |$)")]
    private static partial Regex OrderedStart();

    [GeneratedRegex(@"(\\*)\|")]
    private static partial Regex PipeAfterBackslashes();

    // A table's delimiter row: cells of dashes, each with an optional colon at either end, with at least one pipe.
    [GeneratedRegex(@"^(?=[^|]*\|)\|? *:?-+:? *(?:\| *:?-+:? *)*\|? *$")]
    private static partial Regex Delimiter();
}
