using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TimePickerScenarioTests : BunitContext
{
    public TimePickerScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task StartTime_ShouldGiveTheAppTheTime_WhenATimeIsChosen()
    {
        var cut = Render<TimePickerExample>();
        var start = cut.Tb().TimePicker("Start time");
        var time = new TimeOnly(7, 45);

        // The helper takes a TimeOnly, so the test doesn't depend on how the user's language writes a time.
        await start.ChooseAsync(time);

        Assert.Equal(time, start.Value);
        Assert.Equal($"Starting at {time:t}.", cut.Find("output").TextContent);
    }
}
