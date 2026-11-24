using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CalendarScenarioTests : BunitContext
{
    public CalendarScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task HikeDate_ShouldGiveTheAppTheDay_WhenADayIsChosen()
    {
        var cut = Render<CalendarExample>();
        var hike = cut.Tb().Calendar("Hike date");
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(10);

        // ChooseAsync picks the day as a user would, through the calendar's year and month views.
        await hike.ChooseAsync(date);

        Assert.Equal($"Hiking on {date:D}.", cut.Find("output").TextContent);
    }
}
