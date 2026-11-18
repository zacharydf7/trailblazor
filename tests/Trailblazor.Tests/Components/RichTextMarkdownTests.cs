namespace Trailblazor.Tests.Components;

public class RichTextMarkdownTests
{
    private static readonly string Corpus = Path.Combine(AppContext.BaseDirectory, "RichTextCorpus");

    public static TheoryData<string> CorpusSamples => new(Directory.GetFiles(Corpus, "*.html")
        .Where(path => !path.EndsWith(".expected.html", StringComparison.Ordinal))
        .Select(Path.GetFileNameWithoutExtension)
        .Order()!);

    [Theory]
    [InlineData("# Title\n\nSome **bold** and *italic* text.",
        "<h1>Title</h1><p>Some <strong>bold</strong> and <em>italic</em> text.</p>")]
    [InlineData("## Two\n### Three\n#### Four ##", "<h2>Two</h2><h3>Three</h3><h3>Four</h3>")]
    [InlineData("Title\n=====\n\nSubtitle\n---", "<h1>Title</h1><h2>Subtitle</h2>")]
    [InlineData("- a\n- b\n    1. c\n    2. d\n- e",
        "<ul><li>a</li><li>b<ol><li>c</li><li>d</li></ol></li><li>e</li></ul>")]
    [InlineData("* a\n  + b\n\n1) one", "<ul><li>a<ul><li>b</li></ul></li></ul><ol><li>one</li></ol>")]
    [InlineData("> one\n> two\n>\n> three", "<blockquote><p>one two</p><p>three</p></blockquote>")]
    [InlineData("a\n\n***\n\nb", "<p>a</p><hr><p>b</p>")]
    [InlineData("See ![a map](/map.png) here", "<p>See a map here</p>")]
    [InlineData("Run `dotnet test` now, or ``a`b``", "<p>Run <code>dotnet test</code> now, or <code>a`b</code></p>")]
    [InlineData("```cs\nline 1\nline 2\n```", "<p><code>line 1<br>line 2</code></p>")]
    [InlineData("[Trails](https://example.com/a \"Title\") and <https://example.org>",
        "<p><a href=\"https://example.com/a\">Trails</a> and "
            + "<a href=\"https://example.org\">https://example.org</a></p>")]
    [InlineData("[**bold** link](/a)", "<p><a href=\"/a\"><strong>bold</strong> link</a></p>")]
    [InlineData("~~gone~~ and ***both***", "<p><s>gone</s> and <strong><em>both</em></strong></p>")]
    [InlineData("***m**n* and ***m*n**",
        "<p><strong><em>m</em></strong><em>n</em> and <strong><em>m</em></strong><strong>n</strong></p>")]
    [InlineData("one  \ntwo\\\nthree\nfour", "<p>one<br>two<br>three four</p>")]
    [InlineData("\\*not italic\\* and snake_case_name", "<p>*not italic* and snake_case_name</p>")]
    [InlineData("2 * 3 * 4 and a ~ b", "<p>2 * 3 * 4 and a ~ b</p>")]
    [InlineData("<b>hi</b>", "<p>&lt;b&gt;hi&lt;/b&gt;</p>")]
    [InlineData("- item\nlazy line", "<ul><li>item lazy line</li></ul>")]
    [InlineData("```js``` is code, not a fence\n\nafter", "<p><code>js</code> is code, not a fence</p><p>after</p>")]
    public void FromMarkdown_ShouldReadTheMarkdownsBlocksAndFormats_WhenGivenMarkdown(string markdown, string html)
    {
        Assert.Equal(html, RichTextDocument.FromMarkdown(markdown).ToHtml());
    }

    [Theory]
    [InlineData("![A ridge](/ridge.png)", "<img src=\"/ridge.png\" alt=\"A ridge\">")]
    [InlineData("![A ridge](ftp://x/r.png)", "")]
    [InlineData("**[ftp](ftp://x)** [site](https://x.test)",
        "<p><strong>ftp</strong> <a href=\"https://x.test\">site</a></p>")]
    public void FromMarkdown_ShouldKeepWhatTheSchemaAllows_WhenGivenOne(string markdown, string html)
    {
        var schema = new RichTextSchema { Blocks = RichTextSchema.Default.Blocks | RichTextBlocks.Images };

        Assert.Equal(html, RichTextDocument.FromMarkdown(markdown, schema).ToHtml());
    }

