using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class OverlayScenarioTests : BunitContext
{
    public OverlayScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task PageOverlay_ShouldClose_WhenClickedOutsideItsContentOrOnEscape()
    {
        var cut = Render<OverlayExample>();
        var overlay = cut.Tb().Overlay();

        await cut.Tb().Button("Show page overlay").ClickAsync();
        Assert.True(overlay.IsOpen);

        // ClickAsync clicks the overlay outside its content, which closes an overlay with CloseOnClick.
        await overlay.ClickAsync();
        Assert.False(overlay.IsOpen);

        // Escape closes it too.
        await cut.Tb().Button("Show page overlay").ClickAsync();
        await overlay.PressEscapeAsync();
        Assert.False(overlay.IsOpen);
    }
}
