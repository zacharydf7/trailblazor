using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SwitchScenarioTests : BunitContext
{
    public SwitchScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task AutoSave_ShouldGiveTheAppItsState_WhenTurnedOffAndOn()
    {
        var cut = Render<SwitchExample>();
        var autoSave = cut.Tb().Switch("Auto-save");
        var value = cut.Find("output");
        Assert.True(autoSave.Checked);

        await autoSave.UncheckAsync();
        Assert.Equal("Auto-save value: False", value.TextContent);

        await autoSave.CheckAsync();
        Assert.Equal("Auto-save value: True", value.TextContent);
    }
}
