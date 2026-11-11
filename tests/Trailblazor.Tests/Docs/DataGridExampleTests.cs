using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

// The data grid page's examples, as the docs show them.
public class DataGridExampleTests : DocsTest
{
    [Fact]
    public void DataGridExample_ShouldPressOnlyThePlannedTrailsButton_WhenAnotherTrailIsPlanned()
    {
        var cut = Render<DataGridExample>();
        cut.Find("button[aria-label='Plan Pine Hollow loop']").Click();
        Assert.Equal("true", cut.Find("button[aria-label='Plan Pine Hollow loop']").GetAttribute("aria-pressed"));

        cut.Find("button[aria-label='Plan Cedar Flats']").Click();

        Assert.Contains("Planning Cedar Flats.", cut.Find("output").TextContent);
        Assert.Equal("false", cut.Find("button[aria-label='Plan Pine Hollow loop']").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void DataGridFilterExample_ShouldFilterAndCountTheRows_WhenFilterTextIsTyped()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<DataGridFilterExample>();

        Input(cut.Find("table[aria-label=Hikes]"), "Filter Name").Input("ridge");

        cut.WaitForAssertion(() => Assert.Equal(["Eagle Ridge traverse"], Names(cut, "Hikes")));
        Assert.Equal("Filters: Name Contains ridge.", cut.Find("output").TextContent.Trim());
        announcer.WaitForAssertion(() =>
            Assert.Equal("Rows shown: 1.", announcer.FindAll("[role=status] p")[^1].TextContent));
    }

    [Fact]
    public async Task DataGridFilterExample_ShouldShowRowsMeetingEveryColumnsFilter_WhenSeveralAreSet()
    {
        var cut = Render<DataGridFilterExample>();
        var hikes = cut.Tb().Grid("Hikes");

        await cut.Tb().Select("Filter Difficulty").ChooseAsync("Hard");
        await hikes.FilterAsync("Length (km)", "14.2");

        Assert.Equal(["Eagle Ridge traverse"], Names(cut, "Hikes"));
        await hikes.FilterAsync("Length (km)", "11.9");
        Assert.Equal(["Summit Gate"], Names(cut, "Hikes"));
        await hikes.FilterAsync("Length (km)", "");
        Assert.Equal(["Eagle Ridge traverse", "Summit Gate"], Names(cut, "Hikes"));
    }

    [Fact]
    public async Task DataGridStateExample_ShouldBringBackTheSortAndFilters_WhenASavedViewIsRestored()
    {
        var cut = Render<DataGridStateExample>();
        await cut.Tb().Select("Filter Surface").ChooseAsync("Dirt");
        Sort(cut, "Name");
        Sort(cut, "Name");
        Button(cut, "Save view").Click();
        Assert.Contains("\"Direction\":\"Descending\"", cut.Find("output").TextContent);

        await cut.Tb().Select("Filter Surface").ChooseAsync("Paved");
        Sort(cut, "Name");
        Sort(cut, "Name");
        Assert.Equal("ascending", cut.Find("th[data-title=Name]").GetAttribute("aria-sort"));
        Assert.Equal(["Cedar Flats", "Lake Shore"], Names(cut, "Saved view"));
        Button(cut, "Restore view").Click();

        Assert.Equal(["Ridge Loop", "Pine Hollow loop", "Fern Gully"], Names(cut, "Saved view"));
        Assert.Equal("Dirt", cut.Tb().Select("Filter Surface").Text);
    }

    [Fact]
    public void DataGridFilterMenuExample_ShouldShowOnlyCampsWithWater_WhenTheWaterSwitchIsTurnedOn()
    {
        var cut = Render<DataGridFilterMenuExample>();

        cut.Find("button[aria-label='Filter Water']").Click();
        cut.Find("[role=switch]").Change(true);

        Assert.Equal(["Aspen Flat", "Dune Point", "Elk Meadow"], Names(cut, "Campsites"));
    }

    [Fact]
    public async Task DataGridExample_ShouldShowItsFirstPageOfRows_WhenRenderedStatically()
    {
        await using var renderer = new HtmlRenderer(Services, NullLoggerFactory.Instance);

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<DataGridExample>()).ToHtmlString());

