using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SignaturePadScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override bool HoldsTheMouse => true;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Waiver_ShouldBeSigned_WhenDrawnOrWhenANameIsTyped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/signature-pad/basic");
        var waiver = page.Tb().SignaturePad("Sign the trail waiver");
        await Expect(page.GetByText("Not signed yet.")).ToBeVisibleAsync();

        // Each DrawAsync draws one stroke with the mouse through points in the pad's space of 600 by 200, and waits
        // until the app has it.
        await waiver.DrawAsync((40, 150), (120, 60), (200, 140));
        await waiver.DrawAsync((240, 150), (320, 60));
        Assert.True(await waiver.IsSignedAsync());
        await Expect(page.GetByText("Signed in 2 strokes.")).ToBeVisibleAsync();

        // ClearAsync starts over, and a typed name signs the waiver too.
        await waiver.ClearAsync();
        Assert.False(await waiver.IsSignedAsync());
        await waiver.TypeNameAsync("Robin Vale");
        await Expect(page.GetByText("Signed as Robin Vale.")).ToBeVisibleAsync();
    }
}
