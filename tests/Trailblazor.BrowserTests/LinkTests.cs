namespace Trailblazor.BrowserTests;

public class LinkTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldStayUnderlinedAndShowAFocusRing_WhenFocused(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "theming guide" });

        // WebKit leaves links out of the Tab order by default, so the link is focused directly.
        await link.FocusAsync();

        await Expect(link).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(link).ToHaveCSSAsync("outline-style", "solid");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldRunItsAction_WhenItHasNoAddressAndEnterIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link");
        var action = page.GetByRole(AriaRole.Button, new() { Name = "Show the batch ID" });
        await Expect(action).ToHaveCSSAsync("text-decoration-line", "underline");

        await action.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByText("Batch 2041-B")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldSayItOpensANewTab_WhenNewTabIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link");

        var link = page.GetByRole(AriaRole.Link, new() { Name = "Microsoft Learn (opens in a new tab)" });

        await Expect(link).ToHaveAttributeAsync("target", "_blank");
        await Expect(link).ToHaveAttributeAsync("rel", "noopener noreferrer");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldTakeTheSystemLinkColorInBothKinds_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link", new() { ForcedColors = ForcedColors.Active });

        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: LinkText';
                const linkText = getComputedStyle(probe).color;
                const links = [...document.querySelectorAll('.tb-link')];
                return [linkText, ...links.map(link => getComputedStyle(link).color)];
            }
            """);

        Assert.All(colors.Skip(1), color => Assert.Equal(colors[0], color));
    }
}
