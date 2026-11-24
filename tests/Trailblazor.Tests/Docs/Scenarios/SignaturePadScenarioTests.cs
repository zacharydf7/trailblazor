using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SignaturePadScenarioTests : BunitContext
{
    public SignaturePadScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Waiver_ShouldBeSigned_WhenDrawnOrWhenANameIsTyped()
    {
        var cut = Render<SignaturePadExample>();
        var waiver = cut.Tb().SignaturePad("Sign the trail waiver");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Not signed yet.");

        // Each DrawAsync is one stroke through points in the pad's space of 600 by 200.
        await waiver.DrawAsync((40, 150), (120, 60), (200, 140));
        await waiver.DrawAsync((240, 150), (320, 60));
        Assert.True(waiver.IsSigned);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Signed in 2 strokes.");

        // ClearAsync starts over, and a typed name signs the waiver too.
        await waiver.ClearAsync();
        Assert.False(waiver.IsSigned);
        await waiver.TypeNameAsync("Robin Vale");
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Signed as Robin Vale.");
    }
}
