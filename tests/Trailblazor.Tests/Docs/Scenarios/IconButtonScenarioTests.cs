using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class IconButtonScenarioTests : BunitContext
{
    public IconButtonScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Bold_ShouldMakeEveryWordBold_WhenPressedWhileOnlySomeAre()
    {
        var cut = Render<IconButtonMixedExample>();
        // An icon button is a button to the helpers, found by its AriaLabel.
        var bold = cut.Tb().Button("Bold");

        // Only one word is bold, so the button shows its mixed state.
        Assert.True(bold.Mixed);

        await bold.ClickAsync();

        Assert.True(bold.Pressed);
        Assert.True(cut.Tb().Checkbox("Pack in bold").Checked);
        Assert.True(cut.Tb().Checkbox("today in bold").Checked);
    }
}
