using System.Diagnostics;

namespace Trailblazor.Tests.Components;

public class RichTextDocumentTests
{
    private static readonly string Corpus = Path.Combine(AppContext.BaseDirectory, "RichTextCorpus");

    public static TheoryData<string> CorpusSamples => new(Directory.GetFiles(Corpus, "*.html")
        .Where(path => !path.EndsWith(".expected.html", StringComparison.Ordinal))
        .Select(Path.GetFileNameWithoutExtension)
        .Order()!);

    [Theory, MemberData(nameof(CorpusSamples))]
    public void Parse_ShouldReadTheExpectedValue_WhenGivenACorpusSample(string sample)
    {
        var html = File.ReadAllText(Path.Combine(Corpus, $"{sample}.html"));
        var expected = File.ReadAllText(Path.Combine(Corpus, $"{sample}.expected.html")).TrimEnd('\r', '\n');

        Assert.Equal(expected, RichTextDocument.Parse(html).ToHtml());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    [InlineData("<p></p>")]
    [InlineData("<p><br></p>")]
    [InlineData("<script>alert(1)</script>")]
    public void Parse_ShouldGiveAnEmptyDocument_WhenGivenNoText(string? html)
    {
        var document = RichTextDocument.Parse(html);

        Assert.True(document.IsEmpty);
        Assert.Equal("", document.ToHtml());
    }

    [Fact]
    public void Parse_ShouldExposeParagraphsAndRuns_WhenGivenFormattedText()
    {
        var document = RichTextDocument.Parse("<p>a<strong>b</strong></p><p><em>c</em></p>");

        Assert.Collection(document.Paragraphs,
            paragraph => Assert.Equal([new("a"), new("b", RichTextFormats.Bold)], paragraph.Runs),
            paragraph => Assert.Equal([new("c", RichTextFormats.Italic)], paragraph.Runs));
    }

    [Theory]
    [InlineData("<span style=\"font-weight: 700\">a</span>", "<p><strong>a</strong></p>")]
    [InlineData("<span style=\"FONT-WEIGHT: bold !important\">a</span>", "<p><strong>a</strong></p>")]
    [InlineData("<span style=\"font-weight: 500\">a</span>", "<p>a</p>")]
    [InlineData("<b style=\"font-weight: normal\">a</b>", "<p>a</p>")]
    [InlineData("<i style=\"font-style: normal\">a</i>", "<p>a</p>")]
    [InlineData("<span style=\"font: italic bold 12px Arial\">a</span>", "<p><strong><em>a</em></strong></p>")]
    [InlineData("<b><span style=\"font: 12px Arial\">a</span></b>", "<p>a</p>")]
    [InlineData("<span style=\"text-decoration: underline line-through\">a</span>", "<p><u><s>a</s></u></p>")]
    [InlineData("<sub><span style=\"vertical-align: super\">a</span></sub>", "<p><sup>a</sup></p>")]
    [InlineData("<sup style=\"vertical-align: baseline\">a</sup>", "<p>a</p>")]
    [InlineData("<ins>a</ins><del>b</del><strike>c</strike>", "<p><u>a</u><s>bc</s></p>")]
    public void Parse_ShouldReadFormats_WhenGivenTagsAndInlineStyles(string html, string expected) =>
        Assert.Equal(expected, RichTextDocument.Parse(html).ToHtml());

    [Theory]
    [InlineData("a<p>b</p>c", "<p>a</p><p>b</p><p>c</p>")]
    [InlineData("<p>a<p>b", "<p>a</p><p>b</p>")]
    [InlineData("<p style=\"font-weight: bold\">a<div>b</div>", "<p><strong>a</strong></p><p>b</p>")]
    [InlineData("<ul><li>a<li>b</ul>", "<ul><li>a</li><li>b</li></ul>")]
    [InlineData("<dl><dt>a<dd>b</dl>", "<p>a</p><p>b</p>")]
    [InlineData("a</p>b", "<p>a</p><p><br></p><p>b</p>")]
    [InlineData("a</br>b", "<p>a<br>b</p>")]
    [InlineData("<p>a<br></p>", "<p>a</p>")]
    [InlineData("<p>a<br><br></p>", "<p>a<br><br></p>")]
    [InlineData("<p></p><p>a</p>", "<p><br></p><p>a</p>")]
    [InlineData("<pre>\n  a\n b</pre>", "<p>&nbsp;&nbsp;a<br>&nbsp;b</p>")]
    [InlineData("<!-->a", "<p>a</p>")]
    [InlineData("<head><title>t</title><body>a", "<p>a</p>")]
    [InlineData("<xmp><b>raw</b></xmp>", "<p>&lt;b&gt;raw&lt;/b&gt;</p>")]
    [InlineData("1 < 2 &amp; 3 &gt; 2", "<p>1 &lt; 2 &amp; 3 &gt; 2</p>")]
    [InlineData("<p>\n  one\n  two  </p>", "<p>one two</p>")]
    [InlineData("<p title='a > b' class=x>text</p>", "<p>text</p>")]
    [InlineData("<html><head><meta charset=utf-8><p>Hi</p>", "<p>Hi</p>")]
    [InlineData("<head><title>t</title>visible", "<p>visible</p>")]
    [InlineData("<!DOCTYPE html><p>c<table><tr><td>t</td></tr></table>d</p>", "<p>c</p><p>t</p><p>d</p><p><br></p>")]
    [InlineData("<!DOCTYPE html ><p>c<table><tr><td>t</td></tr></table>d</p>", "<p>c</p><p>t</p><p>d</p><p><br></p>")]
    [InlineData("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">"
        + "<p>c<table><tr><td>t</td></tr></table>d</p>",
        "<p>c</p><p>t</p><p>d</p>")]
    [InlineData("<span style=\"font-weight: bold\"><p>a</span>b</p>", "<p><strong>ab</strong></p>")]
    [InlineData("<div><table><tr><td>a</div>b</td><td>c</td></tr></table>", "<p>ab</p><p>c</p>")]
    [InlineData("<b><table><tr><td>x</b>y</td></tr></table>", "<p><strong>xy</strong></p>")]
    [InlineData("<div><section>a</div>b", "<p>a</p><p>b</p>")]
    [InlineData("<h1>Title</h2>Body", "<h1>Title</h1><p>Body</p>")]
    [InlineData("<p><template></p>hidden</template>", "")]
    [InlineData("<b><svg></b>after", "<p>after</p>")]
    [InlineData("<div><svg></div>after", "<p><br></p><p>after</p>")]
    [InlineData("<div><table><tr><td><svg></div>hidden</td></tr></table>", "")]
    [InlineData("<h1><svg></h2>x", "<h1><br></h1><p>x</p>")]
    [InlineData("<p>x<head></p>y", "<p>x</p><p>y</p>")]
    [InlineData("<b>x<head></b>y", "<p><strong>x</strong>y</p>")]
    [InlineData("<b>1<div>2<sup>3</b>4</sup></div>",
        "<p><strong>1</strong></p><p><strong>2</strong><strong><sup>3</sup></strong>4</p>")]
    [InlineData("<b><p>a<span style=\"font-style:italic\">b</b>c</span></p>",
        "<p><strong>a</strong><strong><em>b</em></strong>c</p>")]
    [InlineData("<b><div><svg></b>x", "<p>x</p>")]
    [InlineData("</p>x", "<p>x</p>")]
    [InlineData("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\""
        + " \"http://www.w3.org/TR/REC-html40/loose.dtd\">"
        + "<p>c<table><tr><td>t</td></tr></table>d</p>", "<p>c</p><p>t</p><p>d</p>")]
    [InlineData("<xmp>a\nb</xmp>", "<p>a<br>b</p>")]
    [InlineData("<math><mi>x</mi><p>after</p>", "<p>after</p>")]
    [InlineData("<td><b>a</td>b", "<p><strong>ab</strong></p>")]
    [InlineData("<table><tr><td><b>a</td><td>b</td></tr></table>", "<p><strong>a</strong></p><p>b</p>")]
    [InlineData("<table><tr><td><b>a</td></tr></table>c", "<p><strong>a</strong></p><p>c</p>")]
    public void Parse_ShouldFollowTheBrowsersParsingRules_WhenGivenLooseHtml(string html, string expected) =>
        Assert.Equal(expected, RichTextDocument.Parse(html).ToHtml());

    [Fact]
    public void Parse_ShouldKeepOnlyTheFormatsGiven_WhenGivenFormats()
    {
        var document = RichTextDocument.Parse("<b><i>a</i></b><u>b</u>",
            new RichTextSchema { Formats = RichTextFormats.Bold });

        Assert.Equal("<p><strong>a</strong>b</p>", document.ToHtml());
    }

    [Theory]
    [InlineData("  a  b  ", "<p>&nbsp;&nbsp;a &nbsp;b &nbsp;</p>")]
    [InlineData("a\u00A0b", "<p>a b</p>")]
    [InlineData("a\tb\fc\rd", "<p>a b c d</p>")]
    [InlineData("a\r\nb", "<p>a<br>b</p>")]
    [InlineData("a\n", "<p>a<br><br></p>")]
    [InlineData("a \nb", "<p>a&nbsp;<br>b</p>")]
    [InlineData("<&>", "<p>&lt;&amp;&gt;</p>")]
    public void ToHtml_ShouldWriteSpacesBreaksAndSymbolsSoTheyReadBack_WhenGivenAParagraph(string text, string expected)
    {
        var paragraph = new RichTextParagraph([new RichTextRun(text)]);
        var html = new RichTextDocument([paragraph]).ToHtml();

        Assert.Equal(expected, html);
        Assert.Equal(paragraph.Text, Assert.Single(RichTextDocument.Parse(html).Paragraphs).Text);
    }

    [Theory]
    [InlineData("<pre>a\tb</pre>", "a b")]
    [InlineData("<p>a&#13;b&#9;c</p>", "a b c")]
    [InlineData("don&#146;t", "don\u2019t")]
    [InlineData("&#x92;", "\u2019")]
    [InlineData("&#0;&#xD800;&#1114112;", "\uFFFD\uFFFD\uFFFD")]
    [InlineData("&#128512;", "\U0001F600")]
    [InlineData("&copy 2026", "\u00A9 2026")]
    [InlineData("&AMP; &COPY 2026 &LT;b&GT;", "& \u00A9 2026 <b>")]
    [InlineData("<pre>  a   b</pre>", "  a   b")]
    [InlineData("&ampx", "&x")]
    [InlineData("&hellip;", "\u2026")]
    [InlineData("&unknown;", "&unknown;")]
    public void Parse_ShouldDecodeTextAsBrowsersDo_WhenGivenWhiteSpaceAndCharacterReferences(string html, string text) =>
        Assert.Equal(text, RichTextDocument.Parse(html).ToPlainText());

    // A browser decodes names HTML 4 lacks, such as &ltrif;, but this reader leaves them as written.
    [Theory]
    [InlineData("&ltrif;")]
    [InlineData("&notit;")]
    public void Parse_ShouldKeepAReferenceAsText_WhenItsNameIsUnknown(string html) =>
        Assert.Equal(html, RichTextDocument.Parse(html).ToPlainText());

    // The optional opening leaves elements open below the repeated units, for their tag rules to search.
    [Theory]
    [InlineData("", 0, "<div>")]
    [InlineData("", 0, "</div>")]
    [InlineData("", 0, "<b style=\"x\">")]
    [InlineData("", 0, "<p><b>x</p>")]
    [InlineData("", 0, "<svg>")]
    [InlineData("", 0, "a<!---->")]
    [InlineData("", 0, "<span></div>")]
    [InlineData("", 0, "<span></x>")]
    [InlineData("<x><div>", 1, "<span></x>")]
    [InlineData("<div>", 500, "<svg><b>")]
    [InlineData("<div>", 500, "<td>")]
    public void Parse_ShouldFinishQuickly_WhenGivenAMegabyteOfDeepOrRepetitiveHtml(string opening, int times,
        string unit)
    {
        var html = string.Concat(Enumerable.Repeat(opening, times)) + string.Concat(Enumerable.Repeat(unit,
            1_000_000 / unit.Length));
        var watch = Stopwatch.StartNew();

        RichTextDocument.Parse(html);

        Assert.InRange(watch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Parse_ShouldFinishQuickly_WhenFormattingIsLeftOpenWithADifferentStyleEachTime()
    {
        var html = string.Concat(Enumerable.Range(0, 60_000).Select(i => $"<p><b style=\"a{i}\">x</p>"));
        var watch = Stopwatch.StartNew();

        RichTextDocument.Parse(html);

        Assert.InRange(watch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Parse_ShouldFinishQuickly_WhenManyNamesHaveOpenedBeforeManyStrayHeadingEndTags()
    {
        var html = string.Concat(Enumerable.Range(0, 30_000).Select(i => $"<a{i}></a{i}>"))
            + string.Concat(Enumerable.Repeat("</h1>", 100_000));
        var watch = Stopwatch.StartNew();

        RichTextDocument.Parse(html);

        Assert.InRange(watch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Parse_ShouldFinishQuickly_WhenManyParagraphsSitInsideDeepStyledBlocks()
    {
        // Each piece starts a Word list style and then fails, so a regex can't skip past it.
        var style = string.Concat(Enumerable.Repeat("mso-list:l0 x; ", 13));
        var html = string.Concat(Enumerable.Repeat($"<div style=\"{style}\">", 500))
            + string.Concat(Enumerable.Repeat("<p>x</p>", 120_000));
        var watch = Stopwatch.StartNew();

        RichTextDocument.Parse(html);

        Assert.InRange(watch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Parse_ShouldFinishQuickly_WhenALongLinkHoldsManyLines()
    {
        // With no "/" before its ":", checking the scheme reads the whole URL.
        var html = $"<a href=\"{new string('a', 500_000)}:x\">"
            + string.Concat(Enumerable.Repeat("x<br>", 100_000));
        var watch = Stopwatch.StartNew();

        RichTextDocument.Parse(html);

        Assert.InRange(watch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Parse_ShouldFinishQuickly_WhenSkippedContentWithALongStyleHoldsManyPieces()
    {
        // A style that keeps starting a Word marker's style and failing, so a regex reads all of it.
        var style = string.Concat(Enumerable.Repeat("mso-list:x; ", 40_000));
        var html = $"<svg style=\"{style}\">" + string.Concat(Enumerable.Repeat("x<g>", 100_000));
        var watch = Stopwatch.StartNew();

        RichTextDocument.Parse(html);

        Assert.InRange(watch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", "", "<ol><li><br></li></ol><ul><li><br></li></ul>")]
    [InlineData("a", "b", "<ol><li>a</li></ol><ul><li>b</li></ul>")]
    public void Parse_ShouldNumberOnlyTheWordItemWithANumber_WhenAnItemWithoutAMarkerFollowsIt(string first,
        string second, string expected) =>
        Assert.Equal(expected, RichTextDocument.Parse(
            $"<p style='mso-list:l0 level1 lfo1'><span style='mso-list:Ignore'>1.</span>{first}</p>"
            + $"<p style='mso-list:l0 level1 lfo1'>{second}</p>").ToHtml());

    [Fact]
    public void Parse_ShouldDropSkippedContent_WhenItIsNestedPastTheDepthLimit()
    {
        var html = string.Concat(Enumerable.Repeat("<div>", 600)) + "<script>alert(1)</script><style>p {}</style>"
            + "<template><div>x</div>hidden</template>ok";

        Assert.Equal("ok", RichTextDocument.Parse(html).ToPlainText());
    }

    [Fact]
    public void ToHtml_ShouldNestEachRunsTagsInOneOrder_WhenARunHasSeveralFormats()
    {
        var run = new RichTextRun("a", RichTextFormats.Superscript | RichTextFormats.Underline | RichTextFormats.Bold);

        Assert.Equal("<p><strong><u><sup>a</sup></u></strong></p>",
            new RichTextDocument([new RichTextParagraph([run])]).ToHtml());
    }

    [Fact]
    public void ToHtml_ShouldWriteAnEmptyParagraphAsALineBreak_WhenTheDocumentHasText() =>
        Assert.Equal("<p>a</p><p><br></p>",
            new RichTextDocument([new RichTextParagraph([new RichTextRun("a")]), new RichTextParagraph([])]).ToHtml());

    [Fact]
    public void ToPlainText_ShouldPutALineBreakBetweenParagraphs_WhenCalled() =>
        Assert.Equal("a\nb\nc", RichTextDocument.Parse("<p>a<br><strong>b</strong></p><p>c</p>").ToPlainText());

    [Fact]
    public void RichTextDocument_ShouldHaveOneEmptyParagraph_WhenCreatedWithNone()
    {
        var document = new RichTextDocument([]);

        Assert.True(document.IsEmpty);
        Assert.Empty(Assert.Single(document.Paragraphs).Runs);
    }

    [Fact]
    public void RichTextParagraph_ShouldJoinRunsAndDropEmptyOnes_WhenCreated()
    {
        var paragraph = new RichTextParagraph([new("a"), new(""), new("b"), new("c", RichTextFormats.Bold)]);

        Assert.Equal([new("ab"), new("c", RichTextFormats.Bold)], paragraph.Runs);
        Assert.Equal("abc", paragraph.Text);
    }

    [Fact]
    public void RichTextRun_ShouldRejectTheFormats_WhenTheyAreBothSubscriptAndSuperscript() =>
        Assert.Throws<ArgumentException>(() =>
            new RichTextRun("a", RichTextFormats.Subscript | RichTextFormats.Superscript));

    [Theory]
    [InlineData("<h1>a</h1><h2>b</h2><h3>c</h3><h4>d</h4><h6>e</h6>",
        "<h1>a</h1><h2>b</h2><h3>c</h3><h3>d</h3><h3>e</h3>")]
    [InlineData("<blockquote><p>a</p><p>b</p></blockquote><p>c</p>",
        "<blockquote><p>a</p><p>b</p></blockquote><p>c</p>")]
    [InlineData("<blockquote>a<ul><li>b</li></ul></blockquote>",
        "<blockquote><p>a</p></blockquote><ul><li>b</li></ul>")]
    [InlineData("<ol><li>a<ul><li>b</li><li>c</li></ul></li><li>d</li></ol>",
        "<ol><li>a<ul><li>b</li><li>c</li></ul></li><li>d</li></ol>")]
    [InlineData("<ul><li>a</li><ul><li>b</li></ul><li>c</li></ul>", "<ul><li>a<ul><li>b</li></ul></li><li>c</li></ul>")]
    [InlineData("<ul><li>a</li></ul><ol><li>b</li></ol>", "<ul><li>a</li></ul><ol><li>b</li></ol>")]
    [InlineData("<ul><li><p>a</p><p>b</p></li></ul>", "<ul><li>a</li><li>b</li></ul>")]
    [InlineData("<li>a</li>", "<ul><li>a</li></ul>")]
    [InlineData("<ul><li></li></ul>", "<ul><li><br></li></ul>")]
    [InlineData("a<hr>b", "<p>a</p><hr><p>b</p>")]
    [InlineData("<code>a</code><kbd>b</kbd><samp>c</samp><tt>d</tt>", "<p><code>abcd</code></p>")]
    [InlineData("<span style=\"font-family: Consolas, monospace\">a</span>", "<p><code>a</code></p>")]
    public void Parse_ShouldKeepEachBlockAndCode_WhenTheSchemaAllowsThem(string html, string expected) =>
        Assert.Equal(expected, RichTextDocument.Parse(html).ToHtml());

    [Theory]
    [InlineData("<a href=\"https://example.com/a?b=1&amp;c=2\">x</a>",
        "<p><a href=\"https://example.com/a?b=1&amp;c=2\">x</a></p>")]
    [InlineData("<a href=\"mailto:a@example.com\">x</a> <a href=\"TEL:123\">y</a>",
        "<p><a href=\"mailto:a@example.com\">x</a> <a href=\"TEL:123\">y</a></p>")]
    [InlineData("<a href=\"/trails?id=2#map\">x</a>", "<p><a href=\"/trails?id=2#map\">x</a></p>")]
    [InlineData("<a href=\"  https://example.com \">x</a>", "<p><a href=\"https://example.com\">x</a></p>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "<p>x</p>")]
    [InlineData("<a href=\"JaVaScRiPt:alert(1)\">x</a>", "<p>x</p>")]
    [InlineData("<a href=\"java&#9;script:alert(1)\">x</a>", "<p>x</p>")]
    [InlineData("<a href=\" data:text/html,x\">x</a>", "<p>x</p>")]
    [InlineData("<a>x</a><a href=\"\">y</a>", "<p>xy</p>")]
    [InlineData("<a href=\"/a\"><b>x</b>y</a>", "<p><a href=\"/a\"><strong>x</strong>y</a></p>")]
    [InlineData("<a href=\"/x\">one <a href=\"/y\">two</a> three</a>",
        "<p><a href=\"/x\">one </a><a href=\"/y\">two</a> three</p>")]
    [InlineData("<a href=\"/?a=1&section=2&copy=3&reg\">x</a>",
        "<p><a href=\"/?a=1&amp;section=2&amp;copy=3\u00AE\">x</a></p>")]
    [InlineData("<a href=\"/a&#13;b\">x</a>", "<p><a href=\"/ab\">x</a></p>")]
    [InlineData("<a href=\"/a\" style=\"text-decoration: underline\">x</a><u><a href=\"/a\">y</a></u>",
        "<p><a href=\"/a\">x<u>y</u></a></p>")]
    [InlineData("<a href=\"/a\"\nhref=\"/b\">x</a>", "<p><a href=\"/a\">x</a></p>")]
    [InlineData("<a href='/a\"b'>x</a>", "<p><a href=\"/a&quot;b\">x</a></p>")]
    public void Parse_ShouldKeepOnlySafeLinks_WhenGivenAnchors(string html, string expected) =>
        Assert.Equal(expected, RichTextDocument.Parse(html).ToHtml());

    [Fact]
    public void Parse_ShouldKeepOnlyTheSchemasBlocksAndSchemes_WhenGivenASchema()
    {
        var schema = new RichTextSchema
        {
            Formats = RichTextFormats.Link,
            Blocks = RichTextBlocks.BulletedLists,
            LinkSchemes = ["https"],
        };

        var document = RichTextDocument.Parse("<h1>a</h1><ul><li><a href=\"https://x\">b</a></li></ul>"
            + "<ol><li><a href=\"mailto:x\">c</a></li></ol><blockquote><code>d</code></blockquote><hr>", schema);

        Assert.Equal("<p>a</p><ul><li><a href=\"https://x\">b</a></li></ul><p>c</p><p>d</p>", document.ToHtml());
    }

    [Fact]
    public void Parse_ShouldGiveBackTheSameDocument_WhenReadingItsOwnBlocksAndLinks()
    {
        foreach (var document in Structures)
        {
            var again = RichTextDocument.Parse(document.ToHtml());

            Assert.Equal(document.ToHtml(), again.ToHtml());
            Assert.Equal(document.Paragraphs.Select(paragraph => (paragraph.Kind, paragraph.Level, paragraph.Text)),
                again.Paragraphs.Select(paragraph => (paragraph.Kind, paragraph.Level, paragraph.Text)));
        }
    }

    private static readonly RichTextDocument[] Structures =
    [
        new([
            new([new("Title")], RichTextParagraphKind.Heading1),
            new([new("quote ", RichTextFormats.Italic), new("link", link: "https://a.example/?q=1&r=\"2\"")],
                RichTextParagraphKind.Quote),
            new([], RichTextParagraphKind.Quote),
            new([new("a")], RichTextParagraphKind.BulletedItem),
            new([new("b", RichTextFormats.Code)], RichTextParagraphKind.BulletedItem, 1),
            new([new("c")], RichTextParagraphKind.NumberedItem, 2),
            new([new("d")], RichTextParagraphKind.BulletedItem, 1),
            new([new("e")], RichTextParagraphKind.NumberedItem),
            new([], RichTextParagraphKind.Rule),
            new([new("end\n")]),
        ]),
        new([new([], RichTextParagraphKind.Rule)]),
        new([new([new("x")], RichTextParagraphKind.Heading3), new([], RichTextParagraphKind.Heading2)]),
    ];

    [Fact]
    public void RichTextDocument_ShouldMoveAListItemOut_WhenItIsNestedMoreThanOneLevelDeeper()
    {
        var document = new RichTextDocument([
            new([new("a")], RichTextParagraphKind.BulletedItem, 2),
            new([new("b")], RichTextParagraphKind.BulletedItem, 3),
            new([new("c")]),
            new([new("d")], RichTextParagraphKind.NumberedItem, 1),
        ]);

        Assert.Equal([0, 1, 0, 0], document.Paragraphs.Select(paragraph => paragraph.Level));
        Assert.Equal("<ul><li>a<ul><li>b</li></ul></li></ul><p>c</p><ol><li>d</li></ol>", document.ToHtml());
    }

    [Fact]
    public void IsEmpty_ShouldBeFalse_WhenTheOnlyParagraphIsAnEmptyHeadingOrRule()
    {
        Assert.False(new RichTextDocument([new([], RichTextParagraphKind.Heading1)]).IsEmpty);
        Assert.False(new RichTextDocument([new([], RichTextParagraphKind.Rule)]).IsEmpty);
    }

    [Fact]
    public void RichTextParagraph_ShouldKeepRunsWithDifferentLinksApart_WhenCreated()
    {
        var paragraph = new RichTextParagraph([new("a", link: "/a"), new("b", link: "/a"), new("c", link: "/c")]);

        Assert.Equal([new("ab", link: "/a"), new("c", link: "/c")], paragraph.Runs);
    }

    [Fact]
    public void RichTextParagraph_ShouldRejectTheArguments_WhenARuleHasTextOrAParagraphIsNested()
    {
        Assert.Throws<ArgumentException>(() => new RichTextParagraph([new("a")], RichTextParagraphKind.Rule));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RichTextParagraph([new("a")], level: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RichTextParagraph([new("a")], RichTextParagraphKind.BulletedItem, -1));
    }

    [Theory]
    [InlineData("<ul><li><h2>x</h2></li></ul>", RichTextBlocks.BulletedLists, "<ul><li>x</li></ul>")]
    [InlineData("<blockquote><h1>a</h1></blockquote>", RichTextBlocks.Quotes, "<blockquote><p>a</p></blockquote>")]
    public void Parse_ShouldUseTheAllowedBlockAround_WhenTheInnermostBlockIsNotAllowed(string html,
        RichTextBlocks blocks, string expected) =>
        Assert.Equal(expected, RichTextDocument.Parse(html, new RichTextSchema { Blocks = blocks }).ToHtml());

    [Fact]
    public void Parse_ShouldDropAScriptLink_WhenTheSchemaListsItsScheme() =>
        Assert.Equal("<p>x</p>", RichTextDocument.Parse("<a href=\"javascript:alert(1)\">x</a>",
            new RichTextSchema { LinkSchemes = ["javascript", "vbscript"] }).ToHtml());

    [Fact]
    public void RichTextSchema_ShouldEqualAnotherWithTheSameSchemes_WhenComparedByValue()
    {
        Assert.Equal(RichTextSchema.Default, new RichTextSchema());
        Assert.Equal(new RichTextSchema { LinkSchemes = ["https"] }, new RichTextSchema { LinkSchemes = ["HTTPS"] });
        Assert.NotEqual(new RichTextSchema { LinkSchemes = ["https"] }, new RichTextSchema { LinkSchemes = ["http"] });
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData(" VBScript:x")]
    public void RichTextRun_ShouldRejectTheLink_WhenItWouldRunScript(string link) =>
        Assert.Throws<ArgumentException>(() => new RichTextRun("x", link: link));

    [Theory]
    [InlineData(" /a\t\r\n", "/a")]
    [InlineData(" \n ", null)]
    [InlineData("", null)]
    public void RichTextRun_ShouldCleanTheLinkAsABrowserDoes_WhenCreated(string link, string? expected) =>
        Assert.Equal(expected, new RichTextRun("x", link: link).Link);

    [Fact]
    public void RichTextRun_ShouldHaveTheLinkFormatExactlyWhenItHasALink_WhenCreated()
    {
        Assert.Equal(RichTextFormats.Bold | RichTextFormats.Link,
            new RichTextRun("a", RichTextFormats.Bold, "/a").Formats);
        Assert.NotEqual(new RichTextRun("a", link: "/a"), new RichTextRun("a", link: "/b"));
        Assert.Throws<ArgumentException>(() => new RichTextRun("a", RichTextFormats.Link));
    }
}
