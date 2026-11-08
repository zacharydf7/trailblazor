using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Trailblazor;

// Follows a browser's parsing rules where they change the text or its formats; only text and formats ever come out.
internal sealed partial class RichTextHtmlReader
{
    public const RichTextFormats AllFormats = RichTextFormats.Bold | RichTextFormats.Italic | RichTextFormats.Underline
        | RichTextFormats.Strikethrough | RichTextFormats.Subscript | RichTextFormats.Superscript | RichTextFormats.Code
        | RichTextFormats.Link;

    public const RichTextBlocks AllBlocks = RichTextBlocks.Headings | RichTextBlocks.Quotes
        | RichTextBlocks.BulletedLists | RichTextBlocks.NumberedLists | RichTextBlocks.Rules;

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
        ["sup"] = RichTextFormats.Superscript, ["code"] = RichTextFormats.Code, ["kbd"] = RichTextFormats.Code,
        ["samp"] = RichTextFormats.Code, ["tt"] = RichTextFormats.Code,
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

    // The scopes an element of each name bounds, besides the inline scope that every block and table part bounds.
    private static readonly Dictionary<string, Scope[]> ScopesBounded = new (Scope Scope, string[] Names)[]
        {
            (Scope.Button, ButtonScope), (Scope.ListItem, ListItemScope), (Scope.Definition, DefinitionScope),
            (Scope.Table, TableScope), (Scope.Block, BlockScope),
        }
        .SelectMany(entry => entry.Names, (entry, name) => (Name: name, entry.Scope))
        .GroupBy(pair => pair.Name, pair => pair.Scope).ToDictionary(group => group.Key, group => group.ToArray());

    private static readonly string[] Headings = ["h1", "h2", "h3", "h4", "h5", "h6"];

    private readonly string _html;
    private readonly RichTextFormats _formats;
    private readonly RichTextSchema _schema;
    private readonly List<Element> _open = [];
    // Formatting to reopen, in order; null marks a boundary.
    private readonly List<Element?> _formatting = [];
    private readonly List<RichTextParagraph> _paragraphs = [];
    // Open elements that rules look for, oldest first, so finding one needs no walk down the open elements.
    private readonly Dictionary<string, List<Element>> _openByName = [];
    private readonly List<Element>[] _openBounds = [.. Enum.GetValues<Scope>().Select(_ => new List<Element>())];
    private readonly List<Element> _openSkipped = [];
    private readonly List<Element> _openForeign = [];
    private List<Piece>? _runs;
    // The kind and level of the paragraph being read, set when its first text arrives.
    private (RichTextParagraphKind Kind, int Level) _block;
    // The Word list marker being skipped and its text, which says, once it ends, whether its list is numbered.
    private Element? _markerElement;
    private StringBuilder? _marker;
    private bool _numberedMarker;
    private int _position;
    private int _opened;
    private bool _dropNewline;
    private bool _started;
    private bool _inBody;
    // Without a standards doctype, a browser parses in quirks mode, where a table doesn't end a paragraph.
    private bool _quirks = true;

    private RichTextHtmlReader(string html, RichTextSchema schema)
    {
        _html = html.Replace("\r\n", "\n").Replace('\r', '\n');
        _formats = schema.Formats & AllFormats;
        _schema = schema;
    }

    private RichTextFormats Formats => _open.Count > 0 ? _open[^1].Formats : RichTextFormats.None;

    private LinkState OpenLink => _open.Count > 0 ? _open[^1].Link : default;

    private BlockContext OpenContext => _open.Count > 0 ? _open[^1].Context : default;

    private string? Link => _formats.HasFlag(RichTextFormats.Link) ? OpenLink.Kept : null;

    private bool InPre => _open.Count > 0 && _open[^1].Pre;

    private bool InSkipped => _open.Count > 0 && _open[^1].Skipped;

    private bool InOpenHead => _open.Count > 0 && _open[^1].Name == "head";

    private bool InForeignContent => _openForeign.Count > 0;

