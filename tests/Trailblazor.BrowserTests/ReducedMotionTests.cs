using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

// With reduced motion, the motion tokens are 0ms and spinners stand still, so nothing on a docs example may move.
// AccessibilityStatusTests checks the stylesheet for the states a page shows only later, such as an open popup.
public class ReducedMotionTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    public static TheoryData<string, string> BrowsersAndExamples => new(
        from browser in AllBrowsers.Names from example in ExampleCatalog.All select (browser, example.Name));

    // An element's own transitions and animations, and those of its ::before and ::after, are read as written, so a
    // duration written without the motion tokens shows up even on a part that only moves on hover. An animation that
    // only fades, as an indeterminate progress bar's pulse does, isn't motion.
    [Theory, MemberData(nameof(BrowsersAndExamples))]
    public async Task Example_ShouldHaveNoMotion_WhenMotionIsReduced(string browser, string example)
    {
        var page = await OpenAsync(browser, $"/examples/{example}", new() { ReducedMotion = ReducedMotion.Reduce });
        await Expect(page.Locator("body > main, body main:not(main main)").First).ToBeVisibleAsync();

        var moving = await page.EvaluateAsync<string[]>("""
            () => {
                const fades = new Set();
                const visit = rules => {
                    for (const rule of rules) {
                        if (rule instanceof CSSKeyframesRule) {
                            const changed = [...rule.cssRules].flatMap(frame => [...frame.style]);
                            if (changed.every(property => property === 'opacity')) {
                                fades.add(rule.name);
                            }
                        } else if (rule.cssRules) {
                            visit(rule.cssRules);
                        }
                    }
                };
                for (const sheet of document.styleSheets) {
                    try { visit(sheet.cssRules); } catch { }
                }

                const longest = list => Math.max(...list.split(',').map(time =>
                    time.trim().endsWith('ms') ? parseFloat(time) : parseFloat(time) * 1000));
                const animates = style => style.animationName.split(',').map(name => name.trim())
                    .some(name => name !== 'none' && !fades.has(name));
                const moves = style => (style.transitionProperty !== 'none' && longest(style.transitionDuration) > 0)
                    || (animates(style) && longest(style.animationDuration) > 0);
                const found = [];
                for (const element of document.querySelectorAll('body *')) {
                    for (const pseudo of [null, '::before', '::after']) {
                        if (moves(getComputedStyle(element, pseudo))) {
                            found.push(`${element.localName}.${[...element.classList].join('.')}${pseudo ?? ''}`);
                        }
                    }
                }

                return [...new Set(found)];
            }
            """);

        Assert.Empty(moving);
    }
}
