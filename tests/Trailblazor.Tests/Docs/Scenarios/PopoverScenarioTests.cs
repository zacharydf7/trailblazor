using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PopoverScenarioTests : BunitContext
{
    public PopoverScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Filters_ShouldKeepTheTickedFilter_WhenThePopoverIsClosedWithApplyAndOpenedAgain()
    {
        var cut = Render<PopoverOpenExample>();
        // A popover is found by the name of the button that opens it. bUnit's markup holds the panel whether it's open
        // or not, so this test checks the app's state; the Playwright test checks that the panel opens and closes.
        await cut.Tb().Popover("Filters").OpenAsync();

        await cut.Tb().Checkbox("Only open items").CheckAsync();
        await cut.Tb().Button("Apply").ClickAsync();
        Assert.Equal("Open items", cut.Find("output").TextContent);

        await cut.Tb().Button("Show filter options").ClickAsync();
        Assert.True(cut.Tb().Checkbox("Only open items").Checked);
    }
}