    [Fact]
    public void FromMarkdown_ShouldLeaveOutImagesAndOtherFormats_WhenGivenTheDefaultOrANarrowSchema()
    {
        var narrow = new RichTextSchema { Formats = RichTextFormats.Bold, Blocks = RichTextBlocks.None };

        Assert.Equal("", RichTextDocument.FromMarkdown("![A ridge](/ridge.png)").ToHtml());
        Assert.Equal("<p><strong>a</strong> b</p><p>c</p>",
            RichTextDocument.FromMarkdown("**a** *b*\n\n# c", narrow).ToHtml());
    }

    [Theory]
    [InlineData("[click](javascript:alert(1))", "<p>click</p>")]
    [InlineData("<javascript:alert(1)>", "<p>&lt;javascript:alert(1)&gt;</p>")]
    [InlineData("![x](vbscript:run)", "<p>x</p>")]
    public void FromMarkdown_ShouldKeepTheText_WhenALinkOrImageWouldRunScript(string markdown, string html)
    {
        var images = new RichTextSchema { Blocks = RichTextSchema.Default.Blocks | RichTextBlocks.Images };

        Assert.Equal(html, RichTextDocument.FromMarkdown(markdown, images).ToHtml());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\n  \n")]
    public void FromMarkdown_ShouldGiveAnEmptyDocument_WhenGivenNoText(string? markdown)
    {
        Assert.True(RichTextDocument.FromMarkdown(markdown).IsEmpty);
    }

    [Theory]
    [InlineData("<p><strong>bold <em>both</em></strong> <em>italic</em> plain <em><code>f</code></em> <em>g</em></p>")]
    [InlineData("<p><a href=\"https://x.test/a b\">link <strong>bold</strong></a> after</p>")]
    [InlineData("<h2>Title with *stars*, _under_ and [brackets]</h2>")]
    [InlineData("<ul><li>one<ul><li>two</li></ul></li><li>three</li></ul><ol><li>first</li><li>second</li></ol>")]
    [InlineData("<blockquote><p>quoted</p><p>again</p></blockquote><p>after</p>")]
    [InlineData("<p>1. not a list</p><p># not a heading</p><p>- not an item</p><p>&gt; not a quote</p>")]
    [InlineData("<p>line<br>break</p><ul><li>item<br>more</li></ul><blockquote><p>a<br>b</p></blockquote>")]
    [InlineData("<p><code>a`b</code> and <s>struck</s> and <code>`edge`</code></p>")]
    [InlineData("<hr><p>after rule</p>")]
    [InlineData("<p><code>one<br>two</code></p>")]
    [InlineData("<p>2 * 3 and a \\ b ~~not struck~~</p>")]
    [InlineData("<p><strong><em>m</em></strong><em>n</em> <strong><em>o</em>p</strong></p>")]
    [InlineData("<p>a\\<br>b</p>")]
    [InlineData("<p><code>a``b</code> x</p><p>after</p>")]
    public void ToMarkdown_ShouldReadBackTheSameDocument_WhenWritten(string html)
    {
        var document = RichTextDocument.Parse(html);

        Assert.Equal(document.ToHtml(), RichTextDocument.FromMarkdown(document.ToMarkdown()).ToHtml());
    }

    [Fact]
    public void ToMarkdown_ShouldReadBackAnImage_WhenTheSchemaAllowsImages()
    {
        var images = new RichTextSchema { Blocks = RichTextSchema.Default.Blocks | RichTextBlocks.Images };
        var document = RichTextDocument.Parse("<p>a</p><img src=\"/a b.png\" alt=\"Alt [text]\">", images);

        Assert.Equal(document.ToHtml(), RichTextDocument.FromMarkdown(document.ToMarkdown(), images).ToHtml());
    }

    [Fact]
    public void ToMarkdown_ShouldWriteTheFewestMarks_WhenFormatsOverlap()
    {
        var document = RichTextDocument.Parse(
            "<h1>Trails</h1><p><strong>Bold <em>and italic</em></strong> then <a href=\"/a\">a link</a></p>"
            + "<ol><li>one</li><li>two<ul><li>nested</li></ul></li></ol><blockquote><p>q</p></blockquote>");

        Assert.Equal("# Trails\n\n**Bold *and italic*** then [a link](/a)\n\n1. one\n2. two\n    - nested\n\n> q",
            document.ToMarkdown());
    }

    [Fact]
    public void ToMarkdown_ShouldWriteUnderlineSubscriptAndSuperscriptPlain_WhenCalled()
    {
        var document = RichTextDocument.Parse("<p><u>under</u> H<sub>2</sub>O x<sup>2</sup></p>");

        Assert.Equal("under H2O x2", document.ToMarkdown());
    }

    [Theory]
    [InlineData("<p>a<em> b </em>c</p>", "a *b* c")]
    [InlineData("<p>&nbsp; lead and trail &nbsp;</p>", "lead and trail")]
    [InlineData("<p>a</p><p><br></p><p>b</p>", "a\n\nb")]
    [InlineData("<p>a<br><br></p><p><br><br></p><p>b</p>", "a\n\nb")]
    [InlineData("<h2>a<br>b</h2><p>see <code>c<br>d</code></p>", "## a b\n\nsee `c d`")]
    [InlineData("<p><strong>a<br></strong>b</p><p>a<strong><br>b</strong></p>", "**a**\\\nb\n\na\\\n**b**")]
    public void ToMarkdown_ShouldWriteWhatMarkdownCanSay_WhenGivenWhatItCant(string html,
        string markdown)
    {
        Assert.Equal(markdown, RichTextDocument.Parse(html).ToMarkdown());
    }

    // Markdown can't say underline, subscript, superscript, an empty paragraph or spaces at a line's ends, and a space
    // at a format's edge moves outside it, so this reads the markdown back, checks it writes the same again, and
    // compares the paragraphs' text.
    [Theory, MemberData(nameof(CorpusSamples))]
    public void ToMarkdown_ShouldReadBackEachCorpusSample_WhenWritten(string sample)
    {
        var schema = new RichTextSchema
        {
            Blocks = RichTextSchema.Default.Blocks | RichTextBlocks.Images | RichTextBlocks.Tables,
        };
        var document = RichTextDocument.Parse(File.ReadAllText(Path.Combine(Corpus, $"{sample}.html")), schema);
        var markdown = document.ToMarkdown();

        var read = RichTextDocument.FromMarkdown(markdown, schema);

        Assert.Equal(markdown, read.ToMarkdown());
        Assert.Equal(Shape(document), Shape(read));
    }

    private static readonly RichTextSchema WithTables =
        new() { Blocks = RichTextSchema.Default.Blocks | RichTextBlocks.Tables };

    [Theory]
    [InlineData("| Trail | Miles |\n| :--- | ---: |\n| Pine **ridge** | 3 |\n| Lake |",
        "<table><thead><tr><th scope=\"col\">Trail</th><th scope=\"col\">Miles</th></tr></thead><tbody>"
        + "<tr><td>Pine <strong>ridge</strong></td><td>3</td></tr><tr><td>Lake</td><td><br></td></tr></tbody></table>")]
    [InlineData("a | b\n--|--\n1 | 2 | 3",
        "<table><thead><tr><th scope=\"col\">a</th><th scope=\"col\">b</th></tr></thead><tbody>"
        + "<tr><td>1</td><td>2</td></tr></tbody></table>")]
    [InlineData("| | |\n|---|---|\n| 1 | 2 |", "<table><tbody><tr><td>1</td><td>2</td></tr></tbody></table>")]
    [InlineData("| a \\| b | `c \\| d` |\n|---|---|",
        "<table><thead><tr><th scope=\"col\">a | b</th><th scope=\"col\"><code>c | d</code></th></tr></thead></table>")]
    [InlineData("intro\n| a |\n| --- |\n| 1 |\nmore\n\nafter",
        "<p>intro</p><table><thead><tr><th scope=\"col\">a</th></tr></thead><tbody><tr><td>1</td></tr>"
        + "<tr><td>more</td></tr></tbody></table><p>after</p>")]
    [InlineData("| a |\n|---|\n| 1 |\n> quote",
        "<table><thead><tr><th scope=\"col\">a</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>"
        + "<blockquote><p>quote</p></blockquote>")]
    [InlineData("| a | b |\n| --- |", "<p>| a | b | | --- |</p>")]
    public void FromMarkdown_ShouldReadPipeTables_WhenTheSchemaAllowsTables(string markdown, string html) =>
        Assert.Equal(html, RichTextDocument.FromMarkdown(markdown, WithTables).ToHtml());

    [Fact]
    public void FromMarkdown_ShouldReadEachCellAsAParagraph_WhenTheSchemaDoesNotAllowTables() =>
        Assert.Equal("<p>a</p><p>1</p>", RichTextDocument.FromMarkdown("| a |\n|---|\n| 1 |").ToHtml());

    [Fact]
    public void ToMarkdown_ShouldWriteAPipeTable_WhenTheDocumentHasATable()
    {
        var document = new RichTextDocument([
            new([new("Trail")], new RichTextTableCell(0, 0, header: true)),
            new([new("a|b", RichTextFormats.Code)], new RichTextTableCell(0, 1, header: true)),
            new([new("x | y")], new RichTextTableCell(1, 0)),
            new([new("1\n2")], new RichTextTableCell(1, 1)),
            new([new("--- | ---")]),
            new([new("p")], new RichTextTableCell(0, 0)),
            new([], new RichTextTableCell(0, 1)),
        ]);

        Assert.Equal("| Trail | `a\\|b` |\n| --- | --- |\n| x \\| y | 1 2 |\n\n\\--- | ---\n\n"
            + "|  |  |\n| --- | --- |\n| p |  |", document.ToMarkdown());
    }

    [Fact]
    public void ToMarkdown_ShouldKeepEachCellInItsColumn_WhenCodeInACellHasABackslashBeforeAPipe()
    {
        var document = new RichTextDocument([
            new([new("a\\|b", RichTextFormats.Code)], new RichTextTableCell(0, 0)),
            new([new("next")], new RichTextTableCell(0, 1)),
        ]);

        var read = RichTextDocument.FromMarkdown(document.ToMarkdown(), WithTables);

        Assert.Equal([("a\\\\|b", new RichTextTableCell(0, 0)), ("next", new RichTextTableCell(0, 1))],
            read.Paragraphs.Select(paragraph => (paragraph.Text, paragraph.Cell!)));
    }

    [Fact]
    public void ToMarkdown_ShouldReadBackTheSameTables_WhenWritten()
    {
        var document = new RichTextDocument([
            new([new("Trail", RichTextFormats.Bold), new("\\|")], new RichTextTableCell(0, 0, header: true)),
            new([new("a|b", RichTextFormats.Code)], new RichTextTableCell(0, 1, header: true)),
            new([new("map", link: "https://x/?a|b")], new RichTextTableCell(1, 0)),
            new([], new RichTextTableCell(1, 1)),
            new([new("| --- |")]),
            new([new("p")], new RichTextTableCell(0, 0)),
            new([new("q")], new RichTextTableCell(0, 1)),
        ]);

        var read = RichTextDocument.FromMarkdown(document.ToMarkdown(), WithTables);

        Assert.Equal(document.Paragraphs.Select(paragraph => (paragraph.Kind, paragraph.Cell)),
            read.Paragraphs.Select(paragraph => (paragraph.Kind, paragraph.Cell)));
        Assert.Equal(document.Paragraphs.Select(paragraph => paragraph.Runs.ToList()),
            read.Paragraphs.Select(paragraph => paragraph.Runs.ToList()));
    }

    // Each paragraph's kind, level and lines without spaces at their ends, leaving out the empty paragraphs. A cell's
    // lines are joined with spaces, since markdown can't break a line in a cell.
    private static List<(RichTextParagraphKind, int, string)> Shape(RichTextDocument document) =>
        [.. document.Paragraphs.Where(paragraph => paragraph.Kind is not (RichTextParagraphKind.Paragraph
                or RichTextParagraphKind.Quote) || paragraph.Text.Length > 0)
            .Select(paragraph => (paragraph.Kind, paragraph.Level,
                string.Join(paragraph.Cell is null ? "\n" : " ",
                    paragraph.Text.Split('\n').Select(line => line.Trim(' ')))))];
}
