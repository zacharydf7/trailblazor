namespace Trailblazor.BrowserTests;

public class FieldInfoTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Field_ShouldPutTheInfoButtonAfterALongLabelsLastWord_WhenTheFieldIsNarrow(string browser) =>
        await AssertInfoAfterLastWordAsync(await OpenAsync(browser, "/field-info"), "The person we should call");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldPutTheInfoButtonAfterALongLabelsLastWord_WhenTheSliderIsNarrow(string browser) =>
        await AssertInfoAfterLastWordAsync(await OpenAsync(browser, "/field-info"), "How far the group can walk");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checkbox_ShouldKeepTheInfoButtonBesideALongLabel_WhenTheLabelWraps(string browser)
    {
        var page = await OpenAsync(browser, "/field-info");

        var label = await BoxAsync(page.Locator(".tb-checkbox__label").Filter(new() { HasText = "Carry a ranger's" }));
        var button = await BoxAsync(page.GetByRole(AriaRole.Button, new() { Name = "Information about Carry a" }));

        Assert.InRange(button.Y + button.Height / 2, label.Y, label.Y + label.Height);
        Assert.True(button.X >= label.X + label.Width - 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FileUpload_ShouldKeepItsChooseButtonsWidth_WhenGivenInfo(string browser)
    {
        var page = await OpenAsync(browser, "/field-info");

        var choose = await BoxAsync(page.Locator(".tb-file-upload__button"));
        var field = await BoxAsync(page.Locator(".tb-file-upload"));

        Assert.True(choose.Width < field.Width / 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checkbox_ShouldShowTheInfoButtonOnItsLabelsLine_WhenGivenInfo(string browser)
    {
        var page = await OpenAsync(browser, "/field-info");

        var label = await BoxAsync(page.GetByText("Guided hike", new() { Exact = true }));
        var button = await BoxAsync(page.GetByRole(AriaRole.Button, new() { Name = "Information about Guided hike" }));

        Assert.InRange(button.Y + button.Height / 2, label.Y, label.Y + label.Height);
        await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = "Guided hike", Exact = true })).ToBeVisibleAsync();
    }

    // The info button sits on the label text's last line, after its last word.
    private static async Task AssertInfoAfterLastWordAsync(IPage page, string labelStart)
    {
        var label = page.GetByText(labelStart, new() { Exact = false });
        await BoxAsync(label);
        var button =
            await BoxAsync(page.GetByRole(AriaRole.Button, new() { Name = $"Information about {labelStart}" }));
        var lastLine = await label.EvaluateAsync<double[]>(
            "e => { const text = document.createRange(); text.selectNodeContents(e); "
            + "const line = [...text.getClientRects()].at(-1); return [line.right, line.top, line.bottom]; }");

        Assert.InRange(button.Y + button.Height / 2, lastLine[1], lastLine[2]);
        Assert.True(button.X >= lastLine[0] - 1);
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        await Expect(locator).ToBeVisibleAsync();
        return (await locator.BoundingBoxAsync())!;
    }
}
