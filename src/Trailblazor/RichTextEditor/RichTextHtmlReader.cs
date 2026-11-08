using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Trailblazor;

// Follows a browser's parsing rules where they change the text or its formats; only text and formats ever come out.
internal sealed partial class RichTextHtmlReader
{
    public const RichTextFormats AllFormats = RichTextFormats.Bold | RichTextFormats.Italic | RichTextFormats.Underline
        | RichTextFormats.Strikethrough | RichTextFormats.Subscript | RichTextFormats.Superscript;

    // Chromium's parser stops nesting elements at this depth, and the limit keeps every scan of open elements short.
    private const int MaxDepth = 512;

    // More formatting elements than this left open at once is far from anything a person writes, so the oldest goes.
    private const int MaxFormatting = 16;

    private static readonly Dictionary<string, RichTextFormats> TagFormats = new()
    {
        ["b"] = RichTextFormats.Bold, ["strong"] = RichTextFormats.Bold, ["i"] = RichTextFormats.Italic,
        ["em"] = RichTextFormats.Italic, ["u"] = RichTextFormats.Underline, ["ins"] = RichTextFormats.Underline,
        ["s"] = RichTextFormats.Strikethrough, ["strike"] = RichTextFormats.Strikethrough,
        ["del"] = RichTextFormats.Strikethrough, ["sub"] = RichTextFormats.Subscript,
        ["sup"] = RichTextFormats.Superscript,
    };

    private static readonly HashSet<string> Blocks =
    [
        "address", "article", "aside", "blockquote", "caption", "center", "dd", "details", "dialog", "dir", "div", "dl",
        "dt", "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header",
        "hgroup", "legend", "li", "listing", "main", "menu", "nav", "ol", "p", "plaintext", "pre", "search", "section",
        "summary", "table", "td", "th", "tr", "ul", "xmp",
    ];

    // Elements whose content never shows as text.
    private static readonly HashSet<string> Skipped =
    [
        "head", "iframe", "math", "noembed", "noframes", "noscript", "object", "script", "select", "style", "svg",
        "template", "textarea", "title",
    ];

    // A "<" inside these starts no tag until their own end tag.
    private static readonly HashSet<string> RawText =
        ["iframe", "noembed", "noframes", "plaintext", "script", "style", "textarea", "title", "xmp"];

    private static readonly HashSet<string> Void =
        ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"];

    private static readonly HashSet<string> ClosesParagraph =
    [
        "address", "article", "aside", "blockquote", "center", "dd", "details", "dialog", "dir", "div", "dl", "dt",
        "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup",
        "hr", "li", "listing", "main", "menu", "nav", "ol", "p", "plaintext", "pre", "search", "section", "summary",
        "table", "ul", "xmp",
    ];

    // The elements the parser reopens when a paragraph or another element closed them early.
    private static readonly HashSet<string> Formatting =
        ["a", "b", "big", "code", "em", "font", "i", "nobr", "s", "small", "strike", "strong", "tt", "u"];

    // Formatting from outside one of these doesn't reopen inside it.
    private static readonly HashSet<string> FormattingBoundaries =
        ["applet", "caption", "marquee", "object", "td", "template", "th"];

    // An unclosed head ends at any element that doesn't belong in it.
    private static readonly HashSet<string> HeadContent =
    [
        "base", "basefont", "bgsound", "link", "meta", "noframes", "noscript", "script", "style", "template", "title",
    ];

    // SVG or MathML left open ends at any of these.
    private static readonly HashSet<string> EndsForeignContent =
    [
        "b", "big", "blockquote", "body", "br", "center", "code", "dd", "div", "dl", "dt", "em", "embed", "h1", "h2",
        "h3", "h4", "h5", "h6", "head", "hr", "i", "img", "li", "listing", "menu", "meta", "nobr", "ol", "p", "pre",
        "ruby", "s", "small", "span", "strike", "strong", "sub", "sup", "table", "tt", "u", "ul", "var",
    ];

