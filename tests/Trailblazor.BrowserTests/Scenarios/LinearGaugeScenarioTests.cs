namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LinearGaugeScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BedsBooked_ShouldSayEachHutsValueAndRange_WhenTheTableIsShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/linear-gauge/table");

        // A gauge is a meter, found by its role and its name. Its value text is what it shows and what screen
        // readers hear: the value and the name of the range the value is in.
        var lower = page.GetByRole(AriaRole.Meter, new() { Name = "Lower hut, beds booked tonight" });
        await Expect(lower).ToHaveAttributeAsync("aria-valuetext", "12 of 40, Quiet");

        var summit = page.GetByRole(AriaRole.Meter, new() { Name = "Summit hut, beds booked tonight" });
        await Expect(summit).ToHaveAttributeAsync("aria-valuetext", "15 of 16, Nearly full");
    }
}
