using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class InPlaceEditorScenarioTests : BunitContext
{
    public InPlaceEditorScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailName_ShouldChangeOnlyWhenSaved_WhenEditedInPlace()
    {
        var cut = Render<InPlaceEditorExample>();
        var name = cut.Tb().InPlaceEditor("Trail name");
        var saved = cut.Find("output");
        Assert.Equal("Pine Hollow loop", name.Text);

        // EditAsync opens the editor, which holds the app's own field.
        await name.EditAsync();
        await cut.Tb().TextField("Trail name").EnterAsync("Eagle Ridge traverse");
        await name.SaveAsync();
        Assert.Equal("Eagle Ridge traverse", name.Text);
        Assert.Equal("Eagle Ridge traverse, 4 miles", saved.TextContent);

        // Cancel keeps the value the app has.
        await name.EditAsync();
        await cut.Tb().TextField("Trail name").EnterAsync("Somewhere else");
        await name.CancelAsync();
        Assert.Equal("Eagle Ridge traverse", name.Text);
        Assert.Equal("Eagle Ridge traverse, 4 miles", saved.TextContent);
    }
}
