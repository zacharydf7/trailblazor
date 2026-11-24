using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ButtonGroupScenarioTests : BunitContext
{
    public ButtonGroupScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task ArchivedTripActions_ShouldBeOff_WhenTheGroupIsDisabled()
    {
        var cut = Render<ButtonGroupSharedExample>();

        // A button group has no helper of its own; it is a group named by its aria-label, and its Disabled turns every
        // button in it off.
        var archived = cut.Find("[role=group][aria-label='Archived trip actions']");
        Assert.All(archived.QuerySelectorAll("button"), button => Assert.True(button.HasAttribute("disabled")));
        var active = cut.Find("[role=group][aria-label='Trip actions']");
        Assert.All(active.QuerySelectorAll("button"), button => Assert.False(button.HasAttribute("disabled")));
    }
}
