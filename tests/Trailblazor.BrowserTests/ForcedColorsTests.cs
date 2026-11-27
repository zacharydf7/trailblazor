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

    // A disabled control shows in the system's GrayText, or is dimmed. Left out: a group, since its disabled children
    // show it; native checkboxes, radios and sliders, which forced colors bring back and the browser grays itself; and
    // a list's status message, a disabled option that carries information in muted text.
    [Theory, MemberData(nameof(Examples))]
    public async Task Example_ShouldShowEachDisabledControlAsDisabled_WhenColorsAreForced(string example)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}", new() { ForcedColors = ForcedColors.Active });

        var enabledLooking = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: GrayText';
                const gray = getComputedStyle(probe).color;
                probe.remove();
                const disabled = ':disabled, [aria-disabled="true"]';
                const dimmed = element => {
                    for (let each = element; each; each = each.parentElement) {
                        if (parseFloat(getComputedStyle(each).opacity) < 1) {
                            return true;
                        }
                    }

                    return false;
                };
                return [...document.querySelectorAll(`main :is(${disabled})`)]
                    .filter(element => element.checkVisibility() && !element.querySelector(disabled)
                        && !['checkbox', 'radio', 'range', 'hidden'].includes(element.type)
                        && element.dataset.part !== 'status')
                    .filter(element => getComputedStyle(element).color !== gray && !dimmed(element))
                    .map(element => `${element.localName} "${(element.getAttribute('aria-label')
                        ?? element.textContent).trim().slice(0, 30)}"`);
            }
            """);

        Assert.Empty(enabledLooking);
    }

    // An app's own palette sets the token outside the library's layers, where it would win over a rule inside them.
    [Fact]
    public async Task DisabledText_ShouldStayGrayText_WhenAnAppsOwnPaletteSetsIt()
    {
        Assert.SkipWhen(StrictPolicy, UsesInlineContent);
        var page = await OpenAsync("chromium", "/examples/tree-view", new() { ForcedColors = ForcedColors.Active });

        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const palette = document.head.appendChild(document.createElement('style'));
                palette.textContent = '[data-tb-palette="app"] { --tb-color-text-disabled: rgb(1, 2, 3); }';
                document.documentElement.dataset.tbPalette = 'app';
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: GrayText';
                const row = document.querySelector('main .tb-tree__item[aria-disabled="true"]');
                return [getComputedStyle(probe).color, getComputedStyle(row).color];
            }
            """);

        Assert.Equal(colors[0], colors[1]);
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
