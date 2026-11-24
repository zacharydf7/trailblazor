using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class MenuScenarioTests : BunitContext
{
    public MenuScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task ViewMenu_ShouldGiveTheAppItsChecksAndUnits_WhenItemsAreChosen()
    {
        var cut = Render<MenuCheckExample>();
        var view = cut.Tb().Menu("View");
        var state = cut.Find("output");
        Assert.True(view.Checked("Map"));

        // ChooseAsync clicks the item with this name, as a user does from the open menu.
        await view.ChooseAsync("Elevation");
        await view.ChooseAsync("Kilometers");

        Assert.True(view.Checked("Elevation"));
        Assert.True(view.Checked("Kilometers"));
        Assert.Equal("Kilometers, map, elevation", state.TextContent);
    }
}
