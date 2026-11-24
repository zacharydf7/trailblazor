using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DateRangePickerScenarioTests : BunitContext
{
    public DateRangePickerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TripDates_ShouldGiveTheAppTheRange_WhenBothDatesAreChosen()
    {
        var cut = Render<DateRangePickerExample>();
        var trip = cut.Tb().DateRangePicker("Trip dates");
        var summary = cut.Find("output");
        var (start, end) = (new DateOnly(2027, 6, 4), new DateOnly(2027, 6, 8));

        // The helper takes both dates, so the test doesn't depend on how the user's language writes a date.
        await trip.ChooseAsync(start, end);

        Assert.Equal((start, end), trip.Range);
        Assert.Equal($"5 days, from {start:D}.", summary.TextContent);
    }
}
