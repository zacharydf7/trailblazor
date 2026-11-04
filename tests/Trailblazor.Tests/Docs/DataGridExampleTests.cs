using Bunit;
using Microsoft.AspNetCore.Components;
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

        Assert.Contains("Planning Cedar Flats.", cut.Find("[data-testid=chosen]").TextContent);
        Assert.Equal("false", cut.Find("button[aria-label='Plan Pine Hollow loop']").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void DataGridFilterExample_ShouldFilterAndCountTheRowsOnceTypingPauses_WhenFilterTextIsTyped()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<DataGridFilterExample>();

        Input(cut.Find("table[aria-label=Hikes]"), "Filter Name").Input("ridge");
        Assert.Equal(6, Names(cut, "Hikes").Count);
        Time.Advance(TimeSpan.FromMilliseconds(300));

        cut.WaitForAssertion(() => Assert.Equal(["Eagle Ridge traverse"], Names(cut, "Hikes")));
        Assert.Equal("Filters: Name Contains ridge.", cut.Find("[data-testid=filters]").TextContent.Trim());
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
        Assert.Contains("\"Direction\":\"Descending\"", cut.Find("[data-testid=saved-view]").TextContent);

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

    private static void Sort<T>(IRenderedComponent<T> cut, string column) where T : IComponent =>
        cut.Find($"th[data-title='{column}'] [data-part=sort]").Click();

    private static IReadOnlyList<string> Names<T>(IRenderedComponent<T> cut, string grid) where T : IComponent =>
        Texts(cut.FindAll($"table[aria-label='{grid}'] tbody td[data-column=Name]"));
}
