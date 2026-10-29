namespace Trailblazor.BrowserTests;

public class CheckboxTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checkbox_ShouldToggle_WhenSpaceIsPressedOrTheLabelIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox");
        var updates = page.GetByRole(AriaRole.Checkbox, new() { Name = "Email me updates" });

        await updates.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(page.GetByTestId("updates-echo")).ToHaveTextAsync("True");
        await page.GetByText("Email me updates").ClickAsync();

        await Expect(page.GetByTestId("updates-echo")).ToHaveTextAsync("False");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checkbox_ShouldBeMixedThenChecked_WhenANullValueIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox");
        var allFormats = page.GetByRole(AriaRole.Checkbox, new() { Name = "All formats" });
        await Expect(allFormats).ToBeCheckedAsync(new() { Indeterminate = true });

        await allFormats.ClickAsync();

        await Expect(page.GetByTestId("all-formats-echo")).ToHaveTextAsync("True");
        await Expect(allFormats).ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Switch_ShouldBeASwitchThatToggles_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/switch");
        var autoSave = page.GetByRole(AriaRole.Switch, new() { Name = "Auto-save" });
        await Expect(autoSave).ToBeCheckedAsync();

        await autoSave.ClickAsync();

        await Expect(page.GetByTestId("auto-save-echo")).ToHaveTextAsync("False");
        await Expect(autoSave).Not.ToBeCheckedAsync();
    }

    [Theory]
    [MemberData(nameof(FeedbackTests.BrowsersAndDirections), MemberType = typeof(FeedbackTests))]
    public async Task Switch_ShouldPutTheThumbAtTheEndOfTheReadingDirection_WhenOn(string browser, string direction)
    {
        var page = await OpenAsync(browser, "/examples/switch");
        // The thumb slides to its new side, so wait until no transition is still running.
        await page.EvaluateAsync("""
            async direction => {
                document.documentElement.dir = direction;
                await new Promise(resolve => requestAnimationFrame(resolve));
                await Promise.all(document.getAnimations().map(animation => animation.finished));
            }
            """, direction);
        var autoSave = page.GetByRole(AriaRole.Switch, new() { Name = "Auto-save" });

        var shift = await autoSave.EvaluateAsync<double>(
            "track => new DOMMatrix(getComputedStyle(track, '::before').transform).m41");

        Assert.True(direction == "rtl" ? shift < 0 : shift > 0, $"The thumb moved {shift}px in {direction}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Checkbox_ShouldPostItsValue_WhenCheckedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByLabel("Name").FillAsync("Ada");
        await page.GetByLabel("Subscribe").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Ada, True, Free");
        await Expect(page.GetByLabel("Subscribe")).ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Controls_ShouldLeaveOnlyTheNativeMarks_WhenColorsAreForced(string browser)
    {
        foreach (var example in new[] { "checkbox", "switch", "radio-group" })
        {
            var page = await OpenAsync(browser, $"/examples/{example}", new() { ForcedColors = ForcedColors.Active });
            if (example == "checkbox")
            {
                // The mixed mark appears only once the checkbox script sets it.
                await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = "All formats" }))
                    .ToBeCheckedAsync(new() { Indeterminate = true });
            }

            var drawn = await page.EvaluateAsync<string[]>("""
                () => [...document.querySelectorAll('input')]
                    .filter(input => getComputedStyle(input, '::before').content !== 'none')
                    .map(input => input.labels[0]?.textContent.trim() ?? input.outerHTML)
                """);

            Assert.Empty(drawn);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Controls_ShouldEndInTheLastChosenState_WhenEachUpdateIsSlowToComeBack(string browser)
    {
        // A smoke test: key presses over a few loads make a lost change likely to show (see #91).
        for (var load = 0; load < 4; load++)
        {
            var page = await OpenAsync(browser, "/slow-fields");
            await PressAsync(page, page.GetByLabel("Subscribe"), " ", 3);
            await PressAsync(page, page.GetByRole(AriaRole.Switch, new() { Name = "Notify" }), " ", 2);
            await PressAsync(page, page.GetByRole(AriaRole.Radio, new() { Name = "Small" }), "ArrowDown", 3);

            await Expect(page.GetByTestId("subscribe")).ToHaveTextAsync("True");
            await Expect(page.GetByTestId("notify")).ToHaveTextAsync("False");
            await Expect(page.GetByTestId("size")).ToHaveTextAsync("Huge");
            await Expect(page.GetByLabel("Subscribe")).ToBeCheckedAsync();
            await Expect(page.GetByRole(AriaRole.Switch, new() { Name = "Notify" })).Not.ToBeCheckedAsync();
            await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Huge" })).ToBeCheckedAsync();
        }
    }

    private static async Task PressAsync(IPage page, ILocator control, string key, int times)
    {
        await control.FocusAsync();
        for (var press = 0; press < times; press++)
        {
            await page.Keyboard.PressAsync(key);
        }
    }
}
