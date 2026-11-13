namespace Trailblazor.Tests;

public class FuzzyMatchTests
{
    [Fact]
    public void Find_ShouldMatchTheLettersWhereTheyAre_WhenTheyAreSpreadOut()
    {
        var match = FuzzyMatch.Find("New tab", "nwtb");

        Assert.Equal([0, 2, 4, 6], match!.Value.Places);
    }

    [Theory]
    [InlineData("New tab", "tn")]
    [InlineData("New tab", "newtabs")]
    [InlineData("New tab", "x")]
    public void Find_ShouldReturnNull_WhenGivenLettersNotInTheTextInOrder(string text, string query) =>
        Assert.Null(FuzzyMatch.Find(text, query));

    [Fact]
    public void Find_ShouldIgnoreCaseAccentsAndSpaces_WhenGivenAQuery()
    {
        var match = FuzzyMatch.Find("Café Menu", "CAFE m");

        Assert.Equal([0, 1, 2, 3, 5], match!.Value.Places);
    }

    [Fact]
    public void Find_ShouldPreferARunAtAWordStart_WhenTheLettersAppearMoreThanOnce()
    {
        var match = FuzzyMatch.Find("Toggle settings", "set");

        Assert.Equal([7, 8, 9], match!.Value.Places);
    }

    [Fact]
    public void Find_ShouldScoreWordStartsAndRunsHigher_WhenGivenTheSameQuery()
    {
        var starts = FuzzyMatch.Find("Git status", "gs")!.Value.Score;
        var inside = FuzzyMatch.Find("Logs", "gs")!.Value.Score;
        var run = FuzzyMatch.Find("Settings", "set")!.Value.Score;
        var scattered = FuzzyMatch.Find("Show extra tabs", "set")!.Value.Score;

        Assert.True(starts > inside, $"{starts} should beat {inside}.");
        Assert.True(run > scattered, $"{run} should beat {scattered}.");
    }

    [Fact]
    public void Find_ShouldCountACapitalAfterASmallLetterAsAWordStart_WhenGivenCamelCase()
    {
        var camel = FuzzyMatch.Find("dataGrid", "g")!.Value.Score;
        var plain = FuzzyMatch.Find("datagrid", "g")!.Value.Score;

        Assert.True(camel > plain, $"{camel} should beat {plain}.");
    }

    [Fact]
    public void Find_ShouldReturnNull_WhenGivenAQueryOfSpaces() => Assert.Null(FuzzyMatch.Find("New tab", "  "));
}
