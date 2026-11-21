using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TextFieldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OnEnter_ShouldSeeTheTypedValue_WhenEnterIsPressedBeforeTheFieldLosesFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var stop = page.GetByLabel("Add a stop");

        await stop.FillAsync("Summit");
        await stop.PressAsync("Enter");

        await Expect(page.GetByText("Stops: Trailhead, Summit", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(stop).ToHaveValueAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OnEnter_ShouldSeeTheFittedText_WhenEnterIsPressedInAMaskedField(string browser)
    {
        var page = await OpenAsync(browser, "/text-field-enter");
        var phone = page.GetByLabel("Phone");
        await Expect(phone).ToHaveAttributeAsync("data-ready", "");

        await phone.PressSequentiallyAsync("5551234567");
        await phone.PressAsync("Enter");

        await Expect(page.GetByText("Calls: 1; last: 5551234567", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OnEnter_ShouldStaySilent_WhenEnterWasPressedOutsideTheField(string browser)
    {
        var page = await OpenAsync(browser, "/text-field-enter");
        var phone = page.GetByLabel("Phone");
        await page.Tb().TextField("Phone").EnterAsync("5551234567");
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit" }).FocusAsync();

        // The button's Enter moves focus to the field, where the key is released.
        await page.Keyboard.DownAsync("Enter");
        await Expect(phone).ToBeFocusedAsync();
        await page.Keyboard.UpAsync("Enter");
        // Events reach the app in order, so a stray call would be counted before this Enter's.
        await phone.PressAsync("Enter");

        await Expect(page.GetByText("Calls: 1; last: 5551234567", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldPostItsValue_WhenSubmittedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByLabel("Name").FillAsync("Ada");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Ada, False, Free");
        await Expect(page.GetByLabel("Name")).ToHaveValueAsync("Ada");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowItsValidationMessage_WhenAStaticFormIsSubmittedEmpty(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByText("Enter a name.")).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Name")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByTestId("posted")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepWhatWasTypedAndExplain_WhenAStaticFormPostsTextThatDoesNotConvert(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(age).ToHaveValueAsync("thirty");
        await Expect(age).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.Locator("[data-part=error]")).ToContainTextAsync("thirty");
        await Expect(page.GetByTestId("posted")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowTheSavedValue_WhenAPlainFormPostsTextThatDoesNotConvert(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted-plain");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("0");
        await Expect(age).ToHaveValueAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowTheNewModelsValue_WhenTheFormStartsOverAfterTextThatDoesNotConvert(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted-reset");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("state")).ToHaveTextAsync("started over");
        await Expect(age).ToHaveValueAsync("18");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldUpdateWhileTyping_WhenUpdateOnInputIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");

        await page.GetByLabel("Search").PressSequentiallyAsync("tea");

        await Expect(page.GetByText("Searching for: tea")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldEmptyAndKeepFocus_WhenTheClearButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var search = page.GetByLabel("Search");
        await search.PressSequentiallyAsync("tea");
        await Expect(page.GetByText("Searching for: tea")).ToBeVisibleAsync();

        var clear = page.GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true });
        await clear.ClickAsync();

        await Expect(page.GetByText("Not searching yet.")).ToBeVisibleAsync();
        await Expect(search).ToHaveValueAsync("");
        await Expect(search).ToBeFocusedAsync();
        await Expect(clear).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepFocusAndTheSelection_WhenTheShowPasswordButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var code = page.GetByLabel("Trail pass code");
        await Expect(code).ToHaveAttributeAsync("data-ready", "");
        await code.PressSequentiallyAsync("ridge-42");
        await code.EvaluateAsync("input => input.setSelectionRange(2, 5)");
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Show password" });

        foreach (var (type, pressed) in new[] { ("text", "true"), ("password", "false") })
        {
            await toggle.ClickAsync();

            await Expect(toggle).ToHaveAttributeAsync("aria-pressed", pressed);
            await Expect(code).ToHaveAttributeAsync("type", type);
            await Expect(code).ToBeFocusedAsync();
            var selection = await code.EvaluateAsync<int[]>("input => [input.selectionStart, input.selectionEnd]");
            Assert.Equal([2, 5], selection);
        }

        await Expect(code).ToHaveValueAsync("ridge-42");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextFieldLocator_ShouldShowAndHideThePassword_WhenAsked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var code = page.Tb().TextField("Trail pass code");

        await code.ShowPasswordAsync();
        var shown = await code.PasswordShownAsync();
        await code.HidePasswordAsync();

        Assert.True(shown);
        Assert.False(await code.PasswordShownAsync());
        await Expect(page.GetByLabel("Trail pass code")).ToHaveAttributeAsync("type", "password");
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            page.Tb().TextField("Password").ShowPasswordAsync);
        Assert.Contains("make it Revealable", refused.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextFieldLocator_ShouldShowAndHideThePassword_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-password");
        var password = page.Tb().TextField("Password");
        var button = page.Locator("[data-part=reveal]");
        var hiddenIcon = await button.Locator("svg").InnerHTMLAsync();

        await password.ShowPasswordAsync();
        await Expect(page.GetByLabel("Password", new() { Exact = true })).ToHaveAttributeAsync("type", "text");
        var shownIcon = await button.Locator("svg").InnerHTMLAsync();
        await password.HidePasswordAsync();

        await Expect(page.GetByLabel("Password", new() { Exact = true })).ToHaveAttributeAsync("type", "password");
        Assert.NotEqual(hiddenIcon, shownIcon);
        Assert.Equal(hiddenIcon, await button.Locator("svg").InnerHTMLAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldCountEachKeystrokeAndStopAtTheLimit_WhenItShowsACount(string browser)
    {
        var page = await OpenAsync(browser, "/text-count");
        var summary = page.GetByLabel("Summary");
        var count = page.Locator("[data-part=count]").First;
        await Expect(summary).ToHaveAttributeAsync("data-ready", "");

        await summary.PressSequentiallyAsync("Ridge");

        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("5 / 25");
        await Expect(count.Locator("[data-part=count-said]")).ToHaveTextAsync("5 of 25 characters");
        await Expect(page.GetByTestId("summary")).ToHaveTextAsync("");
        await summary.PressSequentiallyAsync(" loop past the old fire lookout");
        await Expect(summary).ToHaveValueAsync("Ridge loop past the old f");
        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("25 / 25");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldCountTheServersText_WhenTheServerChangesItAfterTyping(string browser)
    {
        var page = await OpenAsync(browser, "/text-count");
        var camp = page.Tb().TextField("Camp");
        var count = page.Locator("[data-part=count]").Nth(1);

        await camp.EnterAsync("Ridge");
        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("5 / 25");
        await page.GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true }).ClickAsync();

        await Expect(page.GetByLabel("Camp")).ToHaveValueAsync("");
        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("0 / 25");
        await Expect(count.Locator("[data-part=count-said]")).ToHaveTextAsync("0 of 25 characters");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldCountTheServersText_WhenTheAppSetsTextOfTheSameLengthWhileTyping(string browser)
    {
        var page = await OpenAsync(browser, "/text-count");
        var camp = page.GetByLabel("Camp");
        var count = page.Locator("[data-part=count]").Nth(1);
        await page.Tb().TextField("Camp").EnterAsync("Ridge");

        await camp.FocusAsync();
        await camp.PressAsync("End");
        await camp.PressSequentiallyAsync("s");
        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("6 / 25");
        // A click event that moves no focus, so the typed text isn't committed first.
        await page.GetByRole(AriaRole.Button, new() { Name = "Name the camp" }).DispatchEventAsync("click");

        await Expect(camp).ToHaveValueAsync("Camps");
        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("5 / 25");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldCountTheServersText_WhenACommitKeepsTheValue(string browser)
    {
        var page = await OpenAsync(browser, "/text-count");
        var size = page.GetByLabel("Group size");
        var count = page.Locator("[data-part=count]").Nth(2);
        await Expect(size).ToHaveAttributeAsync("data-ready", "");

        await size.PressAsync("Home");
        await size.PressSequentiallyAsync("0");
        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("2");
        await size.BlurAsync();

        await Expect(size).ToHaveValueAsync("5");
        await Expect(count.Locator("[data-part=count-shown]")).ToHaveTextAsync("1");
        await Expect(count.Locator("[data-part=count-said]")).ToHaveTextAsync("1 character");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldAnnounceTheCharactersLeft_WhenTheTextReachesAThreshold(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var summary = page.GetByLabel("Trail summary");
        await Expect(summary).ToHaveAttributeAsync("data-ready", "");
        var polite = page.Locator("[data-part=polite]");

        await summary.FillAsync(new string('a', 99));
        await summary.PressAsync("b");
        await Expect(page.Locator("[data-part=count-shown]").First).ToHaveTextAsync("100 / 120");
        await summary.PressAsync("b");
        await Expect(polite.GetByText("20 characters left.", new() { Exact = true })).ToHaveCountAsync(1);
        await summary.PressSequentiallyAsync(new string('c', 19));

        await Expect(polite.GetByText("0 characters left.", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(polite.GetByText("20 characters left.", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(page.Locator("[data-part=count-shown]").First).ToHaveTextAsync("120 / 120");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldSendOnlyTheTextTypedBeforeAPause_WhenGivenAnUpdateDelay(string browser)
    {
        var page = await OpenAsync(browser, "/delayed-search");

        await page.GetByLabel("Filter").PressSequentiallyAsync("ridge");

        await Expect(page.GetByTestId("filter-echo")).ToHaveTextAsync("ridge");
        await Expect(page.GetByTestId("filter-changes")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldUpdateAtOnce_WhenEnterOrLeavingTheFieldEndsTheDelay(string browser)
    {
        var page = await OpenAsync(browser, "/delayed-search");
        var search = page.GetByLabel("Search");

        await search.PressSequentiallyAsync("tea");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("tea");

        await search.PressSequentiallyAsync("pot");
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("teapot");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldGrowWithItsTextUpToMaxLinesThenScroll_WhenMaxLinesIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var notes = page.GetByLabel("Notes");
        // Playwright passes a float to the page as an empty object, so the height goes over as a double.
        double start = (await notes.BoundingBoxAsync())!.Height;

        await notes.FillAsync("1\n2\n3\n4\n5");
        await page.WaitForFunctionAsync(
            "start => document.querySelector('textarea[data-max-lines]').getBoundingClientRect().height > start",
            start);
        await notes.FillAsync(string.Join('\n', Enumerable.Range(1, 20)));
        await page.WaitForFunctionAsync(
            "() => (box => box.scrollHeight > box.clientHeight)(document.querySelector('textarea[data-max-lines]'))");

        var full = (await notes.BoundingBoxAsync())!.Height;
        Assert.InRange(full, start * 2, start * 8 / 3);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepEveryKeystroke_WhenEachUpdateIsSlowToComeBack(string browser)
    {
        // A lost keystroke depends on timing, so a few page loads make a regression fail reliably.
        const string text = "abcdefghijklmnopqrst";
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var page = await OpenAsync(browser, "/slow-typing");
            var search = page.GetByLabel("Search");

            await search.PressSequentiallyAsync(text);
            await Expect(page.GetByTestId("completed")).ToHaveTextAsync(text.Length.ToString());

            Assert.Equal(text, await search.InputValueAsync());
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepTheTextAndExplain_WhenTheTextDoesNotParse(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await age.PressAsync("Tab");

        await Expect(age).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(age).ToHaveValueAsync("thirty");
        await Expect(age).ToHaveAccessibleDescriptionAsync("Age isn't valid.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowTheParentsErrorUntilItClears_WhenTheParentSetsOne(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var handle = page.GetByLabel("Handle");

        await handle.PressSequentiallyAsync("robin");
        await page.GetByRole(AriaRole.Button, new() { Name = "Check the handle" }).ClickAsync();
        await Expect(handle).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(handle).ToBeFocusedAsync();
        await Expect(handle).ToHaveAccessibleDescriptionAsync(
            "Try robin, who already has an account. That handle is taken.");
        await Expect(page.Locator("[data-part=announcer] [role=status]")).ToContainTextAsync("That handle is taken.");

        await handle.PressSequentiallyAsync("2");

        await Expect(handle).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.Locator("[data-part=announcer]")).Not.ToContainTextAsync("That handle is taken.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldSizeItsBoxAndText_WhenGivenASize(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        string[] fields = ["Trail code", "Name", "Destination"];

        var heights = await Task.WhenAll(fields.Select(field => BoxStyleAsync(page, field, "height")));
        var fonts = await Task.WhenAll(fields.Select(field =>
            page.GetByLabel(field).EvaluateAsync<string>("input => getComputedStyle(input).fontSize")));

        Assert.Equal(["32px", "40px", "48px"], heights);
        Assert.Equal(["14px", "16px", "18px"], fonts);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShrinkEachSizeWithTheDensity_WhenTheThemeIsCompact(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        await page.EvaluateAsync("() => document.documentElement.dataset.tbDensity = 'compact'");

        string[] fields = ["Trail code", "Name", "Destination"];
        var heights = await Task.WhenAll(fields.Select(field => BoxStyleAsync(page, field, "height")));

        Assert.Equal(["26px", "32px", "40px"], heights);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldDrawATintedBoxWithOnlyItsBottomLine_WhenFilled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");

        Assert.Equal("rgba(0, 0, 0, 0)", await BoxStyleAsync(page, "Call sign", "borderTopColor"));
        Assert.Equal(await BoxStyleAsync(page, "Name", "borderBottomColor"),
            await BoxStyleAsync(page, "Call sign", "borderBottomColor"));
        Assert.Equal(await BoxStyleAsync(page, "Account", "backgroundColor"),
            await BoxStyleAsync(page, "Call sign", "backgroundColor"));
        Assert.NotEqual(await BoxStyleAsync(page, "Name", "backgroundColor"),
            await BoxStyleAsync(page, "Call sign", "backgroundColor"));
    }

    private static Task<string> BoxStyleAsync(IPage page, string label, string property) =>
        page.GetByLabel(label).EvaluateAsync<string>(
            "(input, property) => getComputedStyle(input.closest('.tb-field__control'))[property]", property);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldFitTypingToTheMaskAndBindTheDigits_WhenItHasAMask(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var phone = page.GetByLabel("Phone");
        await Expect(phone).ToHaveAttributeAsync("data-ready", "");

        await phone.PressSequentiallyAsync("5551234567");
        await Expect(phone).ToHaveValueAsync("(555) 123-4567");
        await phone.BlurAsync();

        await Expect(page.GetByText("Digits: 5551234567")).ToBeVisibleAsync();
    }

    [Theory, MemberData(nameof(CodeFieldTests.ComposingEnters), MemberType = typeof(CodeFieldTests))]
    public async Task TextField_ShouldCommitOnlyAfterTheComposition_WhenEnterEndsOneInAMaskedField(string browser,
        string composing)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var phone = page.GetByLabel("Phone");
        await Expect(phone).ToHaveAttributeAsync("data-ready", "");
        await phone.PressSequentiallyAsync("555");

        var changes = await phone.EvaluateAsync<int[]>($$"""
            input => {
                let changes = 0;
                input.addEventListener('change', () => changes++);
                input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, {{composing}} }));
                const composed = changes;
                input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
                return [composed, changes];
            }
            """);

        Assert.Equal([0, 1], changes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldCommitTheTextTheMaskRewrote_WhenEnterCommittedEarlierInTheSameFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var phone = page.GetByLabel("Phone");
        await Expect(phone).ToHaveAttributeAsync("data-ready", "");
        await phone.PressSequentiallyAsync("5551234567");
        await phone.PressAsync("Enter");
        await Expect(page.GetByText("Digits: 5551234567", new() { Exact = true })).ToBeVisibleAsync();

        // The mask rewrites the inserted digits as "(555) 12", which only a commit flags as short.
        await phone.SelectTextAsync();
        await page.Keyboard.InsertTextAsync("55512");
        await Expect(phone).ToHaveValueAsync("(555) 12");
        await phone.BlurAsync();

        await Expect(phone).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(phone).ToHaveAccessibleDescriptionAsync("Phone isn't complete.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Mask_ShouldCommitNothing_WhenItAttachesToAFocusedFieldThatIsLeftUnchanged(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        await Expect(page.GetByLabel("Phone")).ToHaveAttributeAsync("data-ready", "");

        var changes = await page.EvaluateAsync<int>("""
            async () => {
                const { createMask } = await import(new URL('_content/Trailblazor/text-mask.js', document.baseURI));
                const input = document.createElement('input');
                input.value = '(555) 12';
                document.body.append(input);
                input.focus();
                createMask(input, '(000) 000-0000');
                let changes = 0;
                input.addEventListener('change', () => changes++);
                input.blur();
                input.remove();
                return changes;
            }
            """);

        Assert.Equal(0, changes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepTheCaretInPlace_WhenADigitIsTypedOrDeletedMidMask(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var phone = page.GetByLabel("Phone");
        await Expect(phone).ToHaveAttributeAsync("data-ready", "");
        await phone.PressSequentiallyAsync("555123");

        await phone.EvaluateAsync("input => input.setSelectionRange(2, 2)");
        await page.Keyboard.TypeAsync("9");
        await Expect(phone).ToHaveValueAsync("(595) 512-3");
        Assert.Equal(3, await Caret(phone));
        // Backspace just after ") " takes the digit before the literal, not the space.
        await phone.EvaluateAsync("input => input.setSelectionRange(6, 6)");
        await page.Keyboard.PressAsync("Backspace");

        await Expect(phone).ToHaveValueAsync("(595) 123");
        Assert.Equal(3, await Caret(phone));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldFitPastedTextAndFlagAShortOne_WhenItHasAMask(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var phone = page.GetByLabel("Phone");
        await Expect(phone).ToHaveAttributeAsync("data-ready", "");

        await phone.FillAsync("555-123-4567");
        await Expect(phone).ToHaveValueAsync("(555) 123-4567");
        await phone.FillAsync("555 12");
        await phone.BlurAsync();

        await Expect(phone).ToHaveValueAsync("(555) 12");
        await Expect(phone).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(phone).ToHaveAccessibleDescriptionAsync("Phone isn't complete.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldStayReadyAndTakeAnyText_WhenItsMaskIsTakenAway(string browser)
    {
        var page = await OpenAsync(browser, "/text-mask");
        var code = page.GetByLabel("Code");
        await Expect(code).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Drop the mask" }).ClickAsync();
        // The old mask is disposed once the browser has shown the render, so a second round trip waits until it's gone.
        await Expect(code).Not.ToHaveAttributeAsync("placeholder", "___");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ping" }).ClickAsync();
        await Expect(page.GetByTestId("pings")).ToHaveTextAsync("1");
        await code.FillAsync("abc");

        await Expect(code).ToHaveValueAsync("abc");
        await Expect(code).ToHaveAttributeAsync("data-ready", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldStayReady_WhenItsMaskIsTakenAwayWhileItAttaches(string browser)
    {
        var (page, script) = await OpenWithAScriptHeldAsync(browser, "/text-mask", "**/text-mask.*js");
        var late = page.GetByLabel("Late");
        await page.GetByRole(AriaRole.Button, new() { Name = "Drop the late mask" }).ClickAsync();
        await Expect(late).ToHaveAttributeAsync("data-ready", "");

        script.SetResult();
        await Expect(page.GetByLabel("Code")).ToHaveAttributeAsync("data-ready", "");
        // Once the script loads, the late field's mask attaches along with the others and is then disposed; two round
        // trips wait for that.
        var ping = page.GetByRole(AriaRole.Button, new() { Name = "Ping" });
        await ping.ClickAsync();
        await ping.ClickAsync();
        await Expect(page.GetByTestId("pings")).ToHaveTextAsync("2");

        await Expect(late).ToHaveAttributeAsync("data-ready", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldFillSlotsWithCharactersLikeTheLiterals_WhenTheMaskHasDigitsOrEndsInOne(
        string browser)
    {
        var page = await OpenAsync(browser, "/text-mask");
        var phone = page.GetByLabel("UK phone");
        var area = page.GetByLabel("Area");
        await Expect(phone).ToHaveAttributeAsync("data-ready", "");

        await phone.PressSequentiallyAsync("4412");
        await Expect(phone).ToHaveValueAsync("+44 4412");
        await phone.FillAsync("+447700900123");
        await Expect(phone).ToHaveValueAsync("+44 7700 900123");
        await area.PressSequentiallyAsync("123");
        await Expect(area).ToHaveValueAsync("(123)");
        await area.BlurAsync();

        await Expect(page.GetByTestId("area")).ToHaveTextAsync("(123)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepItsNameAndShowInfoBesideTheLabel_WhenGivenInfo(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var info = page.GetByRole(AriaRole.Button, new() { Name = "Information about Backcountry permit" });

        var field = page.GetByRole(AriaRole.Textbox, new() { Name = "Backcountry permit", Exact = true });

        await Expect(field).ToBeVisibleAsync();
        var label = (await page.GetByText("Backcountry permit", new() { Exact = true }).BoundingBoxAsync())!;
        var button = (await info.BoundingBoxAsync())!;
        Assert.InRange(button.Y + button.Height / 2, label.Y, label.Y + label.Height);
        Assert.True(button.X > label.X + label.Width);

        await info.HoverAsync();
        await Expect(page.GetByText("The number on your park permit, such as PX-2041.")).ToBeVisibleAsync();
    }

    private static Task<int> Caret(ILocator input) => input.EvaluateAsync<int>("input => input.selectionStart");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Status_ShouldSpinOnlyWithoutReducedMotion_WhenTheFieldIsChecking(string browser)
    {
        var spins = new List<string>();
        foreach (var motion in new[] { ReducedMotion.NoPreference, ReducedMotion.Reduce })
        {
            var page = await OpenAsync(browser, "/field-status", new() { ReducedMotion = motion });
            var name = page.GetByLabel("Trail name");
            await Expect(name).ToHaveAttributeAsync("data-ready", "");
            await name.FillAsync("Robin");
            await name.BlurAsync();
            var spinner = page.Locator("[data-part=validation-status] .tb-field__spinner");
            await Expect(spinner).ToBeVisibleAsync();
            spins.Add(await spinner.EvaluateAsync<string>("spinner => getComputedStyle(spinner).animationName"));
        }

        Assert.Equal(["tb-spin", "none"], spins);
    }
}
