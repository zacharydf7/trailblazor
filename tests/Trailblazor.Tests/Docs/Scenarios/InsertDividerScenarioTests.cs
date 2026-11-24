using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class InsertDividerScenarioTests : BunitContext
{
    public InsertDividerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Checklist_ShouldTakeANewStepWhereItIsInserted_WhenADividersButtonIsPressed()
    {
        var cut = Render<InsertDividerExample>();

        // A divider is found by its label, and InsertAsync presses its insert button.
        await cut.Tb().InsertDivider("Add a step after Pack the tent").InsertAsync();

        Assert.Equal(["Pack the tent", "New step 1", "Fill the water bottles", "Check the trail map"],
            cut.FindAll("ol > li > p").Select(step => step.TextContent));
    }
}
