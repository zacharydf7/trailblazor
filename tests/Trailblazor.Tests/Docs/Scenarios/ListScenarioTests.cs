using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ListScenarioTests : BunitContext
{
    public ListScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Campsites_ShouldShowTheChosenSiteAndLoseARemovedOne_WhenItemsAreClickedAndRemoved()
    {
        var cut = Render<ListExample>();
        var campsites = cut.Tb().List("Campsites");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Showing: Pine Hollow");

        // An item is found by its text, and ClickAsync runs the item's OnClick.
        await campsites.ClickAsync("Eagle Ridge");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Showing: Eagle Ridge");

        // The item's own buttons are found by their names, as any button is.
        await cut.Tb().Button("Remove Eagle Ridge").ClickAsync();
        Assert.DoesNotContain("Eagle Ridge", campsites.Items);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Showing: nothing");
    }
}
