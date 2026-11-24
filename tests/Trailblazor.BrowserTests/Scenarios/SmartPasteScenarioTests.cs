using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SmartPasteScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TripLeaderForm_ShouldFillItsFields_WhenTheLeadersDetailsArePasted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/smart-paste/basic");
        var paste = page.Tb().SmartPaste();

        // PasteAsync makes the clipboard refuse, so the box opens, enters the text there, and waits until the form is
        // filled.
        await paste.PasteAsync("Robin Vale, robin@ridge.example, 555-0100, a party of 4, steady pace");

        Assert.Equal("Filled 5 fields", await paste.StatusAsync());
        Assert.Equal("Robin Vale", await page.Tb().TextField("Leader's name").TextAsync());
        Assert.Equal("robin@ridge.example", await page.Tb().TextField("Email").TextAsync());
        Assert.Equal("555-0100", await page.Tb().TextField("Phone").TextAsync());
        Assert.Equal("4", await page.Tb().NumberField("Party size").TextAsync());
        Assert.Equal("Steady", await page.Tb().RadioGroup("Pace").ChosenAsync());
    }
}
