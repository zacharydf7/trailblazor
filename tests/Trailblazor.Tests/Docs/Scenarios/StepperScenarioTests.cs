using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class StepperScenarioTests : BunitContext
{
    public StepperScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Booking_ShouldMoveThroughItsSteps_WhenNextIsPressedAndAStepIsChosen()
    {
        var cut = Render<StepperExample>();
        var booking = cut.Tb().Stepper("Trip booking");
        Assert.Equal(["Trip", "Gear", "Payment"], booking.Steps);
        Assert.Equal("Trip", booking.Current);

        await cut.Tb().Button("Next").ClickAsync();
        Assert.Equal("Gear", booking.Current);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Choose what to rent for the trip.");

        // A finished step can be chosen again. GoToAsync clicks a step by its title.
        await booking.GoToAsync("Trip");
        Assert.Equal("Trip", booking.Current);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Pick the trail and the dates.");
    }
}
