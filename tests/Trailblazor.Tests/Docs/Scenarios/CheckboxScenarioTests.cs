using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CheckboxScenarioTests : BunitContext
{
    public CheckboxScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Waiver_ShouldDropItsError_WhenTheSafetyNotesAreTicked()
    {
        var cut = Render<CheckboxRequiredExample>();
        var read = cut.Tb().Checkbox("I've read the safety notes");

        // The example validates its form as it opens, so the unticked box already shows its message.
        Assert.False(read.Checked);
        Assert.Equal("Read the safety notes first.", read.Error);

        await read.CheckAsync();

        Assert.True(read.Checked);
        Assert.Null(read.Error);
    }
}
