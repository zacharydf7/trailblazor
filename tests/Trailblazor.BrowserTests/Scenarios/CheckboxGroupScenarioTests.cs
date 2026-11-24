using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CheckboxGroupScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailDays_ShouldGiveTheAppTheTickedDays_WhenDaysAreTickedAndUnticked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox-group/options-and-value");
        var days = page.Tb().CheckboxGroup("Trail days");
        var value = page.Locator("output");
        Assert.Equal(["Saturday"], await days.CheckedAsync());

        // Each box is named by its text. CheckedAsync reads the boxes in the page's order, and the app's value keeps
        // the order they were ticked in.
        await days.CheckAsync("Monday", "Friday");
        Assert.Equal(["Monday", "Friday", "Saturday"], await days.CheckedAsync());
        await Expect(value).ToHaveTextAsync("Trail days value: Saturday, Monday, Friday");

        await days.UncheckAsync("Saturday");
        await Expect(value).ToHaveTextAsync("Trail days value: Monday, Friday");
    }
}
