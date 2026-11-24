using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TextFieldScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Fields_ShouldShowAndClearTheirErrors_WhenAValueIsWrongThenFixed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field/validation-and-server-errors");
        var permit = page.Tb().TextField("Permit number");
        var handle = page.Tb().TextField("Handle");

        // The field's own check runs once the value is entered.
        await permit.EnterAsync("2048");
        Assert.True(await permit.InvalidAsync());
        Assert.Equal("Enter six digits.", await permit.ErrorAsync());

        await permit.EnterAsync("204815");
        Assert.False(await permit.InvalidAsync());

        // The server's answer shows on the field until the handle changes.
        await handle.EnterAsync("robin");
        await page.Tb().Button("Check the handle").ClickAsync();
        Assert.Equal("That handle is taken.", await handle.ErrorAsync());

        await handle.EnterAsync("wren");
        Assert.Null(await handle.ErrorAsync());
    }
}
