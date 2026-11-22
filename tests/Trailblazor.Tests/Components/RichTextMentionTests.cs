namespace Trailblazor.Tests.Components;

public class RichTextMentionTests
{
    private static readonly Mention Ada = new("@", new MentionItem("Ada Lovelace") { Id = "42" });
    private static readonly RichTextSchema WithMentions = RichTextSchema.Default with { Mentions = true };

    [Fact]
    public void ToHtml_ShouldWriteAMentionAsASpanInsideItsFormats_WhenARunIsAMention()
    {
        var document = new RichTextDocument([
            new([new("Ask "), new(Ada, RichTextFormats.Bold), new(" today")]),
        ]);

        Assert.Equal("<p>Ask <strong><span data-mention=\"42\" data-trigger=\"@\">@Ada Lovelace</span></strong> "
            + "today</p>", document.ToHtml());
    }

    [Fact]
    public void Parse_ShouldGiveBackTheSameRuns_WhenReadingItsOwnMentions()
    {
        var quoted = new Mention("#", new MentionItem("a <b> & c") { Id = "x\"y&z <1>" });
        var document = new RichTextDocument([
            new([new(Ada), new(Ada), new(" and "), new(quoted, RichTextFormats.Italic | RichTextFormats.Code)]),
            new([new("In a list "), new(new Mention("", new MentionItem("no trigger or id")))],
                RichTextParagraphKind.BulletedItem),
        ]);

        var again = RichTextDocument.Parse(document.ToHtml(), WithMentions);

        Assert.Equal(document.ToHtml(), again.ToHtml());
        Assert.Equal(document.Paragraphs.SelectMany(paragraph => paragraph.Runs),
            again.Paragraphs.SelectMany(paragraph => paragraph.Runs));
        Assert.Equal([Ada, Ada, quoted, new("", new MentionItem("no trigger or id"))], again.Mentions);
    }

    [Fact]
    public void Parse_ShouldReadAMentionAsItsText_WhenTheSchemaDoesNotKeepMentions()
    {
        const string Html = "<p>Ask <span data-mention=\"42\" data-trigger=\"@\">@Ada Lovelace</span> today</p>";

        var document = RichTextDocument.Parse(Html);

        Assert.Empty(document.Mentions);
        Assert.Equal("<p>Ask @Ada Lovelace today</p>", document.ToHtml());
        Assert.NotEqual(RichTextSchema.Default, WithMentions);
    }

    [Fact]
    public void Parse_ShouldReadOneMention_WhenItsSpanHoldsTagsExtraSpaceAndALineBreak()
    {
        var document = RichTextDocument.Parse("<p>Hi <span data-mention=\"7\" data-trigger=\"@\">@<b>Ada</b>  \n "
            + "Lovelace<br>Jr</span>!</p>", WithMentions);

        // A mention is one line, so a line break inside it reads as a space.
        Assert.Equal(["Hi ", "@Ada Lovelace Jr", "!"], document.Paragraphs[0].Runs.Select(run => run.Text));
        Assert.Equal(new Mention("@", new MentionItem("Ada Lovelace Jr") { Id = "7" }),
            document.Paragraphs[0].Runs[1].Mention);
        Assert.Equal(RichTextFormats.None, document.Paragraphs[0].Runs[1].Formats);
    }

    [Fact]
    public void Parse_ShouldTakeTheWholeTextAsTheItems_WhenItDoesNotStartWithTheTrigger()
    {
        var document = RichTextDocument.Parse("<p><span data-mention data-trigger=\"#\">Ada</span></p>", WithMentions);

        Assert.Equal(new Mention("", new MentionItem("Ada")), Assert.Single(document.Mentions));
    }

    [Fact]
    public void Parse_ShouldLeaveTheLinkOffAMention_WhenItsSpanIsInsideALink()
    {
        var document = RichTextDocument.Parse("<p><a href=\"https://a.example\">"
            + "<span data-mention=\"1\" data-trigger=\"@\">@Ada</span> more</a></p>", WithMentions);

        var runs = document.Paragraphs[0].Runs;
        Assert.Equal((null, RichTextFormats.None), (runs[0].Link, runs[0].Formats));
        Assert.Equal((" more", "https://a.example"), (runs[1].Text, runs[1].Link));
    }

    [Fact]
    public void Parse_ShouldReadPlainText_WhenTheAttributeIsNotOnASpan()
    {
        var document = RichTextDocument.Parse("<p><b data-mention=\"1\">@Ada</b> <i><span>@Bo</span></i></p>",
            WithMentions);

        Assert.Empty(document.Mentions);
        Assert.Equal("@Ada @Bo", document.ToPlainText());
    }

    [Fact]
    public void Parse_ShouldReadOneMention_WhenAMentionSpanIsInsideAnother()
    {
        var document = RichTextDocument.Parse("<p><span data-mention=\"1\" data-trigger=\"@\">@Ada "
            + "<span data-mention=\"2\" data-trigger=\"@\">@Bo</span></span> after</p>", WithMentions);

        Assert.Equal(new Mention("@", new MentionItem("Ada @Bo") { Id = "1" }), Assert.Single(document.Mentions));
        Assert.Null(document.Paragraphs[0].Runs[^1].Mention);
    }

    [Fact]
    public void Run_ShouldCleanTheMention_WhenItHasLineBreaksADescriptionOrAnEmptyId()
    {
        var run = new RichTextRun(
            new Mention("@", new MentionItem("Ada\r\nLovelace\t") { Id = "", Description = "Math" }));

        Assert.Equal("@Ada  Lovelace ", run.Text);
        Assert.Equal(new Mention("@", new MentionItem("Ada  Lovelace ")), run.Mention);
    }

    [Fact]
    public void Run_ShouldThrow_WhenAMentionIsGivenTheLinkFormat()
    {
        Assert.Throws<ArgumentException>(() => new RichTextRun(Ada, RichTextFormats.Link));
    }

    [Fact]
    public void Run_ShouldThrow_WhenTheMentionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RichTextRun((Mention)null!));
    }

    [Fact]
    public void Paragraph_ShouldKeepAMentionARunOfItsOwn_WhenItsNeighborsHaveTheSameFormats()
    {
        var paragraph = new RichTextParagraph([new("a"), new("b"), new(Ada), new(Ada), new("c"), new("d")]);

        Assert.Equal(["ab", "@Ada Lovelace", "@Ada Lovelace", "cd"], paragraph.Runs.Select(run => run.Text));
        Assert.Equal("ab@Ada Lovelace@Ada Lovelacecd", paragraph.Text);
    }

    [Fact]
    public void Mentions_ShouldListEveryMentionInOrder_WhenTheyAreInParagraphsAndCells()
    {
        var bo = new Mention("@", new MentionItem("Bo") { Id = "2" });
        var document = RichTextDocument.Parse("<p><span data-mention=\"2\" data-trigger=\"@\">@Bo</span></p><table><tr>"
            + "<td>x <span data-mention=\"42\" data-trigger=\"@\">@Ada Lovelace</span></td></tr></table>",
            WithMentions with { Blocks = RichTextSchema.Default.Blocks | RichTextBlocks.Tables });

        Assert.Equal([bo, Ada], document.Mentions);
    }

    [Fact]
    public void ToMarkdown_ShouldWriteAMentionAsItsText_WhenARunIsAMention()
    {
        var document = new RichTextDocument([new([new("Ask "), new(Ada, RichTextFormats.Bold), new(".")])]);

        Assert.Equal("Ask **@Ada Lovelace**.", document.ToMarkdown().Trim());
        Assert.Equal("Ask @Ada Lovelace.", document.ToPlainText());
    }
}
