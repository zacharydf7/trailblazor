using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PropertyGridScenarioTests : BunitContext
{
    public PropertyGridScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailProperties_ShouldTellTheAppWhatChanged_WhenAPropertyIsEdited()
    {
        var cut = Render<PropertyGridExample>();
        // PropertyGrid limits the helpers to the grid, whose fields are found by their labels, as anywhere else.
        var trail = cut.Tb().PropertyGrid("Trail properties");
        Assert.Equal("Ridge loop", trail.TextField("Name").Text);

        await trail.Select("Surface").ChooseAsync("Dirt");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Last changed: Surface");

        await trail.Switch("Open").UncheckAsync();
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Last changed: Open");
    }
}
