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
    [InlineData("<ul><li>a<li>b</ul>", "<p>a</p><p>b</p>")]
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
    [InlineData("<h1>Title</h2>Body", "<p>Title</p><p>Body</p>")]
    [InlineData("<p><template></p>hidden</template>", "")]
    [InlineData("<b><svg></b>after", "<p>after</p>")]
    [InlineData("<div><svg></div>after", "<p><br></p><p>after</p>")]
    [InlineData("<div><table><tr><td><svg></div>hidden</td></tr></table>", "")]
    [InlineData("<h1><svg></h2>x", "<p><br></p><p>x</p>")]
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
        var document = RichTextDocument.Parse("<b><i>a</i></b><u>b</u>", RichTextFormats.Bold);

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
}
