using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ModelFieldsScenarioTests : BunitContext
{
    public ModelFieldsScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task NewHike_ShouldSaveOnlyOnceItsTrailIsNamed_WhenSubmitted()
    {
        var cut = Render<ModelFieldsExample>();
        // Each field the model makes is found by its label with the helper for its kind.
        var name = cut.Tb().TextField("Trail name");

        await cut.Tb().Button("Save hike").ClickAsync();
        Assert.Equal("Trail name is required.", name.Error);

        await name.EnterAsync("Ridge loop");
        await cut.Tb().NumberField("Length (km)").EnterAsync("12");
        await cut.Tb().Button("Save hike").ClickAsync();

        Assert.Null(name.Error);
        Assert.Equal("Saved Ridge loop.", cut.Find("output").TextContent);
    }
}
