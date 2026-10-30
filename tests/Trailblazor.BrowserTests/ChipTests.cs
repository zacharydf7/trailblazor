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
    public async Task Chip_ShouldTurnAFilterOnAndOff_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip");
        var queued = page.GetByRole(AriaRole.Button, new() { Name = "Queued" });
        var running = page.GetByRole(AriaRole.Button, new() { Name = "Running" });
        await Expect(running).ToHaveAttributeAsync("aria-pressed", "true");

        await queued.ClickAsync();
        await running.ClickAsync();

        await Expect(queued).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(running).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chip_ShouldBeFilledWithReadableText_WhenAnOutlinedFilterIsSelected(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chip");
        var running = page.Locator(".tb-chip--selected").Filter(new() { HasText = "Running" });
        // The tokens are read once, so first wait until the stylesheet is in place.
        await Expect(running).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

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

        var colors = await page.EvaluateAsync<string[]>("""
            () => ['CanvasText', 'Highlight'].map(color => {
                const probe = document.body.appendChild(document.createElement('div'));
                probe.style.cssText = `forced-color-adjust: none; color: ${color}`;
                return getComputedStyle(probe).color;
            })
            """);

        foreach (var chip in await page.Locator(".tb-chip:not(.tb-chip--selected)").AllAsync())
        {
            await Expect(chip).ToHaveCSSAsync("border-top-color", colors[0]);
        }

        await Expect(page.Locator(".tb-chip--selected")).ToHaveCSSAsync("border-top-color", colors[1]);
    }
}