    // Outside a table, the parser ignores these tags altogether.
    private static readonly HashSet<string> TableParts =
        ["caption", "col", "colgroup", "tbody", "td", "tfoot", "th", "thead", "tr"];

    private static readonly string[] ButtonScope =
        ["applet", "button", "caption", "html", "marquee", "object", "table", "td", "template", "th"];

    private static readonly string[] BlockScope =
        ["applet", "caption", "html", "marquee", "object", "table", "td", "template", "th"];

    private static readonly string[] TableScope = ["html", "table", "template"];

    private static readonly string[] ListItemScope = ["ol", "ul", "table", "td", "th"];

    private static readonly string[] DefinitionScope = ["dl", "table", "td", "th"];

    private readonly string _html;
    private readonly RichTextFormats _formats;
    private readonly List<Element> _open = [];
    // Formatting to reopen, in order; null marks a boundary.
    private readonly List<Element?> _formatting = [];
    private readonly List<RichTextParagraph> _paragraphs = [];
    private List<(string Text, RichTextFormats Formats, bool Pre)>? _runs;
    private int _position;
    private int _openParagraphs;
    private int _openForeign;
    private bool _dropNewline;
    private bool _started;
    private bool _inBody;
    // Without a standards doctype, a browser parses in quirks mode, where a table doesn't end a paragraph.
    private bool _quirks = true;

    private RichTextHtmlReader(string html, RichTextFormats formats)
    {
        _html = html.Replace("\r\n", "\n").Replace('\r', '\n');
        _formats = formats & AllFormats;
    }

    private RichTextFormats Formats => _open.Count > 0 ? _open[^1].Formats : RichTextFormats.None;

    private bool InPre => _open.Count > 0 && _open[^1].Pre;

    private bool InSkipped => _open.Count > 0 && _open[^1].Skipped;

    private bool InOpenHead => _open.Count > 0 && _open[^1].Name == "head";

    private bool InForeignContent => _openForeign > 0;

    public static List<RichTextParagraph> Read(string html, RichTextFormats formats)
    {
        var reader = new RichTextHtmlReader(html, formats);
        reader.ReadAll();
        return reader._paragraphs;
    }

    private void ReadAll()
    {
        while (_position < _html.Length)
        {
            var tag = _html.IndexOf('<', _position);
            var end = tag < 0 ? _html.Length : tag;
            if (end > _position)
            {
                AddText(HtmlCharacterReferences.Decode(_html[_position..end]));
            }

            _position = end;
            if (tag >= 0)
            {
                ReadMarkup();
            }
        }

        while (_open.Count > 0)
        {
            Pop();
        }

        FinishParagraph();
    }

    private void ReadMarkup()
    {
        var next = CharAt(_position + 1);
        _dropNewline = false;
        if (string.CompareOrdinal(_html, _position, "<!--", 0, 4) == 0)
        {
            // Searching from the second dash also ends the short comments "<!-->" and "<!--->".
            var end = _html.IndexOf("-->", _position + 2, StringComparison.Ordinal);
            _position = end < 0 ? _html.Length : end + 3;
        }
        else if (next is '!' or '?' || (next == '/' && !char.IsAsciiLetter(CharAt(_position + 2))))
        {
            // A doctype, Word's conditional markers and other bogus comments run to the next ">".
            var start = _position;
            SkipPast('>');
            if (!_started && IsDoctype(_html[start.._position]))
            {
                _quirks = IsQuirksDoctype(_html[start.._position]);
            }
        }
        else if (next == '/')
        {
            _position += 2;
            var name = ReadName();
            SkipPast('>');
            EndTag(name);
        }
        else if (char.IsAsciiLetter(next))
        {
            _position++;
            var name = ReadName();
            var (style, selfClosing) = ReadAttributes();
            StartTag(name, style, selfClosing);
            if (RawText.Contains(name))
            {
                ReadRawText(name);
            }
        }
        else
        {
            AddText("<");
            _position++;
        }
    }

