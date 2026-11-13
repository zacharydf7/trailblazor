namespace Trailblazor.BrowserTests;

// Components beside an older base stylesheet in a lower layer, which still reaches what they don't declare.
public class BaseStylesheetTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Components_ShouldKeepTheThemeFont_WhenABaseStylesheetSetsTheBodyFont(string browser)
    {
        var page = await OpenAsync(browser, "/base-stylesheet");
        // The test host's own stylesheet sets the body's font outside a layer, which would hide the base one's.
        await page.Locator("link[href*='host']").EvaluateAsync("link => link.disabled = true");

        // A button sets the theme font itself. Keycaps take the monospace one, and checkbox, radio and range inputs
        // show no text.
        var others = await page.Locator("#parts").EvaluateAsync<string[]>("""
            parts => {
                const theme = getComputedStyle(parts.querySelector('.tb-button')).fontFamily;
                const skipped = '.tb-kbd, svg, input:is([type=checkbox], [type=radio], [type=range])';
                return [...parts.querySelectorAll('*')]
                    .filter(element => !element.closest(skipped))
                    .filter(element => getComputedStyle(element).fontFamily !== theme)
                    .map(element => `${element.localName}.${element.className}`);
            }
            """);

        Assert.Empty(others);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task LabelsAndLegends_ShouldKeepTheirSpacingAndSize_WhenABaseStylesheetStylesThem(string browser)
    {
        var page = await OpenAsync(browser, "/base-stylesheet");

        // 8px is the base stylesheet's 0.5rem; some labels keep a margin of their own.
        var changed = await page.Locator("#parts").EvaluateAsync<string[]>("""
            parts => [...parts.querySelectorAll('label, legend')]
                .filter(element => {
                    const style = getComputedStyle(element);
                    return style.marginBottom === '8px' || (element.localName === 'legend'
                        && style.fontSize !== getComputedStyle(element.parentElement).fontSize);
                })
                .map(element => `${element.localName}.${element.className}`)
            """);

        Assert.Empty(changed);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kbd_ShouldDrawOnlyItsKeyCaps_WhenABaseStylesheetStylesKbd(string browser)
    {
        var page = await OpenAsync(browser, "/base-stylesheet");

        var combination = await page.Locator("#parts .tb-kbd").EvaluateAsync<string[]>("""
            kbd => {
                const style = getComputedStyle(kbd);
                const key = getComputedStyle(kbd.querySelector('.tb-kbd__key'));
                return [style.backgroundColor, style.paddingLeft, String(key.fontWeight === style.fontWeight)];
            }
            """);

        Assert.Equal(["rgba(0, 0, 0, 0)", "0px", "true"], combination);
    }

    // WebKit doesn't focus a button on click, so it has nothing to outline there.
    [Theory, InlineData("chromium"), InlineData("firefox")]
    public async Task Button_ShouldShowNoOutline_WhenClickedBesideABaseStylesheetFocusRule(string browser)
    {
        var page = await OpenAsync(browser, "/base-stylesheet");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await save.ClickAsync();

        await Expect(save).ToBeFocusedAsync();
        Assert.Equal("none", await save.EvaluateAsync<string>("button => getComputedStyle(button).outlineStyle"));
    }
}
