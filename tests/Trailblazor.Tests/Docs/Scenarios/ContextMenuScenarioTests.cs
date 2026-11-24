using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ContextMenuScenarioTests : BunitContext
{
    public ContextMenuScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailMenu_ShouldRemoveATrailAndAddOne_WhenItemsAreChosenForARowAndForTheArea()
    {
        var cut = Render<ContextMenuKeysExample>();
        var actions = cut.Tb().ContextMenu("Trail actions");
        var last = cut.Find("output");

        // On names what was right-clicked, by its data-context-key, and ChooseAsync chooses an item of the menu that
        // opens for it.
        await actions.On("Lake path").ChooseAsync("Remove");
        Assert.Equal("Removed Lake path", last.TextContent);
        Assert.DoesNotContain(cut.FindAll("button"), button => button.TextContent.Trim() == "Lake path");

        // Without On, the menu is the one for the rest of the area.
        await actions.ChooseAsync("Add a trail");
        Assert.Equal("Added New trail 1", last.TextContent);
    }
}
