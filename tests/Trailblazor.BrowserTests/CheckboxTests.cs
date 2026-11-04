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

    [Theory]
    [MemberData(nameof(FeedbackTests.BrowsersAndDirections), MemberType = typeof(FeedbackTests))]
    public async Task Switch_ShouldSlideItsThumbToTheTracksFarEdge_WhenOnAtEverySize(string browser, string direction)
    {
        var page = await OpenAsync(browser, "/examples/switch");
        await SetDirectionAsync(page, direction);

        var widths = new List<float>();
        foreach (var name in new[] { "Wi-Fi", "Auto-save", "Alarms" })
        {
            var track = page.GetByRole(AriaRole.Switch, new() { Name = name });
            var gap = await track.EvaluateAsync<double>("""
                track => {
                    const style = getComputedStyle(track);
                    const thumb = getComputedStyle(track, '::before');
                    const shift = Math.abs(new DOMMatrix(thumb.transform).m41);
                    const room = track.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
                    return room - parseFloat(thumb.width) - shift;
                }
                """);
            Assert.True(Math.Abs(gap) < 0.5, $"The {name} thumb stops {gap}px short of the far edge in {direction}.");
            widths.Add((await track.BoundingBoxAsync())!.Width);
        }

        Assert.True(widths[0] < widths[1] && widths[1] < widths[2], $"The tracks are {string.Join(", ", widths)}px.");
    }

    [Theory]
    [MemberData(nameof(FeedbackTests.BrowsersAndDirections), MemberType = typeof(FeedbackTests))]
    public async Task Switch_ShouldPutTheThumbAtTheEndOfTheReadingDirection_WhenOn(string browser, string direction)
    {
        var page = await OpenAsync(browser, "/examples/switch");
        await SetDirectionAsync(page, direction);
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
        foreach (var example in new[] { "checkbox", "switch", "radio-group", "checkbox-group" })
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
    public async Task Controls_ShouldShowTheStateTheServerHolds_WhenEachUpdateIsSlowToComeBack(string browser)
    {
        // A press made while an older render is on its way can be lost, but the control must still match its value.
        for (var load = 0; load < 4; load++)
        {
            var page = await OpenAsync(browser, "/slow-fields");
            var subscribe = page.GetByLabel("Subscribe");
            var notify = page.GetByRole(AriaRole.Switch, new() { Name = "Notify" });
            await PressAsync(page, subscribe, " ", 3);
            await PressAsync(page, notify, " ", 2);
            await PressAsync(page, page.GetByRole(AriaRole.Radio, new() { Name = "Small" }), "ArrowDown", 3);
            // Settle's render follows every change's, and renders arrive in order, so the last one has landed.
            await page.GetByRole(AriaRole.Button, new() { Name = "Settle" }).ClickAsync();
            await Expect(page.GetByTestId("settled")).ToHaveTextAsync("1");

            await Expect(subscribe).ToBeCheckedAsync(
                new() { Checked = await page.GetByTestId("subscribe").TextContentAsync() == "True" });
            await Expect(notify).ToBeCheckedAsync(
                new() { Checked = await page.GetByTestId("notify").TextContentAsync() == "True" });
            var size = await page.GetByTestId("size").TextContentAsync();
            await Expect(page.GetByRole(AriaRole.Radio, new() { Name = size })).ToBeCheckedAsync();
        }
    }

    // The thumb slides to its new side, so wait until no transition is still running.
    private static Task SetDirectionAsync(IPage page, string direction) => page.EvaluateAsync("""
        async direction => {
            document.documentElement.dir = direction;
            await new Promise(resolve => requestAnimationFrame(resolve));
            await Promise.all(document.getAnimations().map(animation => animation.finished));
        }
        """, direction);

    private static async Task PressAsync(IPage page, ILocator control, string key, int times)
    {
        await control.FocusAsync();
        for (var press = 0; press < times; press++)
        {
            await page.Keyboard.PressAsync(key);
        }
    }
}
