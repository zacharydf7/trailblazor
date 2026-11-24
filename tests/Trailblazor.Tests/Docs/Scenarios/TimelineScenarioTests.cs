using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TimelineScenarioTests : BunitContext
{
    public TimelineScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TripHistory_ShouldListItsEventsInOrder_WhenRead()
    {
        var cut = Render<TimelineExample>();
        var history = cut.Find("ol[aria-label='Trip history']");

        // A timeline has no helper of its own; each event's title and time are its item's title and time parts.
        Assert.Equal(["Trip booked", "Gear rented", "Trail closed for snow, so the date moved"],
            history.QuerySelectorAll("[data-part=title]").Select(title => title.TextContent));
        // The time element holds the moment in a form that doesn't depend on the culture.
        Assert.Equal("2026-03-03T09:30:00.000+00:00", history.QuerySelector("time")!.GetAttribute("datetime"));
    }
}
