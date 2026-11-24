using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RadioGroupScenarioTests : BunitContext
{
    public RadioGroupScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task ArrivalDay_ShouldShowTheAppsErrorUntilAnotherDayIsChosen_WhenTheDayIsChecked()
    {
        var cut = Render<RadioGroupErrorExample>();
        var day = cut.Tb().RadioGroup("Arrival day");
        Assert.Equal("Saturday", day.Chosen);

        // The app's answer shows on the group until the day changes.
        await cut.Tb().Button("Check the day").ClickAsync();
        Assert.Equal("Saturday is fully booked.", day.Error);

        await day.ChooseAsync("Friday");

        Assert.Equal("Friday", day.Chosen);
        Assert.Null(day.Error);
    }
}
