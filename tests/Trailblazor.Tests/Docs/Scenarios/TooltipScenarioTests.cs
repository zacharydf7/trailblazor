using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TooltipScenarioTests : BunitContext
{
    public TooltipScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task AddButton_ShouldBeDescribedByItsTooltip_WhenThePointerRestsOnIt()
    {
        var cut = Render<TooltipExample>();

        // The tooltip is found by its trigger's name. bUnit's markup holds its text from the start; only a browser
        // shows it on hover or focus.
        Assert.Equal("Add an item", cut.Tb().Tooltip("Add").Text);
    }
}
