namespace Trailblazor.BrowserTests;

public class ModelFieldsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tab_ShouldVisitEachFieldInTheOrderItShows_WhenPressedThroughTheForm(string browser)
    {
        var page = await OpenAsync(browser, "/model-fields");
        await page.GetByLabel("Trail name").FocusAsync();

        foreach (var label in new[] { "Length (km)", "Notes", "Email" })
        {
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.GetByLabel(label)).ToBeFocusedAsync();
        }

        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Submit_ShouldFocusTheFirstFieldWithAnError_WhenARequiredPropertyIsEmpty(string browser)
    {
        var page = await OpenAsync(browser, "/model-fields");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await save.ClickAsync();
        await Expect(page.GetByLabel("Trail name")).ToBeFocusedAsync();
        await Expect(page.Locator("[data-part=field][data-name=Name]")).ToContainTextAsync("Trail name is required");
        await page.GetByLabel("Trail name").FillAsync("Ridge loop");
        await save.ClickAsync();

        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Saved" })).ToHaveTextAsync("saved");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Fields_ShouldStackInOneColumn_WhenTheFormIsTooNarrowForTwo(string browser)
    {
        var page = await OpenAsync(browser, "/model-fields");
        var name = page.Locator("[data-part=field][data-name=Name]");
        var length = page.Locator("[data-part=field][data-name=Length]");
        var notes = page.Locator("[data-part=field][data-name=Notes]");
        var (wideName, wideLength) = ((await name.BoundingBoxAsync())!, (await length.BoundingBoxAsync())!);
        Assert.Equal(wideName.Y, wideLength.Y, 1.0);
        Assert.True(wideLength.X > wideName.X + wideName.Width);
        Assert.True((await notes.BoundingBoxAsync())!.Width > wideName.Width * 2);

        await page.SetViewportSizeAsync(360, 800);

        var (narrowName, narrowLength) = ((await name.BoundingBoxAsync())!, (await length.BoundingBoxAsync())!);
        Assert.Equal(narrowName.X, narrowLength.X, 1.0);
        Assert.True(narrowLength.Y > narrowName.Y + narrowName.Height);
    }
}
