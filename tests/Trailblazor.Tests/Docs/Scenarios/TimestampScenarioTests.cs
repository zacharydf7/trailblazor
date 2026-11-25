using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TimestampScenarioTests : BunitContext
{
    public TimestampScenarioTests() => this.AddTrailblazor();

    [Fact]
    public void Departure_ShouldShowInTokyoTime_WhenTokyoIsChosen()
    {
        var cut = Render<TimestampZoneExample>();

        // The timestamp has no helper; it is a time element, read by its text and its datetime.
        var departure = cut.Find("time");
        Assert.EndsWith(" UTC+9", departure.TextContent);
        Assert.Equal("2026-12-04T14:30:00.000+00:00", departure.GetAttribute("datetime"));
    }
}
