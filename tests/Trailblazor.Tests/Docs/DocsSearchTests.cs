using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class DocsSearchTests
{
    [Fact]
    public void Find_ShouldFindTheDataGridsTestingAdvice_WhenGivenAHelperItNames()
    {
        var results = DocsSearch.Find("ClickRowAsync");

        Assert.Contains(results, result => result.Page == "Data grid");
    }

    [Fact]
    public void Find_ShouldNotMatchAMethodsParameters_WhenGivenAParameterName()
    {
        var results = DocsSearch.Find("preventScroll");

        Assert.DoesNotContain(results, result => result.Page == "Button");
    }

    [Fact]
    public void Find_ShouldRankTitleMatchesFirst_WhenOthersMatchElsewhere()
    {
        var results = DocsSearch.Find("group");

        Assert.Equal(["Button group", "Toggle group", "Checkbox group", "Radio group"],
            results.Take(4).Select(result => result.Page));
        Assert.All(results.Take(4), result => Assert.Equal(SearchMatch.Title, result.Match));
        Assert.Contains(results.Skip(4), result => result.Page == "Surface");
        Assert.Equal("components/button-group", results[0].Path);
    }

    [Fact]
    public void Find_ShouldLinkTheMembersRow_WhenGivenItsName()
    {
        var results = DocsSearch.Find("placeholder");

        Assert.Contains(results, result => result is { Page: "Select", PagePath: "components/select",
            Section: "TbSelect<TValue>.Placeholder", Path: "components/select/api#tbselect-placeholder",
            Match: SearchMatch.Member });
        Assert.Contains(results, result => result is { Page: "Text field", Match: SearchMatch.Member });
        Assert.DoesNotContain(results, result => result.Page == "Button");
    }

    [Fact]
    public void Find_ShouldLinkTheMembersRow_WhenGivenThePageAndMemberNames()
    {
        var results = DocsSearch.Find("stack gap");

        Assert.Contains(results, result => result is
            { Page: "Stack", Section: "Gap", Path: "components/stack/api#tbstack-gap", Match: SearchMatch.Member });
        // The page's title alone doesn't bring in every member.
        Assert.DoesNotContain(DocsSearch.Find("stack"), result => result.Path == "components/stack/api#tbstack-gap");
    }

    [Fact]
    public void Find_ShouldRankHeadingsThenMembersThenText_WhenAWordMatchesEach()
    {
        var matches = DocsSearch.Find("dialog").Select(result => result.Match).ToList();

        Assert.Equal([SearchMatch.Title, SearchMatch.Heading, SearchMatch.Member, SearchMatch.Text],
            matches.Distinct());
    }

    [Fact]
    public void Find_ShouldNameAMembersType_WhenAnotherTypeOnThePageHasTheSameName()
    {
        var results = DocsSearch.Find("childcontent").Where(result => result.Page == "Dialog").ToList();

        Assert.Contains(results, result => result.Section == "TbDialog.ChildContent");
        Assert.Contains(results, result => result.Section == "TbDialogTitle.ChildContent");
        // The type prefix in a member's label isn't searched, so searching a type's name doesn't return all its
        // members.
        Assert.DoesNotContain(DocsSearch.Find("select"), result =>
            result is { Page: "Select", Match: SearchMatch.Member }
            && !result.Section!.Split('.')[^1].Contains("select", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Find_ShouldNeedEveryWord_WhenGivenSeveral()
    {
        Assert.True(DocsSearch.Find("getting started")[0] is
            { Page: "Getting started", Section: null, Path: "guides/getting-started", Match: SearchMatch.Title });
        Assert.Empty(DocsSearch.Find("dialog xylophone"));
    }

    [Theory]
    [InlineData("overlays", new[] { "Tooltip", "Menu", "Command palette", "Dialog", "Drawer", "Focus scope", "Popover",
        "Popconfirm", "Overlay" })]
    [InlineData("guides",
        new[] { "Getting started", "Forms and validation", "Accessibility", "Theming", "Palettes", "Spacing",
            "Browser", "Announcements", "Rich text", "Icons", "Localization", "Right-to-left and culture", "Testing",
            "Wrapping components", "Analyzers",
            "Telemetry" })]
    public void Find_ShouldRankEverythingInACategoryAsATitleMatch_WhenGivenItsName(string category, string[] titles)
    {
        var results = DocsSearch.Find(category);

        Assert.Equal(titles, results.Where(result => result.Match == SearchMatch.Title).Select(result => result.Page));
    }

    [Fact]
    public void Find_ShouldFindAComponentsAccessibility_WhenTheWordsAreInItsRightToLeftKeys()
    {
        var results = DocsSearch.Find("key points");

        Assert.Contains(results, result => result is { Page: "Slider", Section: "Accessibility" });
    }

    [Fact]
    public void Find_ShouldFindAComponent_WhenTheWordIsOnlyInItsNotes()
    {
        var results = DocsSearch.Find("field-sizing");

        Assert.Contains(results, result => result is { Page: "Text field", Section: "Notes and limitations",
            Path: "components/text-field#notes", Match: SearchMatch.Text });
    }

    [Fact]
    public void Find_ShouldLinkTheSection_WhenASectionTitleMatches()
    {
        var results = DocsSearch.Find("many rows");

        Assert.Contains(results, result => result is { Page: "Tree view", Section: "Many rows",
            Path: "components/tree-view#many-rows", Match: SearchMatch.Heading });
    }

    [Fact]
    public void Find_ShouldFindAGuideSection_WhenItsHeadingMatches()
    {
        var results = DocsSearch.Find("set up bunit");

        Assert.Equal(new SearchMatch[] { SearchMatch.Heading }, results.Where(result => result.Page == "Testing")
            .Where(result => result.Path == "guides/testing#bunit").Select(result => result.Match));
    }

    [Fact]
    public void Find_ShouldFindEveryPageAboutDialogs_WhenGivenDialog()
    {
        var pages = DocsSearch.Find("dialog").Select(result => result.Page).ToHashSet();

        Assert.Superset(
            new HashSet<string> { "Dialog", "Popover", "Overlay", "Focus scope", "Testing", "Getting started" }, pages);
    }

    [Fact]
    public void Find_ShouldShowTheWordsAroundTheMatch_WhenTheTextIsLong()
    {
        var snippet = DocsSearch.Find("F8")
            .Single(result => result.Page == "Toast" && result.Path.EndsWith("#action")).Snippet;

        Assert.Contains("F8", snippet);
        Assert.StartsWith("…", snippet);
        Assert.InRange(snippet.Length, 60, 142);
        // The catalog writes code in backticks; the snippet keeps the code and drops the backticks.
        Assert.Contains(" UnmatchedText ", Assert.Single(DocsSearch.Find("UnmatchedText"),
            result => result.Section == "Options that load late").Snippet);
    }

    [Fact]
    public void Find_ShouldLeaveOutTheSectionsAPageDoesntShow_WhenTheirHeadingMatches()
    {
        var notes = DocsSearch.Find("limitations").Where(result => result.Section == "Notes and limitations").ToList();

        Assert.NotEmpty(notes);
        Assert.All(notes, result => Assert.NotEmpty(result.Snippet));
    }

    [Fact]
    public void Find_ShouldLinkAPalettesSection_WhenItsNameIsSearched()
    {
        var results = DocsSearch.Find("graphite");

        Assert.Contains(results, result => result is { Page: "Palettes", Section: "Graphite",
            Path: "guides/palettes#palette-graphite", Match: SearchMatch.Heading });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Find_ShouldReturnNothing_WhenGivenNoWords(string? query)
    {
        Assert.Empty(DocsSearch.Find(query));
    }
}
