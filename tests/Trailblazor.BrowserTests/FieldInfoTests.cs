namespace Trailblazor.BrowserTests;

public class FieldInfoTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Field_ShouldKeepTheInfoButtonBesideALongLabel_WhenTheFieldIsNarrow(string browser)
    {
        var page = await OpenAsync(browser, "/field-info");

        var label = await BoxAsync(page.GetByText("The person we should call", new() { Exact = false }));
        var button = await BoxAsync(page.GetByRole(AriaRole.Button, new() { Name = "Information about The person" }));

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

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        await Expect(locator).ToBeVisibleAsync();
        return (await locator.BoundingBoxAsync())!;
    }
}