    public static List<RichTextParagraph> Read(string html, RichTextSchema schema)
    {
        var reader = new RichTextHtmlReader(html, schema);
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
            var (style, href, selfClosing) = ReadAttributes();
            StartTag(name, style, href, selfClosing);
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

    private void StartTag(string name, string style, string? href, bool selfClosing)
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

        if (!InSkipped && TableParts.Contains(name) && LastOpen("table") is null)
        {
            return;
        }

        if (!InSkipped && LastOpen("p") is not null && ClosesParagraph.Contains(name) && !(name == "table" && _quirks))
        {
            Close("p");
        }

        if (Void.Contains(name))
        {
            // A rule between two lines leaves them separate paragraphs.
            if (name == "hr" && !InSkipped)
            {
                FinishParagraph();
                if (_schema.Blocks.HasFlag(RichTextBlocks.Rules))
                {
                    _paragraphs.Add(new RichTextParagraph([], RichTextParagraphKind.Rule));
                }
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

        var marker = !InSkipped && WordListMarker().IsMatch(style);
        if (InSkipped || Skipped.Contains(name) || marker)
        {
            // A self-closing <svg/> or <math/> ends at once, as it does in a browser.
            if (!(selfClosing && name is "svg" or "math"))
            {
                var skipped = new Element(name, style, default, Formats, OpenLink, OpenContext, block: false, InPre,
                    skipped: true, _paragraphs.Count);
                Push(skipped);
                if (marker)
                {
                    (_markerElement, _marker) = (skipped, new StringBuilder());
                }
            }

            return;
        }

        // As in a browser, a link starting inside another ends that one first.
        if (name == "a")
        {
            EndFormatting("a");
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
        var target = name == "a" ? RichTextSchema.CleanLink(href) : null;
        var own = new LinkState(target, target is not null && _schema.AllowsLink(target) ? target : null);
        var link = LinkOf(name, own, OpenLink);
        var element = new Element(name, style, own, FormatsOf(name, style, Formats, link.Target is not null), link,
            ContextOf(name, style, OpenContext), block, InPre || pre, skipped: false, _paragraphs.Count);
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
        // Any heading's end tag closes whichever heading is open.
        var closed = Headings.Contains(name)
            ? Headings.Select(LastOpen).OfType<Element>().MaxBy(element => element.Serial)
            : LastOpen(name);
        var bound = _openBounds[(int)ScopeOf(name)] is [.., var last] ? last : null;
        // Everything above the closed element closes with it, so the search costs no more than closing them.
        return closed is null || bound?.Serial > closed.Serial ? -1 : _open.LastIndexOf(closed);
    }

    // Inside skipped content an end tag closes only what opened there.
    private int ClosedInSkipped(string name)
    {
        var closed = LastOpen(name);
        return closed is not null && closed.Serial >= _openSkipped[0].Serial ? _open.LastIndexOf(closed) : -1;
    }

    // Whether an end tag inside open SVG or MathML names nothing opened there, so the HTML rules apply to it.
    private bool LeavesForeignContent(string name)
    {
        var root = _openSkipped[0];
        return root.Name is "svg" or "math" && (LastOpen(name)?.Serial ?? 0) < root.Serial;
    }

    private Element? LastOpen(string name) =>
        _openByName.TryGetValue(name, out var open) && open.Count > 0 ? open[^1] : null;

    // The kind of scope an end tag closes within, which decides the open elements that stop it.
    private static Scope ScopeOf(string name) => name switch
    {
        "p" => Scope.Button,
        "li" => Scope.ListItem,
        "dd" or "dt" => Scope.Definition,
        _ when TableParts.Contains(name) || name == "table" => Scope.Table,
        _ when Blocks.Contains(name) => Scope.Block,
        _ => Scope.Inline,
    };

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
            Track(element, open: false);
            PopTo(_open.FindLastIndex(open => open.Block) + 1);
            for (var i = index; i < _open.Count; i++)
            {
                var parent = i > 0 ? _open[i - 1].Formats : RichTextFormats.None;
                var parentLink = i > 0 ? _open[i - 1].Link : default;
                _open[i].Link = _open[i].Skipped ? parentLink : LinkOf(_open[i].Name, _open[i].Own, parentLink);
                _open[i].Formats = _open[i].Skipped ? parent
                    : FormatsOf(_open[i].Name, _open[i].Style, parent, _open[i].Link.Target is not null);
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
            var link = LinkOf(entry.Name, entry.Own, OpenLink);
            var reopened = new Element(entry.Name, entry.Style, entry.Own,
                FormatsOf(entry.Name, entry.Style, Formats, link.Target is not null), link, OpenContext, block: false,
                InPre, skipped: false, _paragraphs.Count);
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
        element.Serial = ++_opened;
        Track(element, open: true);
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

    private void Track(Element element, bool open)
    {
        if (!_openByName.TryGetValue(element.Name, out var named))
        {
            named = _openByName[element.Name] = [];
        }

        Toggle(named, element, open);
        if (element.Block || TableParts.Contains(element.Name))
        {
            Toggle(_openBounds[(int)Scope.Inline], element, open);
        }

        foreach (var scope in ScopesBounded.GetValueOrDefault(element.Name, []))
        {
            Toggle(_openBounds[(int)scope], element, open);
        }

        if (element.Skipped)
        {
            Toggle(_openSkipped, element, open);
        }

        if (element.Name is "svg" or "math")
        {
            Toggle(_openForeign, element, open);
        }
    }

    // Elements almost always close from the top, so the search from the end is short.
    private static void Toggle(List<Element> elements, Element element, bool open)
    {
        if (open)
        {
            elements.Add(element);
        }
        else
        {
            elements.RemoveAt(elements.LastIndexOf(element));
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

    private void CloseForeignContent() => PopTo(_open.LastIndexOf(_openForeign[0]));

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
        Track(element, open: false);
        if (element == _markerElement)
        {
            _numberedMarker = NumberedMarker().IsMatch(_marker!.ToString().Trim());
            (_markerElement, _marker) = (null, null);
        }

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
                var (kind, level) = Resolve(element.Context);
                _numberedMarker = false;
                _paragraphs.Add(new RichTextParagraph([], kind, level));
            }
        }
    }

    // An element's own heading, quote, list item or Word list paragraph, if the schema allows it, else its parent's.
    private BlockContext ContextOf(string name, string style, BlockContext parent)
    {
        var context = name is "ul" or "ol" or "menu" or "dir"
            ? parent with { Lists = parent.Lists + 1, Ordered = name == "ol" }
            : parent;
        var own = name switch
        {
            "h1" => (RichTextParagraphKind.Heading1, 0, false),
            "h2" => (RichTextParagraphKind.Heading2, 0, false),
            "h3" or "h4" or "h5" or "h6" => (RichTextParagraphKind.Heading3, 0, false),
            "blockquote" => (RichTextParagraphKind.Quote, 0, false),
            "li" => (parent.Ordered ? RichTextParagraphKind.NumberedItem : RichTextParagraphKind.BulletedItem,
                Math.Max(parent.Lists - 1, 0), false),
            _ when WordListLevel().Match(style) is { Success: true } match => (RichTextParagraphKind.BulletedItem,
                Math.Max(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) - 1, 0), true),
            _ => ((RichTextParagraphKind, int, bool)?)null,
        };
        if (own is not var (kind, level, word)
            || !(word ? Allows(RichTextParagraphKind.BulletedItem) || Allows(RichTextParagraphKind.NumberedItem)
                : Allows(kind)))
        {
            return context;
        }

        return context with { Kind = kind, Level = level, WordList = word };
    }

    private (RichTextParagraphKind Kind, int Level) CurrentBlock() => Resolve(OpenContext);

    // A Word list paragraph is numbered when its skipped marker is a number or letters.
    private (RichTextParagraphKind Kind, int Level) Resolve(BlockContext context)
    {
        if (!context.WordList)
        {
            return (context.Kind, context.Level);
        }

        var kind = _numberedMarker ? RichTextParagraphKind.NumberedItem : RichTextParagraphKind.BulletedItem;
        return Allows(kind) ? (kind, context.Level) : (RichTextParagraphKind.Paragraph, 0);
    }

    private bool Allows(RichTextParagraphKind kind) => _schema.Blocks.HasFlag(kind switch
    {
        RichTextParagraphKind.Heading1 or RichTextParagraphKind.Heading2 or RichTextParagraphKind.Heading3 =>
            RichTextBlocks.Headings,
        RichTextParagraphKind.Quote => RichTextBlocks.Quotes,
        RichTextParagraphKind.BulletedItem => RichTextBlocks.BulletedLists,
        RichTextParagraphKind.NumberedItem => RichTextBlocks.NumberedLists,
        _ => RichTextBlocks.Rules,
    });

    // An <a> with an href starts a link, and elements inside a link carry it.
    private static LinkState LinkOf(string name, LinkState own, LinkState parent) => name == "a" ? own : parent;

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

        if (InSkipped)
        {
            // Only the marker's own text: the list paragraph's skipped content after it says nothing about the list.
            _marker?.Append(text);
            return;
        }

        if (text.Length > 0)
        {
            AddPiece(InPre ? text : Whitespace().Replace(text, " "), InPre);
        }
    }

    private void AddBreak()
    {
        if (!InSkipped)
        {
            AddPiece("\n", pre: false);
        }
    }

    private void AddPiece(string text, bool pre)
    {
        Reconstruct();
        if (_runs is null)
        {
            _block = CurrentBlock();
            _numberedMarker = false;
            _runs = [];
        }

        _runs.Add(new Piece(text, Formats & _formats, Link, pre));
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
        var paragraph = new RichTextParagraph(
            runs.Select(run => new RichTextRun(run.Text, run.Formats, run.Link, vetted: true)), _block.Kind,
            _block.Level);
        // A line break shows a line even with nothing on it, such as a <br> between two paragraphs.
        if (paragraph.Runs.Count > 0 || hasBreak)
        {
            _paragraphs.Add(paragraph);
        }
    }

    // Keeps the characters the test passes, given the paragraph's text, an index into it, and whether it's in <pre>.
    private static List<Piece> Keep(List<Piece> runs, Func<string, int, bool, bool> keep)
    {
        var text = string.Concat(runs.Select(run => run.Text));
        var offset = 0;
        var result = new List<Piece>(runs.Count);
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
            result.Add(run with { Text = kept.ToString() });
        }

        return result;
    }

