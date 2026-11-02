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
    public async Task Badge_ShouldSitOnTheChosenCorner_WhenPlacedAtTheTopStartOrOnACircle(string browser)
    {
        var page = await OpenAsync(browser, "/examples/badge");

        // The badge's center, then where it should be: 4px in from the content's top start corner, or the point where
        // a circle's edge meets the diagonal to its bottom end corner.
        const string StartScript = """
            host => {
                const badge = host.querySelector('[data-part=badge]').getBoundingClientRect();
                const content = host.firstElementChild.getBoundingClientRect();
                const start = getComputedStyle(host).direction === 'rtl' ? content.right - 4 : content.left + 4;
                return [badge.left + badge.width / 2, badge.top + badge.height / 2, start, content.top + 4];
            }
            """;
        var start = await page.GetByTestId("start").EvaluateAsync<double[]>(StartScript);
        var avatar = await page.GetByTestId("avatar").EvaluateAsync<double[]>("""
            host => {
                const badge = host.querySelector('[data-part=badge]').getBoundingClientRect();
                const circle = host.firstElementChild.getBoundingClientRect();
                const radius = circle.width / 2;
                return [badge.left + badge.width / 2, badge.top + badge.height / 2,
                    circle.left + radius + radius * Math.SQRT1_2, circle.top + radius + radius * Math.SQRT1_2];
            }
            """);

        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        var startRtl = await page.GetByTestId("start").EvaluateAsync<double[]>(StartScript);

        foreach (var corner in new[] { start, avatar, startRtl })
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

        var text = await SystemColorAsync(page, "CanvasText");

        foreach (var badge in await page.Locator("[data-part=badge]").AllAsync())
        {
            await Expect(badge).ToHaveCSSAsync("border-top-style", "solid");
        }

        await Expect(Host(page, "Filters, some applied").Locator(".tb-badge--dot"))
            .ToHaveCSSAsync("background-color", text);
    }
}
