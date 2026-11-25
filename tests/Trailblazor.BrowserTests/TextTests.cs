using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TextTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Pine = "Pine Hollow loop climbs through old cedar groves to a lookout over the river valley.";
    private const string Eagle = "Eagle Ridge traverse follows the crest for six miles, with steep drops on both "
        + "sides, two creek crossings and a long descent through switchbacks to the trailhead.";
    private const string Permit = "Permit TRL-2026-004173-N";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FullText_ShouldShowOnHoverAndFocusAndHideOnEscape_WhenTheTextIsCutOff(string browser)
    {
        var page = await OpenAsync(browser, "/text-full-text");
        var (pine, cedar) = (page.Tb().Text(Pine), page.Tb().Text("Cedar Flats"));
        var tooltip = pine.Element.Locator("[role=tooltip]");
        Assert.True(await pine.IsCutAsync());
        Assert.False(await cedar.IsCutAsync());

        await pine.Element.HoverAsync();

        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(tooltip).ToHaveTextAsync(Pine);
        // It wraps to fit its bubble, though the text it sits in keeps to one line.
        Assert.True(await tooltip.EvaluateAsync<bool>("tip => tip.scrollWidth <= tip.clientWidth"));
        await page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToBeHiddenAsync();
        // Only cut-off text is a tab stop, and its tooltip shows at once on keyboard focus.
        await page.Mouse.MoveAsync(0, 0);
        await page.GetByRole(AriaRole.Button, new() { Name = "After the texts" }).FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.Tb().Text(Permit).Element).ToBeFocusedAsync();
        await Expect(page.Tb().Text(Permit).Element.Locator("[role=tooltip]")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.Tb().Text(Eagle).Element).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(pine.Element).ToBeFocusedAsync();
        await Expect(tooltip).ToBeVisibleAsync();
        // Screen readers hear each text once, with no description that repeats it.
        await Expect(pine.Element).ToMatchAriaSnapshotAsync($"- paragraph: {Pine}");
        Assert.Null(await pine.Element.GetAttributeAsync("aria-describedby"));
        Assert.Empty((await page.RunAxe(AxeTests.WcagAA)).Violations);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FullText_ShouldComeAndGoWithTheCut_WhenTheBoxIsResized(string browser)
    {
        var page = await OpenAsync(browser, "/text-full-text");
        var pine = page.Tb().Text(Pine);
        Assert.True(await pine.IsCutAsync());

        await page.GetByTestId("box").EvaluateAsync("box => box.style.maxInlineSize = '80rem'");

        await Expect(pine.Element).Not.ToHaveAttributeAsync("data-cut", "");
        await Expect(pine.Element).Not.ToHaveAttributeAsync("tabindex", "0");
        await Expect(pine.Element.Locator("[role=tooltip]")).ToHaveCountAsync(0);
        Assert.False(await pine.IsCutAsync());
        await page.GetByTestId("box").EvaluateAsync("box => box.style.maxInlineSize = '16rem'");
        await Expect(pine.Element).ToHaveAttributeAsync("data-cut", "");
        await Expect(pine.Element).ToHaveAttributeAsync("tabindex", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FullText_ShouldComeWithTheCut_WhenTheTextGrowsInABoxThatKeepsItsSize(string browser)
    {
        var page = await OpenAsync(browser, "/text-full-text");
        Assert.False(await page.Tb().Text("Bring water").IsCutAsync());

        await page.GetByRole(AriaRole.Button, new() { Name = "Lengthen the note" }).ClickAsync();

        var note = page.Tb().Text("Bring water: the creek at the second crossing runs dry by midsummer most years.");
        await Expect(note.Element).ToHaveAttributeAsync("data-cut", "");
        await Expect(note.Element.Locator("[role=tooltip]")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FullText_ShouldShowItsTooltip_WhenRightToLeftTextIsCutOff(string browser)
    {
        const string forest = "שביל היער הישן עובר דרך חורשות ארזים עד תצפית על העמק";
        var page = await OpenAsync(browser, "/text-full-text");
        var text = page.Tb().Text(forest);
        Assert.True(await text.IsCutAsync());

        await text.Element.HoverAsync();

        await Expect(text.Element.Locator("[role=tooltip]")).ToBeVisibleAsync();
        await Expect(text.Element.Locator("[role=tooltip]")).ToHaveTextAsync(forest);
        // The text starts on the right, so the ellipsis cuts it off on the left.
        var (textBox, fullTextEdges) = ((await text.Element.BoundingBoxAsync())!,
            (await text.Element.Locator("[data-part=full-text]").EvaluateAsync<double[]>(
                "full => { const box = full.getClientRects()[0]; return [box.left, box.right]; }")));
        Assert.Equal(textBox.X + textBox.Width, fullTextEdges[1], 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MaxLength_ShouldShowTheShortenedTextAndReadTheWhole_WhenTheTextIsLonger(string browser)
    {
        var page = await OpenAsync(browser, "/text-full-text");
        var permit = page.Tb().Text(Permit);

        Assert.True(await permit.IsCutAsync());
        Assert.Equal(Permit, await permit.FullTextAsync());
        await Expect(permit.Element.Locator("[aria-hidden=true]").First).ToHaveTextAsync("Permit TRL-202…");
        await Expect(permit.Element).ToMatchAriaSnapshotAsync($"- paragraph: {Permit}");
        await permit.Element.HoverAsync();
        await Expect(permit.Element.Locator("[role=tooltip]")).ToHaveTextAsync(Permit);
        // Text that MaxLength cuts, with MaxLines but no ShowFullText, isn't measured, so this doesn't wait.
        Assert.True(await page.Tb().Text("Permit TRL-2026-004173-S").IsCutAsync());
        // Text written across lines in markup reads as one line.
        Assert.Equal(Eagle, await page.Tb().Text(Eagle).FullTextAsync());
    }

    // The lines the text shows, whether it holds more than it shows, and how it clips the rest.
    private static Task<string[]> FitAsync(IPage page, string testId) =>
        page.GetByTestId(testId).EvaluateAsync<string[]>("""
            text => {
                const style = getComputedStyle(text);
                return [String(Math.round(text.clientHeight / parseFloat(style.lineHeight))),
                    String(text.scrollWidth > text.clientWidth || text.scrollHeight > text.clientHeight),
                    style.overflow, style.textOverflow];
            }
            """);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldShowOnlyItsLinesWithAnEllipsis_WhenGivenMaxLines(string browser)
    {
        var page = await OpenAsync(browser, "/text-fit");

        Assert.Equal(["1", "true", "hidden", "ellipsis"], await FitAsync(page, "truncated"));
        Assert.Equal(["1", "true", "hidden", "ellipsis"], await FitAsync(page, "truncated-span"));
        Assert.Equal(["2", "true", "hidden"], (await FitAsync(page, "clamped"))[..3]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Text_ShouldUseTheMutedColor_WhenMuted(string browser)
    {
        var page = await OpenAsync(browser, "/text-fit");

        var colors = await page.GetByTestId("muted").EvaluateAsync<string[]>("""
            text => {
                const probe = text.parentElement.appendChild(document.createElement('span'));
                probe.style.color = 'var(--tb-color-text-muted)';
                return [getComputedStyle(text).color, getComputedStyle(probe).color, getComputedStyle(document.body).color];
            }
            """);

        Assert.Equal(colors[1], colors[0]);
        Assert.NotEqual(colors[2], colors[0]);
    }
}
