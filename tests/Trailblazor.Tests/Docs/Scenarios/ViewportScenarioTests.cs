using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ViewportScenarioTests : BunitContext
{
    public ViewportScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task CampgroundPlan_ShouldTakeTheAppsZoomAndFit_WhenItsButtonsArePressed()
    {
        var cut = Render<ViewportZoomExample>();
        var plan = cut.Tb().Viewport("Campground plan");
        Assert.Equal(1, plan.Zoom);

        // The app's buttons set the bound Zoom and Fit, which the viewport takes.
        await cut.Tb().Button("Show at 200%").ClickAsync();
        Assert.Equal(2, plan.Zoom);

        await cut.Tb().Button("Fit the width").ClickAsync();
        Assert.Equal(ViewportFit.Width, plan.CurrentFit);
    }
}
