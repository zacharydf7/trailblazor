using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DatePickerScenarioTests : BunitContext
{
    public DatePickerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task StartDate_ShouldGiveTheAppTheDate_WhenADateIsChosenAndCleared()
    {
        var cut = Render<DatePickerExample>();
        var start = cut.Tb().DatePicker("Start date");
        var shown = cut.Find("output");
        var date = new DateOnly(2027, 5, 14);

        // The helper takes a DateOnly, so the test doesn't depend on how the user's language writes a date.
        await start.ChooseAsync(date);
        Assert.Equal(date, start.Value);
        Assert.Equal($"Starting {date:D}.", shown.TextContent);

        await start.ChooseAsync(null);
        Assert.Null(start.Value);
        Assert.Equal("No date chosen yet.", shown.TextContent);
    }
}
