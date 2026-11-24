using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TransferListScenarioTests : BunitContext
{
    public TransferListScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Crew_ShouldGiveTheAppWhoIsOnTheTrip_WhenHikersAreMovedAcrossAndBack()
    {
        var cut = Render<TransferListExample>();
        var crew = cut.Tb().TransferList("Trip crew");
        var onTheTrip = cut.Find("output");
        Assert.Equal(["Blake", "Emery"], crew.Chosen);

        // MoveAsync marks the hikers in the available list and presses the button that moves them across. The app gets
        // them in the order of Items, not the order they were moved in.
        await crew.MoveAsync("Casey", "Avery");
        Assert.Equal(["Avery", "Blake", "Casey", "Emery"], crew.Chosen);
        Assert.Equal("Avery, Blake, Casey, Emery", onTheTrip.TextContent);

        await crew.MoveBackAsync("Blake");
        Assert.Equal(["Avery", "Casey", "Emery"], crew.Chosen);
        Assert.Equal("Avery, Casey, Emery", onTheTrip.TextContent);
    }
}