    // Inside a link, an underline from a style is the link's own look, as Google Docs writes it, not a format.
    private static RichTextFormats FormatsOf(string name, string style, RichTextFormats inherited, bool inLink)
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
                "text-decoration" or "text-decoration-line" => Decoration(formats, value, inLink),
                "font-family" when IsMonospace(value) => With(formats, RichTextFormats.Code),
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

    private static RichTextFormats Decoration(RichTextFormats formats, string value, bool inLink)
    {
        if (!inLink && value.Contains("underline", StringComparison.Ordinal))
        {
            formats = With(formats, RichTextFormats.Underline);
        }

        return value.Contains("line-through", StringComparison.Ordinal)
            ? With(formats, RichTextFormats.Strikethrough)
            : formats;
    }

    // Google Docs and Word have no code format; people set code in a monospace font there, so that reads as code.
    private static bool IsMonospace(string families) => families.Split(',')
        .Select(family => family.Trim().Trim('"', '\'').ToLowerInvariant())
        .Any(family => family is "monospace" or "courier new" or "courier" or "consolas" or "menlo" or "monaco");

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

    // Reads to the end of a start tag, keeping only the style and href attributes and whether the tag closed itself.
    private (string Style, string? Href, bool SelfClosing) ReadAttributes()
    {
        string? style = null;
        string? href = null;
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

            // As in a browser, the first of a repeated attribute counts.
            if (style is null && name.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                style = HtmlCharacterReferences.DecodeAttribute(value);
            }
            else if (href is null && name.Equals("href", StringComparison.OrdinalIgnoreCase))
            {
                href = HtmlCharacterReferences.DecodeAttribute(value);
            }
        }

