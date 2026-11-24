using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LightboxScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailPhotos_ShouldShowTheClickedPhotoAndWrapAround_WhenOpenedAndStepped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/lightbox/basic");
        // A photo's button is named by the photo's alternative text, and opens the lightbox on that photo.
        await page.Tb().Button("Cedar Flats at dusk").ClickAsync();
        var photos = page.Tb().Lightbox("Trail photos");
        Assert.Equal("Cedar Flats at dusk", await photos.AltAsync());

        await photos.NextAsync();
        Assert.Equal("Pine Hollow falls, a tall photo", await photos.CaptionAsync());

        // Next on the last photo goes back to the first.
        await photos.NextAsync();
        Assert.Equal("Eagle Ridge, from the north shore", await photos.CaptionAsync());

        await photos.CloseAsync();
        Assert.False(await page.Tb().Dialog("Trail photos").IsOpenAsync());
    }
}
