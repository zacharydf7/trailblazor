using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class LocatedFieldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string NoTimeBox = "WebKit on Windows has no time box.";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldWorkTheFieldTheyAreGiven_WhenTheAppDrawsTheLabelItself(string browser)
    {
        var page = await OpenAsync(browser, "/drawn-labels");
        var tb = page.Tb();

        await tb.Slider(page.Locator("#pace")).SetAsync(40);
        await tb.RangeSlider(page.Locator("#elevation")).SetAsync(20, 60);
        await tb.DatePicker(page.Locator("#departure")).ChooseAsync(new DateOnly(2026, 5, 2));
        await tb.Checkbox(page.Locator("#terms")).CheckAsync();
        await tb.Switch(page.Locator("#alerts")).CheckAsync();
        await tb.CheckboxGroup(page.Locator("#gear")).CheckAsync("Rope");
        await tb.RadioGroup(page.Locator("#route")).ChooseAsync("Ridge");
        await tb.Rating(page.Locator("#effort")).RateAsync(3);
        await tb.ToggleGroup(page.Locator("#view")).ChooseAsync("Map");
        await tb.TagInput(page.Locator("#tags")).AddAsync("Shade");
        await tb.RichTextEditor(page.Locator("#notes")).EnterAsync("<p>Bring water</p>");

        Assert.Equal(40, await tb.Slider(page.Locator("#pace")).ValueAsync());
        Assert.Equal((20m, 60m), await tb.RangeSlider(page.Locator("#elevation")).RangeAsync());
        Assert.Equal(new DateOnly(2026, 5, 2), await tb.DatePicker(page.Locator("#departure")).ValueAsync());
        Assert.True(await tb.Checkbox(page.Locator("#terms")).CheckedAsync());
        Assert.True(await tb.Switch(page.Locator("#alerts")).CheckedAsync());
        Assert.Equal(["Rope"], await tb.CheckboxGroup(page.Locator("#gear")).CheckedAsync());
        Assert.Equal("Ridge", await tb.RadioGroup(page.Locator("#route")).ChosenAsync());
        Assert.Equal(3, await tb.Rating(page.Locator("#effort")).ValueAsync());
        Assert.Equal(["Map"], await tb.ToggleGroup(page.Locator("#view")).ChosenAsync());
        Assert.Equal(["Shade"], await tb.TagInput(page.Locator("#tags")).TagsAsync());
        await Expect(tb.RichTextEditor(page.Locator("#notes")).Input).ToHaveTextAsync("Bring water");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldLookOnlyInsideTheScope_WhenGivenALocatorThatMatchesMore(string browser)
    {
        var page = await OpenAsync(browser, "/drawn-labels");

        // Every checkbox on the page matches, but only one is inside the section.
        await page.GetByTestId("later").Tb().Checkbox(page.GetByRole(AriaRole.Checkbox)).CheckAsync();

        Assert.True(await page.Tb().Checkbox(page.Locator("#remember")).CheckedAsync());
        Assert.False(await page.Tb().Checkbox(page.Locator("#terms")).CheckedAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TimePicker_ShouldWorkTheFieldItIsGiven_WhenTheAppDrawsTheLabelItself(string browser)
    {
        Assert.SkipWhen(browser == "webkit", NoTimeBox);
        var page = await OpenAsync(browser, "/drawn-labels");
        var start = page.Tb().TimePicker(page.Locator("#start"));

        await start.ChooseAsync(new TimeOnly(9, 30));

        Assert.Equal(new TimeOnly(9, 30), await start.ValueAsync());
    }
}
