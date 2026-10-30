namespace Trailblazor.BrowserTests;

public class BadgeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The badge's center, and the button's top end corner, as x and y.
    private const string CornerScript = """
        host => {
            const badge = host.querySelector('[data-part=badge]').getBoundingClientRect();
            const button = host.querySelector('button').getBoundingClientRect();
            const end = getComputedStyle(host).direction === 'rtl' ? button.left : button.right;
            return [badge.left + badge.width / 2, badge.top + badge.height / 2, end, button.top];
        }
        """;

    private static ILocator Host(IPage page, string buttonName) => page.Locator(".tb-badge-host")
        .Filter(new() { Has = page.GetByRole(AriaRole.Button, new() { Name = buttonName }) });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Badge_ShouldStraddleTheButtonsTopEndCorner_WhenItSitsOnAButton(string browser)
    {
        var page = await OpenAsync(browser, "/examples/badge");

        var ltr = await Host(page, "Review changes").EvaluateAsync<double[]>(CornerScript);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        var rtl = await Host(page, "Review changes").EvaluateAsync<double[]>(CornerScript);
        // A left-to-right section inside a right-to-left page goes back to the right-hand corner.
        await Host(page, "Review changes").EvaluateAsync("host => host.parentElement.dir = 'ltr'");
        var nested = await Host(page, "Review changes").EvaluateAsync<double[]>(CornerScript);

        foreach (var corner in new[] { ltr, rtl, nested })
        {
            Assert.Equal(corner[2], corner[0], tolerance: 1);
            Assert.Equal(corner[3], corner[1], tolerance: 1);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Badge_ShouldLetClicksReachTheButton_WhenThePointerIsOnTheBadge(string browser)
    {
        var page = await OpenAsync(browser, "/examples/badge");

        var hit = await Host(page, "Review changes").EvaluateAsync<string>("""
            host => {
                const badge = host.querySelector('[data-part=badge]').getBoundingClientRect();
                const target = document.elementFromPoint(badge.left + badge.width / 4, badge.bottom - 2);
                return target.closest('button')?.textContent.trim() ?? target.tagName;
            }
            """);

        Assert.Equal("Review changes", hit);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Badge_ShouldHideAndLeaveTheButton_WhenItIsNoLongerVisible(string browser)
    {
        var page = await OpenAsync(browser, "/examples/badge");
        await Expect(Host(page, "Messages").Locator("[data-part=badge]")).ToHaveTextAsync("99+");

        await page.GetByRole(AriaRole.Button, new() { Name = "Mark all read" }).ClickAsync();

        await Expect(Host(page, "Messages").Locator("[data-part=badge]")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Messages", Exact = true }))
            .ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Badge_ShouldKeepItsOutlineAndDot_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/badge", new() { ForcedColors = ForcedColors.Active });

        var looks = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: CanvasText';
                const text = getComputedStyle(probe).color;
                return [...document.querySelectorAll('[data-part=badge]')].map(badge => {
                    const style = getComputedStyle(badge);
                    const dot = badge.classList.contains('tb-badge--dot');
                    return style.borderTopStyle === 'solid' && (!dot || style.backgroundColor === text) ? 'ok'
                        : `${style.borderTopStyle} ${style.backgroundColor}`;
                });
            }
            """);

        Assert.All(looks, look => Assert.Equal("ok", look));
    }
}