        return (style ?? "", href, selfClosing);
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

    // Word writes a list paragraph's level in its style, as in mso-list:l0 level2 lfo1.
    [GeneratedRegex(@"mso-list:\s*l\d+\s+level(\d{1,2})", RegexOptions.IgnoreCase)]
    private static partial Regex WordListLevel();

    // A numbered list's marker is a number or letters with a period or parenthesis, as in "1.", "a)" or "iv.".
    [GeneratedRegex(@"^\(?([0-9]+|[a-z]+|[A-Z]+)[.)]$")]
    private static partial Regex NumberedMarker();

    private sealed class Element(string name, string style, LinkState own, RichTextFormats formats, LinkState link,
        BlockContext context, bool block, bool pre, bool skipped, int firstParagraph)
    {
        public string Name { get; } = name;

        public string Style { get; } = style;

        // An <a>'s own href, which a reopened copy of it keeps, so its scheme is checked once.
        public LinkState Own { get; } = own;

        // Changes when a formatting element around it ends while it stays open.
        public RichTextFormats Formats { get; set; } = formats;

        // The link the text inside belongs to; changes as Formats does.
        public LinkState Link { get; set; } = link;

        public BlockContext Context { get; } = context;

        public bool Block { get; } = block;

        public bool Pre { get; } = pre;

        public bool Skipped { get; } = skipped;

        public int FirstParagraph { get; } = firstParagraph;

        public bool IsOpen { get; set; }

        // Rises with each element opened, so of two open elements the one with the higher serial is above.
        public int Serial { get; set; }
    }

    private sealed record Piece(string Text, RichTextFormats Formats, string? Link, bool Pre);

    // Target is any href (a styled underline under it is the link's look); Kept is Target if the schema allows it.
    private readonly record struct LinkState(string? Target, string? Kept);

    // The kind and level of paragraph an element's text goes in, and the count of lists around it.
    private readonly record struct BlockContext(RichTextParagraphKind Kind, int Level, bool WordList, int Lists,
        bool Ordered);

    private enum Scope
    {
        Inline,
        Button,
        ListItem,
        Definition,
        Table,
        Block,
    }
}
