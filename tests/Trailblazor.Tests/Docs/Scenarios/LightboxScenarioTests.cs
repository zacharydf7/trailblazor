using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LightboxScenarioTests : BunitContext
{
    public LightboxScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailPhotos_ShouldShowTheClickedPhotoAndWrapAround_WhenOpenedAndStepped()
    {
        var cut = Render<LightboxExample>();
        // A photo's button is named by the photo's alternative text, and opens the lightbox on that photo.
        await cut.Tb().Button("Cedar Flats at dusk").ClickAsync();
        var photos = cut.Tb().Lightbox("Trail photos");
        Assert.Equal("Cedar Flats at dusk", photos.Alt);

        await photos.NextAsync();
        Assert.Equal("Pine Hollow falls, a tall photo", photos.Caption);

        // Next on the last photo goes back to the first.
        await photos.NextAsync();
        Assert.Equal("Eagle Ridge, from the north shore", photos.Caption);

        await photos.CloseAsync();
        Assert.False(cut.Tb().Dialog("Trail photos").IsOpen);
    }
}
