using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class StepperScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Booking_ShouldMoveThroughItsSteps_WhenNextIsPressedAndAStepIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/stepper/basic");
        var booking = page.Tb().Stepper("Trip booking");
        Assert.Equal(["Trip", "Gear", "Payment"], await booking.StepsAsync());
        Assert.Equal("Trip", await booking.CurrentAsync());

        await page.Tb().Button("Next").ClickAsync();
        Assert.Equal("Gear", await booking.CurrentAsync());
        await Expect(page.GetByText("Choose what to rent for the trip.")).ToBeVisibleAsync();

        // A finished step can be chosen again. GoToAsync clicks a step by its title.
        await booking.GoToAsync("Trip");
        Assert.Equal("Trip", await booking.CurrentAsync());
        await Expect(page.GetByText("Pick the trail and the dates.")).ToBeVisibleAsync();
    }
}
