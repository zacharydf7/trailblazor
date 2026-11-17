using Bunit;

namespace Trailblazor.Tests.Components;

public class TbHighlightTests : BunitContext
{
    public TbHighlightTests() => this.AddTrailblazor();

    private IRenderedComponent<TbHighlight> RenderHighlight(string text, string? search, bool caseSensitive = false) =>
        Render<TbHighlight>(parameters => parameters.Add(highlight => highlight.Text, text)
            .Add(highlight => highlight.Search, search).Add(highlight => highlight.CaseSensitive, caseSensitive));

    private static List<string> Matches(IRenderedComponent<TbHighlight> cut) =>
        [.. cut.FindAll("[data-part=match]").Select(match => match.TextContent)];

    [Fact]
    public void Render_ShouldMarkEachWordWhereverItAppears_WhenSearched()
    {
        var cut = RenderHighlight("Pine Creek, past the old pine", "pine  creek");

        Assert.Equal(["Pine", "Creek", "pine"], Matches(cut));
        Assert.Equal("Pine Creek, past the old pine", cut.Find(".tb-highlight").TextContent);
        Assert.Equal("mark", cut.Find("[data-part=match]").LocalName);
    }

    [Fact]
    public void Render_ShouldJoinOverlappingMatches_WhenWordsOverlap()
    {
        var cut = RenderHighlight("Pinecrest", "pine necr");

        Assert.Equal(["Pinecr"], Matches(cut));
    }

    [Fact]
    public void Render_ShouldMarkTheWholeMatch_WhenItIsLongerThanTheSearch()
    {
        // An e followed by a separate accent matches é, which is one character shorter.
        var cut = RenderHighlight("Cafe\u0301 on Pine", "caf\u00e9");

        Assert.Equal(["Cafe\u0301"], Matches(cut));
    }

    [Fact]
    public void Render_ShouldMarkOnlyTheSameCapitals_WhenCaseSensitive()
    {
        var cut = RenderHighlight("Pine and pine", "pine", caseSensitive: true);

        Assert.Equal(["pine"], Matches(cut));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("cedar")]
    public void Render_ShouldShowThePlainText_WhenNothingMatches(string? search)
    {
        var cut = RenderHighlight("<b>Pine</b> Hollow", search);

        Assert.Empty(Matches(cut));
        Assert.Equal("<b>Pine</b> Hollow", cut.Find(".tb-highlight").TextContent);
    }
}