    private void StartTag(string name, string style, bool selfClosing)
    {
        _started = true;
        if (InOpenHead && !HeadContent.Contains(name))
        {
            Close("head");
        }

        // A browser ignores a head that starts after the body has begun.
        if (name == "head" && _inBody)
        {
            return;
        }

        if (name is not ("html" or "head") && !HeadContent.Contains(name))
        {
            _inBody = true;
        }

        if (InForeignContent && EndsForeignContent.Contains(name))
        {
            CloseForeignContent();
        }

        if (name == "br")
        {
            AddBreak();
            return;
        }

        if (!InSkipped && TableParts.Contains(name) && !_open.Exists(element => element.Name == "table"))
        {
            return;
        }

        if (!InSkipped && _openParagraphs > 0 && ClosesParagraph.Contains(name) && !(name == "table" && _quirks))
        {
            Close("p");
        }

        if (Void.Contains(name))
        {
            // A rule between two lines leaves them separate paragraphs.
            if (name == "hr" && !InSkipped)
            {
                FinishParagraph();
            }

            return;
        }

        if (!InSkipped && name == "li")
        {
            Close("li");
        }
        else if (!InSkipped && name is "dd" or "dt")
        {
            Close("dd");
            Close("dt");
        }

        if (InSkipped || Skipped.Contains(name) || WordListMarker().IsMatch(style))
        {
            // A self-closing <svg/> or <math/> ends at once, as it does in a browser.
            if (!(selfClosing && name is "svg" or "math"))
            {
                Push(new Element(name, style, Formats, block: false, InPre, skipped: true, _paragraphs.Count));
            }

            return;
        }

        var block = Blocks.Contains(name);
        if (block)
        {
            FinishParagraph();
        }
        else
        {
            Reconstruct();
        }

        // A browser keeps the white space in these as written.
        var pre = name is "pre" or "listing" or "xmp" or "plaintext";
        var element = new Element(name, style, FormatsOf(name, style, Formats), block, InPre || pre, skipped: false,
            _paragraphs.Count);
        if (!Push(element))
        {
            return;
        }

        if (Formatting.Contains(name))
        {
            Remember(element);
        }
        else if (FormattingBoundaries.Contains(name))
        {
            _formatting.Add(null);
        }

        // The parser drops a line break right after <pre> or <listing>.
        _dropNewline = name is "pre" or "listing";
    }

    private void EndTag(string name)
    {
        _started = true;
        if (InForeignContent && name is "p" or "br")
        {
            CloseForeignContent();
        }

        if (name == "br")
        {
            AddBreak();
            return;
        }

        // An end tag for an element outside open SVG or MathML follows the HTML rules, as in a browser.
        var leavesForeign = InSkipped && LeavesForeignContent(name);
        if ((!InSkipped || leavesForeign) && Formatting.Contains(name) && EndFormatting(name))
        {
            return;
        }

        var index = InSkipped && !leavesForeign ? ClosedInSkipped(name) : ClosedBy(name);
        if (index >= 0)
        {
            PopTo(index);
        }
        else if (name == "p" && !InSkipped && _inBody)
        {
            // In the body, a </p> that closes no <p> makes an empty paragraph.
            FinishParagraph();
            _paragraphs.Add(new RichTextParagraph([]));
        }
    }

    // The open element an end tag closes, or -1: a block only within its scope, anything else only with no block inside.
    private int ClosedBy(string name)
    {
        string[]? scope = name switch
        {
            "p" => ButtonScope,
            "li" => ListItemScope,
            "dd" or "dt" => DefinitionScope,
            _ when TableParts.Contains(name) || name == "table" => TableScope,
            _ when Blocks.Contains(name) => BlockScope,
            _ => null,
        };
        for (var i = _open.Count - 1; i >= 0; i--)
        {
            var element = _open[i];
            if (element.Name == name || (IsHeading(name) && IsHeading(element.Name)))
            {
                return i;
            }

            if (scope?.Contains(element.Name) ?? (element.Block || TableParts.Contains(element.Name)))
            {
                return -1;
            }
        }

        return -1;
    }

