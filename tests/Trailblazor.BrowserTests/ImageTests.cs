using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class ImageTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Image_ShouldShrinkToItsContainerKeepingItsShape_WhenFluid(string browser)
    {
        var page = await OpenAsync(browser, "/examples/image");

        var box = (await page.GetByRole(AriaRole.Img, new() { Name = "A trail winding between two hills" })
            .BoundingBoxAsync())!;

        Assert.Equal(320, box.Width, tolerance: 0.5);
        Assert.Equal(180, box.Height, tolerance: 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Image_ShouldFillItsBoxAsFitSays_WhenAFitIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/image");

        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "The trail, cropped to fill a square" }))
            .ToHaveCSSAsync("object-fit", "cover");
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "The trail, shrunk to fit a square" }))
            .ToHaveCSSAsync("object-fit", "contain");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Image_ShouldShowItsFallback_WhenItsAddressFailsToLoad(string browser)
    {
        var page = await OpenAsync(browser, "/examples/image");
        var image = page.GetByRole(AriaRole.Img, new() { Name = "The trail, shown from the fallback picture" });

        await Expect(image).ToHaveAttributeAsync("src", new Regex("^data:image/svg"));
        Assert.True(await image.EvaluateAsync<bool>("image => image.complete && image.naturalWidth > 0"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Image_ShouldBeLeftOutOfTheAccessibilityTree_WhenItsAltIsEmpty(string browser)
    {
        var page = await OpenAsync(browser, "/examples/image");

        await Expect(page.Locator("img")).ToHaveCountAsync(7);
        await Expect(page.GetByRole(AriaRole.Img)).ToHaveCountAsync(6);
    }
}
