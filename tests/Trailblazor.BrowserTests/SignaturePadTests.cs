using System.Globalization;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SignaturePadTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Output(IPage page, string name) =>
        page.GetByRole(AriaRole.Status, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Draw_ShouldAddAStrokeAndUndoTakesItBack_WhenDrawnWithTheMouse(string browser)
    {
        var page = await OpenAsync(browser, "/signature-pad");
        var pad = page.Tb().SignaturePad("Signature");

        await pad.DrawAsync((50, 50), (150, 120), (300, 80));

        await Expect(Output(page, "Signed")).ToHaveTextAsync("1 strokes");
        await Expect(page.Locator("[data-part=surface]")).ToHaveAttributeAsync("aria-label", "Signature, signed");
        await pad.UndoAsync();
        await Expect(Output(page, "Signed")).ToHaveTextAsync("nothing");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Draw_ShouldKeepPointsInTheSignaturesSpace_WhenThePadIsShownSmaller(string browser)
    {
        var page = await OpenAsync(browser, "/signature-pad");
        Assert.InRange((await page.Locator("[data-part=surface]").BoundingBoxAsync())!.Width, 290, 310);

        await page.Tb().SignaturePad("Signature").DrawAsync((50, 50), (150, 120));

        await Expect(Output(page, "First point")).Not.ToHaveTextAsync("");
        var point = (await Output(page, "First point").TextContentAsync())!.Split(',')
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToArray();
        Assert.InRange(point[0], 48, 52);
        Assert.InRange(point[1], 48, 52);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TypeName_ShouldSignFromTheKeyboard_WhenTheToggleIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/signature-pad");
        await page.Locator("[data-part=type-name]").FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.Locator("[data-part=type-name]")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.GetByLabel("Your full name").FillAsync("Ada Lovelace");
        await page.Keyboard.PressAsync("Tab");

        await Expect(Output(page, "Signed")).ToHaveTextAsync("typed Ada Lovelace");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToPngAsync_ShouldMakeAPng_WhenSigned(string browser)
    {
        var page = await OpenAsync(browser, "/signature-pad");
        await page.Tb().SignaturePad("Signature").DrawAsync((50, 50), (150, 120));

        await page.GetByRole(AriaRole.Button, new() { Name = "Make image" }).ClickAsync();

        await Expect(Output(page, "Image")).ToHaveTextAsync("89504E47");
    }
}
