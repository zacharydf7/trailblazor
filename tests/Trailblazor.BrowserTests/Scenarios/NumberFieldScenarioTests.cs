using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class NumberFieldScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Quantity_ShouldKeepTheAppsValueWithinItsLimits_WhenSteppedAndTyped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field/limits-and-steps");
        var quantity = page.Tb().NumberField("Quantity");
        var value = page.Locator("output");

        // StepAsync presses the field's step buttons, up for a positive count and down for a negative one.
        await quantity.StepAsync(2);
        await Expect(value).ToHaveTextAsync("Quantity value: 3");

        // A number past the limit stays in the field with an error, and the app keeps the value it had.
        await quantity.EnterAsync("12");
        Assert.True(await quantity.InvalidAsync());
        Assert.Equal("12", await quantity.TextAsync());
        await Expect(value).ToHaveTextAsync("Quantity value: 3");

        await quantity.EnterAsync("7");
        Assert.False(await quantity.InvalidAsync());
        await Expect(value).ToHaveTextAsync("Quantity value: 7");
    }
}
