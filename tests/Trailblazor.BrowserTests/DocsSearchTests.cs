using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class DocsSearchTests
{
    [Fact]
    public void Find_ShouldRankTitleMatchesFirst_WhenOthersMatchOnlyInTheirSummaries()
    {
        var results = DocsSearch.Find("group");

        Assert.Equal(["Button group", "Toggle group", "Radio group"], results.Take(3).Select(result => result.Title));
        Assert.Contains(results.Skip(3), result => result.Title == "Surface");
        Assert.Equal("components/button-group", results[0].Path);
    }

    [Fact]
    public void Find_ShouldListTheComponentsWithAParameter_WhenGivenItsName()
    {
        var results = DocsSearch.Find("placeholder");

        Assert.Contains(results, result => result is { Title: "Select", Parameters: ["Placeholder"] });
        Assert.Contains(results, result => result.Title == "Text field");
        Assert.DoesNotContain(results, result => result.Title == "Button");
    }

    [Fact]
    public void Find_ShouldNeedEveryWord_WhenGivenSeveral()
    {
        Assert.Equal(["Getting started"], DocsSearch.Find("getting started").Select(result => result.Title));
        Assert.Empty(DocsSearch.Find("dialog getting"));
    }

    [Theory]
    [InlineData("overlays", new[] { "Tooltip", "Menu", "Dialog", "Popover", "Overlay" })]
    [InlineData("guides",
        new[] { "Getting started", "Theming", "Palettes", "Browser", "Announcements", "Testing", "Roadmap" })]
    public void Find_ShouldFindEverythingInACategory_WhenGivenItsName(string category, string[] titles)
    {
        var results = DocsSearch.Find(category);

        Assert.Equal(titles, results.Select(result => result.Title));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Find_ShouldReturnNothing_WhenGivenNoWords(string? query)
    {
        Assert.Empty(DocsSearch.Find(query));
    }
}
