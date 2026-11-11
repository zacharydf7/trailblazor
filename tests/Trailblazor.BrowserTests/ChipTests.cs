namespace Trailblazor.BrowserTests;

public class ChipTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Remove(IPage page, string tag) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Remove {tag}" });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chip_ShouldPassFocusToTheChipInItsPlace_WhenRemovedWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip");
        var tags = page.GetByRole(AriaRole.List, new() { Name = "Selected tags" });

        await Remove(page, "Archive").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Remove(page, "2026")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Delete");
        await Expect(Remove(page, "Invoices")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Backspace");
        await Expect(tags.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(0);
        await Expect(tags).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chip_ShouldKeepBackspaceFromGoingBack_WhenItCanBeRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip");
        await page.EvaluateAsync("""
            () => addEventListener('keydown', event => window.backspaceKept = event.defaultPrevented)
            """);

        await Remove(page, "Archive").FocusAsync();
        await page.Keyboard.PressAsync("Backspace");

        Assert.True(await page.EvaluateAsync<bool>("() => window.backspaceKept"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chip_ShouldBeFilledWithReadableText_WhenAnOutlinedFilterIsSelected(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip");
        var running = page.Locator(".tb-chip--selected").Filter(new() { HasText = "Running" });

        var colors = await page.EvaluateAsync<string[]>("""
            () => ['--tb-color-neutral', '--tb-color-on-neutral'].map(token => {
                const probe = document.querySelector('main').appendChild(document.createElement('div'));
                probe.style.color = `var(${token})`;
                return getComputedStyle(probe).color;
            })
            """);

        await Expect(running).ToHaveCSSAsync("background-color", colors[0]);
        await Expect(running).ToHaveCSSAsync("color", colors[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chip_ShouldKeepAnOutlineAndMarkTheSelectedOne_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip", new() { ForcedColors = ForcedColors.Active });

        var text = await SystemColorAsync(page, "CanvasText");

        foreach (var chip in await page.Locator(".tb-chip:not(.tb-chip--selected)").AllAsync())
        {
            await Expect(chip).ToHaveCSSAsync("border-top-color", text);
        }

        await Expect(page.Locator(".tb-chip--selected"))
            .ToHaveCSSAsync("border-top-color", await SystemColorAsync(page, "Highlight"));
    }
}