    // Inside skipped content an end tag closes only what opened there.
    private int ClosedInSkipped(string name)
    {
        var index = _open.FindLastIndex(element => element.Name == name);
        return index >= _open.FindIndex(element => element.Skipped) ? index : -1;
    }

    // Whether an end tag inside open SVG or MathML names nothing opened there, so the HTML rules apply to it.
    private bool LeavesForeignContent(string name)
    {
        var root = _open.FindIndex(element => element.Skipped);
        return _open[root].Name is "svg" or "math" && _open.FindLastIndex(element => element.Name == name) < root;
    }

    // Any heading's end tag closes whichever heading is open.
    private static bool IsHeading(string name) => name is "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

    // Ends a formatting element, as far as a browser would; false when nothing remembers it.
    private bool EndFormatting(string name)
    {
        var boundary = _formatting.FindLastIndex(entry => entry is null);
        var entryIndex = _formatting.FindLastIndex(entry => entry?.Name == name);
        if (entryIndex <= boundary)
        {
            return false;
        }

        var element = _formatting[entryIndex]!;
        _formatting.RemoveAt(entryIndex);
        var index = _open.IndexOf(element);
        if (index < 0)
        {
            return true;
        }

        if (_open.Skip(index + 1).Any(open => open.Block))
        {
            // Blocks inside stay open without the format; elements above the last close, and their formatting reopens.
            _open.RemoveAt(index);
            element.IsOpen = false;
            PopTo(_open.FindLastIndex(open => open.Block) + 1);
            for (var i = index; i < _open.Count; i++)
            {
                var parent = i > 0 ? _open[i - 1].Formats : RichTextFormats.None;
                _open[i].Formats = _open[i].Skipped ? parent : FormatsOf(_open[i].Name, _open[i].Style, parent);
            }
        }
        else
        {
            PopTo(index);
        }

        return true;
    }

    // Reopens the formatting a paragraph or another element closed early, before more content goes in.
    private void Reconstruct()
    {
        if (InSkipped)
        {
            return;
        }

        var start = _formatting.Count;
        while (start > 0 && _formatting[start - 1] is { IsOpen: false })
        {
            start--;
        }

        for (var i = start; i < _formatting.Count; i++)
        {
            var entry = _formatting[i]!;
            var reopened = new Element(entry.Name, entry.Style, FormatsOf(entry.Name, entry.Style, Formats),
                block: false, InPre, skipped: false, _paragraphs.Count);
            if (!Push(reopened))
            {
                return;
            }

            _formatting[i] = reopened;
        }
    }

    private bool Push(Element element)
    {
        // A skipped element still opens at the limit, so its content never shows as text.
        if (_open.Count >= MaxDepth && !(element.Skipped && !InSkipped))
        {
            return false;
        }

        _open.Add(element);
        element.IsOpen = true;
        Count(element, 1);
        return true;
    }

    // Like the parser, remembers no more than three of the same formatting element since the last boundary.
    private void Remember(Element element)
    {
        var boundary = _formatting.FindLastIndex(entry => entry is null);
        var same = Enumerable.Range(boundary + 1, _formatting.Count - boundary - 1)
            .Where(i => _formatting[i]!.Name == element.Name && _formatting[i]!.Style == element.Style).ToList();
        if (same.Count >= 3)
        {
            _formatting.RemoveAt(same[0]);
        }

        _formatting.Add(element);
        if (_formatting.Count > MaxFormatting)
        {
            _formatting.RemoveAt(0);
        }
    }

    // Keeps count of the open elements that other rules ask about, so they need no search.
    private void Count(Element element, int change)
    {
        if (element.Name is "svg" or "math")
        {
            _openForeign += change;
        }
        else if (element.Name == "p")
        {
            _openParagraphs += change;
        }
    }