        Assert.Contains("Pine Hollow loop", html);
        Assert.DoesNotContain("Lake camp", html);
    }

    [Fact]
    public void EmptyExample_ShouldShowTheDefaultMessageAndItsOwnContent_WhenThereAreNoRows()
    {
        var cut = Render<DataGridEmptyExample>();

        var messages = cut.FindAll("[data-part=empty]");

        Assert.Equal("Nothing to show.", messages[0].TextContent.Trim());
        Assert.Contains("Trips you save show here.", messages[1].TextContent);
    }

    [Fact]
    public void EmptyExample_ShouldShowTheRow_WhenATripIsPlanned()
    {
        var cut = Render<DataGridEmptyExample>();

        Button(cut, "Plan a trip").Click();

        Assert.Equal(["Pine Hollow loop"], Names(cut, "Saved trips"));
    }

    [Fact]
    public void LoadingExample_ShouldShowTheMessageThenTheRows_WhenLoaded()
    {
        var cut = Render<DataGridLoadingExample>();

        Button(cut, "Load the camps").Click();

        Assert.NotNull(cut.Find("[data-part=loading]"));
        Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy"));
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-part=row]").Count), TimeSpan.FromSeconds(5));
        Assert.Empty(cut.FindAll("[data-part=loading]"));
    }

    [Fact]
    public void FailedLoadExample_ShouldOfferToTryAgainAndSayWhy_WhenALoadFails()
    {
        var cut = Render<DataGridFailedLoadExample>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-part=row]").Count), TimeSpan.FromSeconds(5));

        cut.Find("input[type=checkbox]").Change(true);
        Button(cut, "Load again").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")), TimeSpan.FromSeconds(5));
        Assert.Equal("Failed loads: 1", cut.Find("output").TextContent.Trim());
        Assert.False(cut.Find("input[type=checkbox]").HasAttribute("checked"));

        cut.Find("[data-part=failed] button").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-part=row]").Count), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RowClickExample_ShouldOpenTheMapWithoutSelecting_WhenARowIsClicked()
    {
        var cut = Render<DataGridRowClickExample>();

        await cut.Tb().Grid("Trail maps").ClickRowAsync("Cedar Flats");

        Assert.Equal("Map open: Cedar Flats. Chosen: 0.", cut.Find("output").TextContent.Trim());
    }

    [Fact]
    public void RowClickExample_ShouldOpenTheMap_WhenTheRowsButtonIsClicked()
    {
        var cut = Render<DataGridRowClickExample>();

        cut.Find("button[aria-label='Open the map of Summit Gate']").Click();

        Assert.Contains("Map open: Summit Gate.", cut.Find("output").TextContent);
    }

    [Fact]
    public void ColumnsExample_ShouldOrderGradesByRank_WhenSortedFromTheStart()
    {
        var cut = Render<DataGridColumnsExample>();

        Assert.Equal(["Cedar Flats", "Pine Hollow loop", "Eagle Ridge traverse"], Names(cut, "Trail grades"));
    }

    [Fact]
    public void RowClassExample_ShouldMarkOnlyTheClosedTrail_WhenRendered()
    {
        var cut = Render<DataGridRowClassExample>();

        var closed = cut.FindAll("tr.closed-trail");

        Assert.Contains("Cedar Flats", Assert.Single(closed).TextContent);
    }

    [Fact]
    public void HeightExample_ShouldGoToTheRowsPage_WhenAskedForTheLastMarker()
    {
        var cut = Render<DataGridHeightExample>();
        Assert.Empty(cut.FindAll("tr[data-key='60']"));

        Button(cut, "Go to the last marker").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("tr[data-key='60']")));
    }

    [Fact]
    public void LabelsExample_ShouldUseItsOwnWords_WhenRendered()
    {
        var cut = Render<DataGridLabelsExample>();

        Assert.NotNull(cut.Find("input[aria-label='Choose every hut on this page']"));
        Assert.Contains("Huts per page", cut.Markup);
    }

    [Fact]
    public void ComponentPage_ShouldGroupTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheDataGrid()
    {
        var cut = ComponentPage("data-grid");
        var accessibility = cut.Find("ul[aria-label=Accessibility]").TextContent;

        Assert.Contains("With cell navigation", cut.Markup);
        Assert.Contains("On a row's move handle", cut.Markup);
        Assert.Contains("ClickRowAsync", cut.Find("ul[aria-label='Testing tips']").TextContent);
        Assert.DoesNotContain("OnRowMove", accessibility);
        Assert.DoesNotContain("PageSizes", accessibility);
    }

    private static void Sort<T>(IRenderedComponent<T> cut, string column) where T : IComponent =>
        cut.Find($"th[data-title='{column}'] [data-part=sort]").Click();

    private static IReadOnlyList<string> Names<T>(IRenderedComponent<T> cut, string grid) where T : IComponent =>
        Texts(cut.FindAll($"table[aria-label='{grid}'] tbody td[data-column=Name]"));
}
