using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

// Forced colors drop box shadows and repaint every color, so a focus ring has to be an outline to stay visible. Each
// docs example is tabbed through in chromium only, to keep a run over every example short.
public class ForcedColorsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Enough stops to reach every kind of control an example holds, without walking a long list one row at a time.
    private const int Stops = 15;

    public static TheoryData<string> Examples => new(ExampleCatalog.All.Select(example => example.Name));

    [Theory, MemberData(nameof(Examples))]
    public async Task Example_ShouldShowAFocusRingAtEachTabStop_WhenColorsAreForced(string example)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}", new() { ForcedColors = ForcedColors.Active });
        List<string> ringless = [];

        for (var stop = 0; stop < Stops; stop++)
        {
            await page.Keyboard.PressAsync("Tab");
            var focused = await page.EvaluateAsync<string?>(FocusedWithoutARing);
            if (focused == "")
            {
                break;
            }

            if (focused is not null && !ringless.Contains(focused))
            {
                ringless.Add(focused);
            }
        }

        Assert.Empty(ringless);
    }

    // Returns null when focus changes an outline, the focused element's name when it doesn't, and "" once focus has
    // left the example. The outline may be on the element, on one of its pseudo-elements (a separator draws its line
    // in ::before), or on an ancestor: a field rings the box that holds its input, and a card rings itself for its
    // link. The outlines are read with focus and again without it, since a selected item keeps an outline of its own.
    // A TbSlider rings its thumb, which getComputedStyle can't read, so SliderTests compares screenshots instead.
    private const string FocusedWithoutARing = """
        () => {
            const main = document.querySelector('main');
            const focused = document.activeElement;
            if (!focused || !main?.contains(focused)) {
                return '';
            }

            if (focused.matches('.tb-slider__input')) {
                return null;
            }

            const outlines = () => {
                const parts = [[focused, '::before'], [focused, '::after']];
                for (let element = focused; element && element !== main; element = element.parentElement) {
                    parts.push([element, null]);
                }

                return parts.map(([element, pseudo]) => {
                    const style = getComputedStyle(element, pseudo);
                    return style.outlineStyle === 'none' ? ''
                        : `${style.outlineStyle} ${style.outlineWidth} ${style.outlineColor} ${style.outlineOffset}`;
                }).join('|');
            };
            const withFocus = outlines();
            focused.blur();
            const withoutFocus = outlines();
            focused.focus();
            if (withFocus !== withoutFocus) {
                return null;
            }

            const name = focused.getAttribute('aria-label') ?? focused.textContent.trim().slice(0, 30);
            return `${focused.localName}${focused.dataset.part ? `[data-part=${focused.dataset.part}]` : ''} "${name}"`;
        }
        """;
}
