using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DrawerScenarioTests : BunitContext
{
    public DrawerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Filters_ShouldKeepTheTickedFiltersUntilReset_WhenTheDrawerIsClosedAndOpenedAgain()
    {
        var cut = Render<DrawerExample>();
        // A drawer is a dialog to the helpers, found by its title.
        var filters = cut.Tb().Dialog("Trail filters");

        await cut.Tb().Button("Filters").ClickAsync();
        await cut.Tb().Checkbox("Dogs allowed").CheckAsync();
        await filters.ChooseAsync("Show trails");
        Assert.False(filters.IsOpen);

        // The app keeps the filters, so they're still ticked when the drawer opens again.
        await cut.Tb().Button("Filters").ClickAsync();
        Assert.True(cut.Tb().Checkbox("Dogs allowed").Checked);

        // Reset leaves the drawer open, so it's clicked as any button is.
        await cut.Tb().Button("Reset").ClickAsync();
        Assert.False(cut.Tb().Checkbox("Dogs allowed").Checked);
    }
}
