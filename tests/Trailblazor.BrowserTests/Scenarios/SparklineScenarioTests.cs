namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SparklineScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HikersByTrail_ShouldHaveASparklineNamedForItsRow_WhenTheTableIsShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sparkline/table");

        // A sparkline is a picture, found by its role and its name, which says what its shape shows.
        var picture = page.GetByRole(AriaRole.Img, new() { Name = "Ridge loop by day, busiest at the weekend" });

        // It has a bar for each day. Test the values the app gives it, not the shape it draws.
        await Expect(picture.Locator("rect")).ToHaveCountAsync(7);
    }
}