    private void Close(string name)
    {
        var index = ClosedBy(name);
        if (index >= 0)
        {
            PopTo(index);
        }
    }

    private void CloseForeignContent() => PopTo(_open.FindIndex(element => element.Name is "svg" or "math"));

    private void PopTo(int index)
    {
        while (_open.Count > index)
        {
            Pop();
        }
    }

    private void Pop()
    {
        var element = _open[^1];
        _open.RemoveAt(_open.Count - 1);
        element.IsOpen = false;
        Count(element, -1);
        if (!element.Skipped && FormattingBoundaries.Contains(element.Name))
        {
            var boundary = Math.Max(_formatting.FindLastIndex(entry => entry is null), 0);
            _formatting.RemoveRange(boundary, _formatting.Count - boundary);
        }

        if (element.Block)
        {
            FinishParagraph();
            // A block with no text is still a paragraph.
            if (_paragraphs.Count == element.FirstParagraph)
            {
                _paragraphs.Add(new RichTextParagraph([]));
            }
        }
    }

    private void AddText(string text)
    {
        if (_dropNewline && text.StartsWith('\n'))
        {
            text = text[1..];
        }

        _dropNewline = false;
        if (text.AsSpan().IndexOfAnyExcept(" \t\n\f\r") >= 0)
        {
            _started = true;
            // Visible text ends a head left open.
            if (InOpenHead)
            {
                Close("head");
            }

            _inBody |= !InSkipped;
        }

        if (InSkipped || text.Length == 0)
        {
            return;
        }

        Reconstruct();
        (_runs ??= []).Add((InPre ? text : Whitespace().Replace(text, " "), Formats & _formats, InPre));
    }

    private void AddBreak()
    {
        if (!InSkipped)
        {
            Reconstruct();
            (_runs ??= []).Add(("\n", Formats & _formats, false));
        }
    }

    private void FinishParagraph()
    {
        if (_runs is null)
        {
            return;
        }

        var hasBreak = _runs.Exists(run => run.Text.Contains('\n'));
        // As HTML renders it outside <pre>: no space after another or at a line's start, and none at a line's end.
        var runs = Keep(_runs, (text, i, pre) => pre || text[i] != ' ' || (i > 0 && text[i - 1] is not (' ' or '\n')));
        runs = Keep(runs, (text, i, pre) => pre || text[i] != ' ' || (i < text.Length - 1 && text[i + 1] != '\n'));
        // A paragraph's last line break shows nothing.
        runs = Keep(runs, (text, i, _) => i < text.Length - 1 || text[i] != '\n');
        _runs = null;
        var paragraph = new RichTextParagraph(runs.Select(run => new RichTextRun(run.Text, run.Formats)));
        // A line break shows a line even with nothing on it, such as a <br> between two paragraphs.
        if (paragraph.Runs.Count > 0 || hasBreak)
        {
            _paragraphs.Add(paragraph);
        }
    }

    // Keeps the characters the test passes, given the paragraph's text, an index into it, and whether it's in <pre>.
    private static List<(string Text, RichTextFormats Formats, bool Pre)> Keep(
        List<(string Text, RichTextFormats Formats, bool Pre)> runs, Func<string, int, bool, bool> keep)
    {
        var text = string.Concat(runs.Select(run => run.Text));
        var offset = 0;
        var result = new List<(string Text, RichTextFormats Formats, bool Pre)>(runs.Count);
        foreach (var run in runs)
        {
            var kept = new StringBuilder(run.Text.Length);
            for (var i = 0; i < run.Text.Length; i++)
            {
                if (keep(text, offset + i, run.Pre))
                {
                    kept.Append(run.Text[i]);
                }
            }

            offset += run.Text.Length;
            result.Add((kept.ToString(), run.Formats, run.Pre));
        }

        return result;
    }

