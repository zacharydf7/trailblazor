using System.Globalization;
using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

// Keyboard tests and shared helpers for SliderTests; the slow-link class reruns only these.
public abstract class SliderKeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected static ILocator Slider(IPage page, string name) =>
        page.GetByRole(AriaRole.Slider, new() { Name = name, Exact = true });

    private static async Task ExpectLeftToRaiseAsync(IPage page, ILocator input)
    {
        var value = decimal.Parse(await input.InputValueAsync(), CultureInfo.InvariantCulture);
        var step = decimal.Parse((await input.GetAttributeAsync("step"))!, CultureInfo.InvariantCulture);
        // The value text comes from the server, so it shows the slider's value changed, not only the input's.
        Task ExpectAsync(decimal expected) => Expect(input).ToHaveAttributeAsync("aria-valuetext",
            new Regex($"^{expected.ToString(CultureInfo.InvariantCulture)}\\b"));
        await input.FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");
        await ExpectAsync(value + step);
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");

        await ExpectAsync(value - step);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldStepJumpAndReachTheEnds_WhenTheKeysAreUsed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var brightness = Slider(page, "Map brightness");
        await brightness.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(brightness).ToHaveValueAsync("61");
        await page.Keyboard.PressAsync("Home");
        await Expect(brightness).ToHaveValueAsync("0");
        await page.Keyboard.PressAsync("End");

        await Expect(brightness).ToHaveValueAsync("100");
        await Expect(brightness).ToHaveAttributeAsync("aria-valuetext", "100%");
        await Expect(page.GetByText("Saved at")).ToHaveTextAsync("Saved at 100%");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldMoveEachThumbAndStopItAtTheOther_WhenUsedWithTheKeys(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var minimum = Slider(page, "Elevation Minimum");
        var maximum = Slider(page, "Elevation Maximum");

        await minimum.FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(minimum).ToHaveValueAsync("2400");
        await page.Keyboard.PressAsync("Tab");
        await Expect(maximum).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");

        await Expect(maximum).ToHaveValueAsync("2400");
        await Expect(page.GetByText("Between")).ToHaveTextAsync("Between 2400 and 2400 m");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldRaiseTheValueWithTheLeftArrow_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");

        await ExpectLeftToRaiseAsync(page, Slider(page, "Trail length"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RangeSlider_ShouldRaiseAThumbWithTheLeftArrow_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/range-slider");

        await ExpectLeftToRaiseAsync(page, Slider(page, "Flipped Minimum"));
    }
}
