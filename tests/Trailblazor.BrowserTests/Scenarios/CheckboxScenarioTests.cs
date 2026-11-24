using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CheckboxScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Waiver_ShouldDropItsError_WhenTheSafetyNotesAreTicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox/required");
        var read = page.Tb().Checkbox("I've read the safety notes");

        // The example validates its form as it opens, so the unticked box shows its message.
        await Expect(page.GetByText("Read the safety notes first.")).ToBeVisibleAsync();
        Assert.False(await read.CheckedAsync());

        await read.CheckAsync();

        Assert.True(await read.CheckedAsync());
        Assert.Null(await read.ErrorAsync());
    }
}