    private static RichTextFormats FormatsOf(string name, string style, RichTextFormats inherited)
    {
        var formats = inherited;
        if (TagFormats.TryGetValue(name, out var tagFormat))
        {
            formats = With(formats, tagFormat);
        }

        foreach (var declaration in style.Split(';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon < 0)
            {
                continue;
            }

            var property = declaration[..colon].Trim().ToLowerInvariant();
            var value = declaration[(colon + 1)..].Replace("!important", "", StringComparison.OrdinalIgnoreCase)
                .Trim().ToLowerInvariant();
            formats = property switch
            {
                "font-weight" => Weight(formats, value),
                "font-style" when value is "italic" or "oblique" => With(formats, RichTextFormats.Italic),
                "font-style" when value == "normal" => formats & ~RichTextFormats.Italic,
                "font" when value is not ("inherit" or "initial" or "unset" or "revert") => Font(formats, value),
                "text-decoration" or "text-decoration-line" => Decoration(formats, value),
                "vertical-align" when value == "super" => With(formats, RichTextFormats.Superscript),
                "vertical-align" when value == "sub" => With(formats, RichTextFormats.Subscript),
                "vertical-align" when value == "baseline" =>
                    formats & ~(RichTextFormats.Subscript | RichTextFormats.Superscript),
                _ => formats,
            };
        }

        return formats;
    }

    private static RichTextFormats Weight(RichTextFormats formats, string value)
    {
        var number = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : double.NaN;
        if (value is "bold" or "bolder" || number >= 600)
        {
            return With(formats, RichTextFormats.Bold);
        }

        return value is "normal" or "lighter" || number < 600 ? formats & ~RichTextFormats.Bold : formats;
    }

    // The font shorthand resets the weight and style it doesn't name.
    private static RichTextFormats Font(RichTextFormats formats, string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        formats = parts.Any(part => Weight(RichTextFormats.None, part) == RichTextFormats.Bold)
            ? With(formats, RichTextFormats.Bold)
            : formats & ~RichTextFormats.Bold;
        return parts.Any(part => part is "italic" or "oblique")
            ? With(formats, RichTextFormats.Italic)
            : formats & ~RichTextFormats.Italic;
    }

    private static RichTextFormats Decoration(RichTextFormats formats, string value)
    {
        if (value.Contains("underline", StringComparison.Ordinal))
        {
            formats = With(formats, RichTextFormats.Underline);
        }

        return value.Contains("line-through", StringComparison.Ordinal)
            ? With(formats, RichTextFormats.Strikethrough)
            : formats;
    }

    private static RichTextFormats With(RichTextFormats formats, RichTextFormats format) => format switch
    {
        RichTextFormats.Subscript => (formats & ~RichTextFormats.Superscript) | format,
        RichTextFormats.Superscript => (formats & ~RichTextFormats.Subscript) | format,
        _ => formats | format,
    };

    private static bool IsDoctype(string markup) => markup.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase);

    // Quirks mode stays for non-HTML doctypes, HTML 2 or 3, 4.0 transitional or frameset, and 4.01 ones with no DTD URL.
    private static bool IsQuirksDoctype(string markup)
    {
        var doctype = Whitespace().Replace(markup, " ").ToLowerInvariant();
        var oldLoose = doctype.Contains("//dtd html 4.01 transitional//", StringComparison.Ordinal)
            || doctype.Contains("//dtd html 4.01 frameset//", StringComparison.Ordinal);
        return !doctype.StartsWith("<!doctype html", StringComparison.Ordinal)
            || doctype.Contains("//dtd html 2", StringComparison.Ordinal)
            || doctype.Contains("//dtd html 3", StringComparison.Ordinal)
            || doctype.Contains("//dtd html 4.0 transitional//", StringComparison.Ordinal)
            || doctype.Contains("//dtd html 4.0 frameset//", StringComparison.Ordinal)
            || (oldLoose && !doctype.Contains(".dtd", StringComparison.Ordinal));
    }

