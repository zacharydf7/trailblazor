using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class GaugeScenarioTests : BunitContext
{
    public GaugeScenarioTests() => this.AddTrailblazor();

    [Fact]
    public void SummitWind_ShouldSayItsValueAndRange_WhenTheWeatherStationIsConnected()
    {
        var cut = Render<GaugeEmptyExample>();

        // A gauge is a meter. Its value text is what it shows and what screen readers hear.
        Assert.Equal("Offline", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));

        cut.Find("button").Click();

        // The text holds the value and the name of the range the value is in.
        Assert.Equal("52, Windy", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
    }
}
