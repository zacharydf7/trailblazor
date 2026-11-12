using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Over a slow link the app hears each action late, so a helper that returns before the app has answered leaves the
// page as it was. Each test reads the page once, without retrying, right after the helper returns.
public class HelperWaitTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(TimeSpan.FromMilliseconds(400));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasTheValue_WhenEntered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");

        await page.Tb().NumberField("Quantity").EnterAsync("7");

        Assert.Equal(1, await page.GetByText("Quantity value: 7", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasEachValue_WhenItUpdatesAsTextIsTyped(string browser)
    {
        var page = await OpenAsync(browser, "/number-live");
        var copies = page.Tb().NumberField("Copies");

        await copies.EnterAsync("7");
        await copies.EnterAsync("8");

        Assert.Equal("8", await page.GetByTestId("copies").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenEntered(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");

        await page.Tb().RichTextEditor("Summary").EnterAsync("<p>Hi</p>");

        Assert.Equal("<p>Hi</p>", await page.GetByTestId("summary").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenTheEditorUpdatesAsTextIsTyped(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");
        var notes = page.Tb().RichTextEditor("Notes");

        await notes.EnterAsync("<p>Old</p>");
        await notes.EnterAsync("<p>New</p>");

        Assert.Equal("<p>New</p>", await page.GetByTestId("notes").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenTheAppTransformsThePaste(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");

        await page.Tb().RichTextEditor("Shouting").EnterAsync("<p>hey</p>");

        Assert.Equal("<p>HEY</p>", await page.GetByTestId("shouting").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RichTextEditor_ShouldReturnOnceTheAppHasTheValue_WhenItsHandlerStoresItLate(string browser)
    {
        var page = await OpenAsync(browser, "/rich-text-editor");

        await page.Tb().RichTextEditor("Live").EnterAsync("<p>Late</p>");

        Assert.Equal("<p>Late</p>", await page.GetByTestId("live").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldReturnOnceTheAppHasAnswered_WhenATagIsAddedRefusedOrRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tag-input");
        var features = page.Tb().TagInput("Trail features");

        await features.AddAsync("Views");
        Assert.Contains("Views", await features.Tags.AllTextContentsAsync(), StringComparer.Ordinal);
        await features.AddAsync("Views");
        Assert.Equal("true", await features.Input.GetAttributeAsync("aria-invalid"));
        await features.AddAsync(",");
        Assert.Null(await features.Input.GetAttributeAsync("aria-invalid"));
        await features.RemoveAsync("Views");

        Assert.DoesNotContain("Views", await features.Tags.AllTextContentsAsync(), StringComparer.Ordinal);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldReturnOnceTheAppHasTheTabs_WhenOneIsAddedOrClosed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tabs");
        var open = page.GetByRole(AriaRole.Tablist, new() { Name = "Open trails" }).GetByRole(AriaRole.Tab);
        var before = await open.CountAsync();
        var days = page.GetByRole(AriaRole.Tablist, new() { Name = "Trip days" }).GetByRole(AriaRole.Tab);
        var count = await days.CountAsync();

        await page.Tb().Tabs("Open trails").CloseAsync("Cedar flats");
        Assert.Equal(before - 1, await open.CountAsync());
        await page.Tb().Tabs("Trip days").AddAsync();

        Assert.Equal(count + 1, await days.CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldReturnOnceTheAppHasTheAnswer_WhenConfirmed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popconfirm");

        await page.Tb().Popconfirm("Delete trail").ConfirmAsync();

        Assert.Equal("Deleted.", await page.Locator("output").First.TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popconfirm_ShouldReturnOnceTheAppHasTheAnswer_WhenConfirmingRemovesItsTrigger(string browser)
    {
        var page = await OpenAsync(browser, "/popconfirm-remove");

        await page.Tb().Popconfirm("Delete Cedar Flats").ConfirmAsync();

        Assert.Equal(1, await page.Locator("li").CountAsync());
    }
}
