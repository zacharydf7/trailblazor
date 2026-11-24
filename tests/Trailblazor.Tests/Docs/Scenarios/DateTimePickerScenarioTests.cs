using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DateTimePickerScenarioTests : BunitContext
{
    public DateTimePickerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task HikeStart_ShouldHoldTheChosenDateAndTime_WhenBothAreChosen()
    {
        var cut = Render<DateTimePickerExample>();
        var start = cut.Tb().DateTimePicker("Guided hike starts");
        var hike = new DateTime(2027, 5, 14, 7, 45, 0);

        // ChooseAsync enters the date and the time, as a user does.
        await start.ChooseAsync(hike);

        Assert.Equal(hike, start.Value);
        Assert.Equal($"Starting {hike:f}.", cut.Find("output").TextContent);
    }
}
