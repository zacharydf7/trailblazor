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

    // A field shows where to type by its box, so each text box, editable area, list, grid, drawing surface, drop zone
    // and in-place value in a field's example must sit in a border, its own or one on any of its four nearest
    // ancestors. A static page's file upload is left out: it shows the browser's own file input there.
    [Theory]
    [InlineData("text-field"), InlineData("number-field"), InlineData("code-field"), InlineData("mention-input")]
    [InlineData("rich-text-editor"), InlineData("markdown-editor"), InlineData("signature-pad")]
    [InlineData("tag-input"), InlineData("autocomplete")]
    [InlineData("cascader"), InlineData("file-upload"), InlineData("date-picker"), InlineData("time-picker")]
    [InlineData("date-time-picker"), InlineData("transfer-list"), InlineData("property-grid")]
    [InlineData("query-builder"), InlineData("in-place-editor")]
    public async Task Example_ShouldBoxEachField_WhenColorsAreForced(string example)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}", new() { ForcedColors = ForcedColors.Active });
        await Expect(page.Locator("main").Locator(Fields).Locator("visible=true").First).ToBeVisibleAsync();

        var unboxed = await page.EvaluateAsync<string[]>("""
            fields => {
                const bordered = element => ['Top', 'Right', 'Bottom', 'Left'].every(side => {
                    const style = getComputedStyle(element);
                    return style[`border${side}Style`] !== 'none' && parseFloat(style[`border${side}Width`]) > 0;
                });
                const boxed = field => {
                    for (let element = field, level = 0; element && level < 5; level++) {
                        if (bordered(element)) {
                            return true;
                        }

                        element = element.parentElement;
                    }

                    return false;
                };
                return [...document.querySelectorAll(`main :is(${fields})`)]
                    .filter(field => field.checkVisibility() && field.getBoundingClientRect().width > 1)
                    .filter(field => !boxed(field))
                    .map(field => `${field.localName}.${[...field.classList].join('.')}`);
            }
            """, Fields);

        Assert.Empty(unboxed);
    }

    private const string Fields = "input:not([type=checkbox], [type=radio], [type=range], [type=hidden], [type=file]), "
        + "textarea, [contenteditable=true], [role=combobox], [role=listbox], [role=tree], [role=grid], "
        + ".tb-signature-pad__surface, .tb-in-place-editor__display, .tb-file-upload__zone:not(:has(> [data-static]))";

    // Forced colors drop backgrounds and shadows, so a button's or a surface's edge has to be a border. A button keeps
    // the transparent border every variant has, which forced colors draw; a surface gets one only in forced colors.
    [Theory]
    [InlineData("button")]
    [InlineData("icon-button")]
    [InlineData("copy-button")]
    [InlineData("button-group")]
    [InlineData("floating-action-button")]
    [InlineData("back-to-top")]
    [InlineData("surface")]
    public async Task Example_ShouldDrawABorderAroundEachButtonAndSurface_WhenColorsAreForced(string example)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}", new() { ForcedColors = ForcedColors.Active });
        await Expect(page.Locator("main :is(.tb-button, .tb-surface)").First).ToBeVisibleAsync();

        var borderless = await page.EvaluateAsync<string[]>("""
            () => [...document.querySelectorAll('main :is(.tb-button, .tb-surface)')]
                .filter(element => element.checkVisibility())
                .filter(element => {
                    const style = getComputedStyle(element);
                    return style.borderTopStyle === 'none' || parseFloat(style.borderTopWidth) === 0
                        || style.borderTopColor === 'rgba(0, 0, 0, 0)';
                })
                .map(element => `${element.localName}.${[...element.classList].join('.')}`)
            """);

        Assert.Empty(borderless);
    }

    // A tinted message loses its tint in forced colors, so its edges have to be borders: every side of an alert, a
    // validation summary, a chat message and a prompt box, and the bottom of a banner, which spans the page.
    [Theory]
    [InlineData("alert", ".tb-alert", "top right bottom left")]
    [InlineData("banner", ".tb-banner", "bottom")]
    [InlineData("validation-summary", ".tb-validation-summary", "top right bottom left")]
    [InlineData("chat", ".tb-chat__message", "top right bottom left")]
    [InlineData("ai-prompt", ".tb-ai-prompt__input", "top right bottom left")]
    public async Task Example_ShouldBorderEachMessage_WhenColorsAreForced(string example, string selector, string sides)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}", new() { ForcedColors = ForcedColors.Active });
        await Expect(page.Locator($"main {selector}").First).ToBeVisibleAsync();

        var open = await page.EvaluateAsync<string[]>("""
            ([selector, sides]) => [...document.querySelectorAll(`main ${selector}`)]
                .filter(element => element.checkVisibility())
                .flatMap(element => sides.split(' ')
                    .filter(side => {
                        const style = getComputedStyle(element);
                        const name = side[0].toUpperCase() + side.slice(1);
                        return style[`border${name}Style`] === 'none' || parseFloat(style[`border${name}Width`]) === 0;
                    })
                    .map(side => `${element.className} ${side}`))
            """, new[] { selector, sides });

        Assert.Empty(open);
    }

    // The speed dial's list has no edge of its own, since each action floats by itself; the next test checks the
    // actions.
    // Back to top, the validation summary and the focus scope open nothing that floats, nor do a ribbon's key tips,
    // which label its own tabs, or its peeked commands, which drop below it across its width with a border under them.
    public static TheoryData<string, string> Popups => new(
        from example in ExampleCatalog.All
        where example.Name is not ("floating-action-button" or "back-to-top" or "validation-summary" or "focus-scope")
        from opening in AxeTests.OpeningsOf(example)
        let name = AxeTests.PopupName(opening)
        where !(example.Name == "ribbon" && name is "Rapids" or "Add a picture")
        select (example.Name, name));

    [Fact]
    public async Task SpeedDial_ShouldBorderEachAction_WhenColorsAreForced()
    {
        var page = await OpenAsync("chromium", "/examples/floating-action-button",
            new() { ForcedColors = ForcedColors.Active });
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Add" });
        var plus = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        await Expect(plus).ToHaveAttributeAsync("data-ready", "");

        await plus.ClickAsync();

        await Expect(menu).ToBeVisibleAsync();
        var parts = menu.Locator("[role=menuitem] > :is([data-part=icon], [data-part=label])");
        Assert.NotEqual(0, await parts.CountAsync());
        foreach (var part in await parts.AllAsync())
        {
            foreach (var side in new[] { "top", "right", "bottom", "left" })
            {
                await Expect(part).ToHaveCSSAsync($"border-{side}-style", "solid");
            }
        }
    }

    // Forced colors drop a popup's shadow and paint it the same color as the page, so its edge has to be a border:
    // on the popup, on what it shows (such as a toast in its region) or on any element between them. A control's own
    // border doesn't count, since a button in a borderless popup would pass on it.
    [Theory, MemberData(nameof(Popups))]
    public async Task Popup_ShouldHaveABorderOnEverySide_WhenColorsAreForced(string example, string popup)
    {
        var page = await OpenAsync("chromium", $"/examples/{example}", new() { ForcedColors = ForcedColors.Active });
        var opening = AxeTests.OpeningsOf(ExampleCatalog.Find(example)!)
            .Single(each => AxeTests.PopupName(each) == popup);

        await opening.OpenAsync(page);

        var shown = ExampleTargetTests.Find(page, opening.Shown);
        await Expect(shown).ToBeVisibleAsync();
        Assert.True(await shown.EvaluateAsync<bool>("""
            shown => {
                const popup = shown.closest('[popover], dialog, [role=dialog]');
                const bordered = element => ['Top', 'Right', 'Bottom', 'Left'].every(side => {
                    const style = getComputedStyle(element);
                    return style[`border${side}Style`] !== 'none' && parseFloat(style[`border${side}Width`]) > 0;
                });
                for (let element = shown; popup?.contains(element); element = element.parentElement) {
                    if (!element.matches('button, a, input, [role=tab]') && bordered(element)) {
                        return true;
                    }
                }

                return false;
            }
            """), $"The {popup} popup is missing a border on at least one side.");
    }

    // The ring's gap is what shows it turning, and forced colors would paint a transparent border.
    [Fact]
    public async Task Spinner_ShouldKeepItsGap_WhenColorsAreForced()
    {
        var page = await OpenAsync("chromium", "/button-busy", new() { ForcedColors = ForcedColors.Active });
        var send = page.GetByRole(AriaRole.Button, new() { Name = "Send code" });

        await send.ClickAsync();

        var spinner = send.Locator("[data-part=spinner]");
        await Expect(spinner).ToBeVisibleAsync();
        var gap = await spinner.EvaluateAsync<string>("ring => getComputedStyle(ring).borderTopColor");

        Assert.Equal("rgba(0, 0, 0, 0)", gap);
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
