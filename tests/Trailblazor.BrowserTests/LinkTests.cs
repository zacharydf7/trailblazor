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
    public async Task Link_ShouldTakeTheSystemLinkColorInBothKinds_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link", new() { ForcedColors = ForcedColors.Active });

        var colors = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: LinkText';
                const linkText = getComputedStyle(probe).color;
                const links = [...document.querySelectorAll('.tb-link:not(:disabled, [aria-disabled="true"])')];
                return [linkText, ...links.map(link => getComputedStyle(link).color)];
            }
            """);

        Assert.All(colors.Skip(1), color => Assert.Equal(colors[0], color));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldTakeTheSystemGray_WhenDisabledAndColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link", new() { ForcedColors = ForcedColors.Active });

        var link = page.GetByRole(AriaRole.Link, new() { Name = "isn't published yet" });

        var colors = await link.EvaluateAsync<string[]>("""
            element => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = 'forced-color-adjust: none; color: GrayText';
                return [getComputedStyle(probe).color, getComputedStyle(element).color];
            }
            """);

        Assert.Equal(colors[0], colors[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldBeAnnouncedDisabledAndGoNowhere_WhenDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/link");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "isn't published yet" });

        await Expect(link).ToBeDisabledAsync();
        await link.ClickAsync(new() { Force = true });

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/examples/link$"));
        await Expect(link).ToHaveCSSAsync("text-decoration-line", "none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldNotRunItsAction_WhenDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/link");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "Run the report" });
        await Expect(link).ToBeDisabledAsync();
        // The focus button works only once the page is interactive, so the clicks below reach the server.
        await page.GetByRole(AriaRole.Button, new() { Name = "Focus the guides link" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Trail guides" })).ToBeFocusedAsync();

        await link.ClickAsync(new() { Force = true });
        await page.GetByRole(AriaRole.Button, new() { Name = "Check" }).ClickAsync();

        await Expect(page.GetByTestId("ran")).ToHaveTextAsync("checked");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusAsync_ShouldFocusTheLink_WhenCalledFromCode(string browser)
    {
        var page = await OpenAsync(browser, "/link");

        await page.GetByRole(AriaRole.Button, new() { Name = "Focus the guides link" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Trail guides" })).ToBeFocusedAsync();
    }
}
