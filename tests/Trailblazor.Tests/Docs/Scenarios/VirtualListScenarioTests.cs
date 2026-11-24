using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class VirtualListScenarioTests : BunitContext
{
    public VirtualListScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Reports_ShouldHoldEveryReport_WhenOnlySomeAreDrawn()
    {
        var cut = Render<VirtualListExample>();
        var reports = cut.Tb().VirtualList("Trail reports");

        // Count is every item the list holds, drawn or not.
        Assert.Equal(5000, reports.Count);

        // bUnit runs no script, so the list draws only the items of its first view; the browser test scrolls to the
        // last.
        Assert.Contains("Report 1: Trail 2", reports.Item(0).TextContent);
    }
}
