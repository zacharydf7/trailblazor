using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PaginationScenarioTests : BunitContext
{
    public PaginationScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Reports_ShouldGiveTheAppThePage_WhenAnotherPageIsChosen()
    {
        var cut = Render<PaginationExample>();
        var reports = cut.Tb().Pagination("Trail reports");
        Assert.Equal(1, reports.Page);

        // GoToPageAsync clicks its way there as a user would: the page's own button, or the nearest page shown on
        // the way to it. Page 7 isn't shown from page 1, so the app hears of pages 5 and 6 on the way.
        await reports.GoToPageAsync(7);

        Assert.Equal(7, reports.Page);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Showing report page 7 of 20");
    }
}