    // Reads to the end of a start tag, keeping only the style attribute and whether the tag closed itself.
    private (string Style, bool SelfClosing) ReadAttributes()
    {
        string? style = null;
        var selfClosing = false;
        while (_position < _html.Length)
        {
            var character = _html[_position];
            if (character == '>')
            {
                _position++;
                break;
            }

            selfClosing = character == '/' && CharAt(_position + 1) == '>';
            if (character == '/' || IsWhitespace(character))
            {
                _position++;
                continue;
            }

            // An attribute name runs to whitespace, "/", ">" or "=", though it may start with "=".
            var start = _position++;
            while (_position < _html.Length && !IsWhitespace(_html[_position])
                && _html[_position] is not ('/' or '>' or '='))
            {
                _position++;
            }

            var name = _html[start.._position];
            SkipWhitespace();
            var value = "";
            if (CharAt(_position) == '=')
            {
                _position++;
                SkipWhitespace();
                value = ReadAttributeValue();
            }

            if (style is null && name.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                style = HtmlCharacterReferences.Decode(value);
            }
        }

        return (style ?? "", selfClosing);
    }

    private string ReadAttributeValue()
    {
        var quote = CharAt(_position);
        if (quote is '"' or '\'')
        {
            var end = _html.IndexOf(quote, _position + 1);
            var value = end < 0 ? _html[(_position + 1)..] : _html[(_position + 1)..end];
            _position = end < 0 ? _html.Length : end + 1;
            return value;
        }

        var start = _position;
        while (_position < _html.Length && !IsWhitespace(_html[_position]) && _html[_position] != '>')
        {
            _position++;
        }

        return _html[start.._position];
    }

    private void ReadRawText(string name)
    {
        var end = name == "plaintext" ? -1 : FindEndTag(name);
        var content = _html[_position..(end < 0 ? _html.Length : end)];
        _position = end < 0 ? _html.Length : end;
        AddText(content);
    }

    private int FindEndTag(string name)
    {
        var from = _position;
        while (true)
        {
            var index = _html.IndexOf("</" + name, from, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return -1;
            }

            var after = CharAt(index + 2 + name.Length);
            if (after is '>' or '/' || IsWhitespace(after))
            {
                return index;
            }

            from = index + 2;
        }
    }

    private string ReadName()
    {
        var start = _position;
        while (_position < _html.Length && !IsWhitespace(_html[_position]) && _html[_position] is not ('/' or '>'))
        {
            _position++;
        }

        return _html[start.._position].ToLowerInvariant();
    }

    private void SkipPast(char character)
    {
        var index = _html.IndexOf(character, _position);
        _position = index < 0 ? _html.Length : index + 1;
    }

    private void SkipWhitespace()
    {
        while (_position < _html.Length && IsWhitespace(_html[_position]))
        {
            _position++;
        }
    }

    private char CharAt(int index) => index < _html.Length ? _html[index] : '\0';

    private static bool IsWhitespace(char character) => character is ' ' or '\t' or '\n' or '\f';

    [GeneratedRegex("[\t\n\f\r ]+")]
    private static partial Regex Whitespace();

    // Word writes a list item's bullet or number as text, in a span styled mso-list: Ignore.
    [GeneratedRegex(@"mso-list:\s*ignore", RegexOptions.IgnoreCase)]
    private static partial Regex WordListMarker();

    private sealed class Element(string name, string style, RichTextFormats formats, bool block, bool pre, bool skipped,
        int firstParagraph)
    {
        public string Name { get; } = name;

        public string Style { get; } = style;

        // Changes when a formatting element around it ends while it stays open.
        public RichTextFormats Formats { get; set; } = formats;

        public bool Block { get; } = block;

        public bool Pre { get; } = pre;

        public bool Skipped { get; } = skipped;

        public int FirstParagraph { get; } = firstParagraph;

        public bool IsOpen { get; set; }
    }
}
