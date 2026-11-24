using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ListboxScenarioTests : BunitContext
{
    public ListboxScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Gear_ShouldGiveTheAppWhatIsChosen_WhenOptionsAreChosenAndTakenAway()
    {
        var cut = Render<ListBoxMultipleExample>();
        var gear = cut.Tb().ListBox("Gear to pack");
        Assert.Equal(["Tent"], gear.Chosen);

        await gear.ChooseAsync("Stove", "Headlamp");
        Assert.Equal(["Tent", "Stove", "Headlamp"], gear.Chosen);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Packing: Tent, Stove, Headlamp");

        await gear.UnchooseAsync("Tent");
        Assert.Equal(["Stove", "Headlamp"], gear.Chosen);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Packing: Stove, Headlamp");
    }
}
