using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class InPlaceEditorTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InPlaceEditor_ShouldEditWithTheKeyboard_WhenEnterAndEscapeArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/in-place-editor");
        var name = page.Tb().InPlaceEditor("Trail name");
        await Expect(name.InPlaceEditor).ToHaveAttributeAsync("data-ready", "");
        var display = name.InPlaceEditor.Locator("[data-part=display]");
        var input = name.InPlaceEditor.GetByRole(AriaRole.Textbox, new() { Name = "Trail name" });
        // A dialog around the editor closes on an Escape that reaches the page unused; this listener notes one.
        await page.EvaluateAsync("() => document.addEventListener('keydown', event => "
            + "window.escapeLeaked ||= event.key === 'Escape' && !event.defaultPrevented)");

        await display.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(input).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("Ridge loop");
        await page.Keyboard.PressAsync("Enter");
        await Expect(name.InPlaceEditor).Not.ToHaveAttributeAsync("data-editing", "");
        await Expect(display).ToBeFocusedAsync();
        Assert.Equal("Ridge loop", await name.TextAsync());

        await page.Keyboard.PressAsync("Space");
        await Expect(input).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("Eagle");
        await page.Keyboard.PressAsync("Escape");
        Assert.False(await page.EvaluateAsync<bool>("() => window.escapeLeaked === true"));
        await Expect(display).ToBeFocusedAsync();
        Assert.Equal("Ridge loop", await name.TextAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InPlaceEditor_ShouldKeepEnterFromTheFormAroundIt_WhenSaving(string browser)
    {
        var page = await OpenAsync(browser, "/in-place-editor-form");
        var name = page.Tb().InPlaceEditor("Trail name");

        await name.EditAsync();
        await page.Keyboard.TypeAsync("Ridge loop");
        await page.Keyboard.PressAsync("Enter");

        await Expect(name.InPlaceEditor).Not.ToHaveAttributeAsync("data-editing", "");
        Assert.Equal("Ridge loop", await name.TextAsync());
        await Expect(page.GetByTestId("submits")).ToHaveTextAsync("0");
    }
}
