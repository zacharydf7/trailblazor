using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class FileUploadScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangerReport_ShouldAskForAnotherName_WhenTheFileNameHasSpaces(string browser)
    {
        var page = await OpenAsync(browser, "/examples/file-upload/validation");
        var report = page.Tb().FileUpload("Ranger report");
        var text = "Bridge out at Pine Hollow"u8.ToArray();

        // ChooseAsync takes files made in the test, and waits until the app's ValueChanged has run.
        await report.ChooseAsync(
            new FilePayload { Name = "ranger report.txt", MimeType = "text/plain", Buffer = text });
        Assert.Equal("Rename the file so its name has no spaces.", await report.ErrorAsync());

        await report.ChooseAsync(
            new FilePayload { Name = "ranger-report.txt", MimeType = "text/plain", Buffer = text });
        Assert.Equal(["ranger-report.txt"], await report.FilesAsync());
        Assert.Null(await report.ErrorAsync());
    }
}
