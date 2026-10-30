namespace Trailblazor.Tests.Components;

public class PageRangeTests
{
    [Theory]
    [InlineData(1, 10, 1, 1, "1 2 3 4 5 … 10")]
    [InlineData(5, 10, 1, 1, "1 … 4 5 6 … 10")]
    [InlineData(10, 10, 1, 1, "1 … 6 7 8 9 10")]
    [InlineData(4, 10, 1, 1, "1 2 3 4 5 … 10")]
    [InlineData(6, 12, 2, 1, "1 … 4 5 6 7 8 … 12")]
    [InlineData(5, 20, 1, 2, "1 2 3 4 5 6 … 19 20")]
    [InlineData(2, 3, 1, 1, "1 2 3")]
    [InlineData(1, 1, 1, 1, "1")]
    [InlineData(4, 7, 1, 1, "1 2 3 4 5 6 7")]
    public void For_ShouldShowTheBoundariesTheSiblingsAndGaps_WhenGivenAPage(int page, int count, int siblings,
        int boundaries, string expected)
    {
        var pages = PageRange.For(page, count, siblings, boundaries);

        Assert.Equal(expected, string.Join(" ", pages.Select(number => number?.ToString() ?? "…")));
    }
}
