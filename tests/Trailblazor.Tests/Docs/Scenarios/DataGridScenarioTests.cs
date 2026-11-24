using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DataGridScenarioTests : BunitContext
{
    public DataGridScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Trails_ShouldSortPageAndReportWhatIsChosen_WhenAUserWorksThroughTheGrid()
    {
        var cut = Render<DataGridExample>();
        var trails = cut.Tb().Grid("Trails");
        var chosen = cut.Find("output");

        // The longest trail comes first, and the first page holds five of the seven.
        await trails.SortAsync("Length (km)", descending: true);
        Assert.Equal(5, trails.RowCount);
        Assert.Equal("Eagle Ridge traverse", trails.Rows[0][0]);

        // SelectAsync finds a row by the exact text of one of its cells.
        await trails.SelectAsync("Eagle Ridge traverse");
        await trails.SelectAsync("Summit Gate");
        Assert.Contains("Eagle Ridge traverse", chosen.TextContent);
        Assert.Contains("Summit Gate", chosen.TextContent);

        // The selection is kept from page to page.
        await trails.GoToPageAsync(2);
        Assert.Equal(2, trails.RowCount);
        Assert.Contains("Summit Gate", chosen.TextContent);
    }
}
