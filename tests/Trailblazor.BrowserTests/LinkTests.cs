using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class LinkTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // A mark on the window survives Blazor's in-place navigation and is gone after a whole new page loads.
    public static TheoryData<string, string, bool> Navigations()
    {
        var data = new TheoryData<string, string, bool>();
        foreach (var browser in AllBrowsers.Names)
        {
            data.Add(browser, "/link", false);
            data.Add(browser, "/link", true);
            data.Add(browser, "/static-link", false);
            data.Add(browser, "/static-link", true);
        }

        return data;
    }

    [Theory, MemberData(nameof(Navigations))]
    public async Task Link_ShouldLoadAWholeNewPageOnlyWithForceLoad_WhenFollowed(string browser, string path,
        bool forceLoad)
    {
        var page = path == "/link" ? await OpenAsync(browser, path) : await OpenStaticAsync(browser, path);
        await page.EvaluateAsync("() => window.tbMark = true");
        var name = (path, forceLoad) switch
        {
            ("/link", false) => "Open dialogs",
            ("/link", true) => "Reload into dialogs",
            (_, false) => "Read-only fields",
            _ => "Reload into read-only fields",
        };

        await page.GetByRole(AriaRole.Link, new() { Name = name, Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex(path == "/link" ? "/dialogs$" : "/static-read-only$"));
        Assert.Equal(!forceLoad, await page.EvaluateAsync<bool>("() => window.tbMark === true"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Link_ShouldStillOpenANewTab_WhenItHasForceLoadAndNewTab(string browser)
    {
        var page = await OpenAsync(browser, "/link");
        await page.EvaluateAsync("() => window.tbMark = true");

        var tab = await page.Context.RunAndWaitForPageAsync(() =>
            page.GetByRole(AriaRole.Link, new() { Name = "Reload into dialogs in a new tab" }).ClickAsync());

        await Expect(tab).ToHaveURLAsync(new Regex("/dialogs$"));
        Assert.True(await page.EvaluateAsync<bool>("() => window.tbMark === true"));
    }

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
