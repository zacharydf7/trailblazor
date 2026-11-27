using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LinearGaugeScenarioTests : BunitContext
{
    public LinearGaugeScenarioTests() => this.AddTrailblazor();

    [Fact]
    public void BedsBooked_ShouldSayEachHutsValueAndRange_WhenTheTableIsShown()
    {
        var cut = Render<LinearGaugeTableExample>();

        // A gauge is a meter, found by its name. Its value text is what it shows and what screen readers hear:
        // the value and the name of the range the value is in.
        var lower = cut.Find("[role=meter][aria-label='Lower hut, beds booked tonight']");
        Assert.Equal("12 of 40, Quiet", lower.GetAttribute("aria-valuetext"));

        var summit = cut.Find("[role=meter][aria-label='Summit hut, beds booked tonight']");
        Assert.Equal("15 of 16, Nearly full", summit.GetAttribute("aria-valuetext"));
    }
}
