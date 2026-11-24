using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChipScenarioTests : BunitContext
{
    public ChipScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Tags_ShouldLoseATag_WhenItsChipIsRemoved()
    {
        var cut = Render<ChipExample>();
        var tags = cut.Tb().ChipGroup("Selected tags");
        Assert.Equal(["Invoices", "Archive", "2026"], tags.Chips);

        // RemoveAsync presses the chip's remove button and waits for the app's handler.
        await tags.Chip("Archive").RemoveAsync();

        Assert.Equal(["Invoices", "2026"], tags.Chips);
    }
}
