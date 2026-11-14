using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Over a slow link the app hears each action late, so a helper that returns before the app has answered leaves the
// page as it was. Each test reads the page once, without retrying, right after the helper returns.
public class HelperWaitTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override Task PreparePageAsync(IPage page) => page.UseSlowLinkAsync(TimeSpan.FromMilliseconds(400));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Choices_ShouldReturnOnceTheAppHasTheValue_WhenCheckedOrChosen(string browser)
    {
        var boxes = await OpenAsync(browser, "/examples/checkbox");
        var groups = await OpenAsync(browser, "/examples/checkbox-group");
        var radios = await OpenAsync(browser, "/examples/radio-group");

        await boxes.Tb().Checkbox("Email me updates").CheckAsync();
        await groups.Tb().CheckboxGroup("Trail days").CheckAsync("Monday");
        await radios.Tb().RadioGroup("Paper size").ChooseAsync("A4");

        Assert.Equal(1, await boxes.GetByText("Updates value: True", new() { Exact = true }).CountAsync());
        Assert.Equal(1,
            await groups.GetByText("Trail days value: Saturday, Monday", new() { Exact = true }).CountAsync());
        Assert.Equal(1, await radios.GetByText("Paper size value: A4", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasTheValue_WhenEntered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");

        await page.Tb().NumberField("Quantity").EnterAsync("7");

        Assert.Equal(1, await page.GetByText("Quantity value: 7", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldReturnOnceTheAppHasEachStepAndStopAtItsLimit_WhenStepped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var quantity = page.Tb().NumberField("Quantity");

        await quantity.StepAsync(3);
        var stepped = await page.GetByText("Quantity value: 4", new() { Exact = true }).CountAsync();
        await quantity.StepAsync(-10);

        Assert.Equal(1, stepped);
        Assert.Equal(1, await page.GetByText("Quantity value: 0", new() { Exact = true }).CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldThrowOnStepping_WhenTheFieldIsReadOnlyOrHidesItsButtons(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");

        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().NumberField("Permit limit").StepAsync(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().NumberField("Year").StepAsync(1));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DatePickers_ShouldReturnOnceTheFieldShowsItsError_WhenTextItCantReadIsEntered(string browser)
    {
        var dates = await OpenAsync(browser, "/date-picker");
        var times = await OpenAsync(browser, "/examples/date-time-picker");
        var ranges = await OpenAsync(browser, "/examples/date-range-picker");

        await dates.Tb().DatePicker("Start date").EnterAsync("not a date");
        await times.Tb().DateTimePicker("Guided hike starts").EnterAsync("soon");
        await ranges.Tb().DateRangePicker("Trip dates").EnterAsync(null, "someday");

        Assert.True(await dates.Tb().DatePicker("Start date").InvalidAsync());
        Assert.True(await times.Tb().DateTimePicker("Guided hike starts").InvalidAsync());
        Assert.True(await ranges.Tb().DateRangePicker("Trip dates").InvalidAsync());
        await Expect(times.Tb().DateTimePicker("Guided hike starts").DateInput).ToHaveValueAsync("soon");
        await Expect(ranges.GetByRole(AriaRole.Textbox, new() { Name = "Trip dates End date" }))
            .ToHaveValueAsync("someday");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DatePickers_ShouldThrowOnEntering_WhenTheFieldIsStaticReadOnlyOrDisabled(string browser)
    {
        var statics = await OpenStaticAsync(browser, "/static-date-picker");
        var staticTimes = await OpenStaticAsync(browser, "/static-date-time-picker");
        var states = await OpenAsync(browser, "/examples/date-picker");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => statics.Tb().DatePicker("Start date").EnterAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => staticTimes.Tb().DateTimePicker("Starts").EnterAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => states.Tb().DatePicker("Season opens").EnterAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => states.Tb().DatePicker("Last season opened").EnterAsync("x"));
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
    public async Task NumberField_ShouldReturnOnceTheAppHasTheValue_WhenTheAppReKeysTheField(string browser)
    {
        var page = await OpenAsync(browser, "/number-live");

        await page.Tb().NumberField("Trips").EnterAsync("7");

        Assert.Equal("7", await page.GetByTestId("trips").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldReturnOnceTheAppHasTheAnswer_WhenAButtonIsChosenOrTheDialogIsCanceled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete report" });

        await delete.ClickAsync();
        await page.Tb().Dialog("Delete report?").ChooseAsync("Delete");
        Assert.Equal(1, await page.GetByText("Deleted", new() { Exact = true }).CountAsync());
        await delete.ClickAsync();
        await page.Tb().Dialog("Delete report?").CancelAsync();

        Assert.Equal(1, await page.GetByText("Kept", new() { Exact = true }).CountAsync());
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
