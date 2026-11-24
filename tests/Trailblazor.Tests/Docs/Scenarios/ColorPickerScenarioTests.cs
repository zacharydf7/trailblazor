using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ColorPickerScenarioTests : BunitContext
{
    public ColorPickerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task WarningColor_ShouldClearItsError_WhenAColorThatShowsIsChosen()
    {
        var cut = Render<ColorPickerValidationExample>();
        var warning = cut.Tb().ColorPicker("Warning color");
        Assert.Equal("White won't show on the trail map.", warning.Error);

        // ChooseAsync types the color and commits it, as pressing Enter does, and the app's Validation runs again.
        await warning.ChooseAsync("#e8a33d");

        Assert.Equal("#e8a33d", warning.Value);
        Assert.Null(warning.Error);
    }
}
