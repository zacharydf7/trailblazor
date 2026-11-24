using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ThemePickerScenarioTests : BunitContext
{
    public ThemePickerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Theme_ShouldShowTheChosenMode_WhenAModeIsChosen()
    {
        var cut = Render<ThemePickerExample>();
        // The picker is a toggle group named "Theme". bUnit has no page around the example, so this test checks what
        // the app shows; the browser test checks the page's html too.
        var theme = cut.Tb().ToggleGroup("Theme");
        Assert.Equal(["Match the device"], theme.Chosen);

        await theme.ChooseAsync("Dark");

        Assert.Equal(["Dark"], theme.Chosen);
        Assert.StartsWith("The page is in Dark mode", cut.Find("output").TextContent);
    }
}
