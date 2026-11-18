using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class MentionInputTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Listbox(IPage page) => page.Locator(".tb-mention-input__listbox");

    private async Task<(IPage Page, ILocator Input)> OpenAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/mention-input");
        var input = page.Tb().MentionInput("Comment").Input;
        await Expect(input).ToHaveAttributeAsync("data-ready", "");
        await input.FocusAsync();
        return (page, input);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MentionInput_ShouldSuggestUnderTheTrigger_WhenItStartsAWord(string browser)
    {
        var (page, input) = await OpenAsync(browser);

        await page.Keyboard.TypeAsync("Thanks @an");

        await Expect(Listbox(page)).ToBeVisibleAsync();
        await Expect(Listbox(page).GetByRole(AriaRole.Option)).ToHaveTextAsync(["Ana LopezTrail crew", "Dan NgRanger"]);
        var caret = (await page.Locator("[data-part=caret]").BoundingBoxAsync())!;
        var list = (await Listbox(page).BoundingBoxAsync())!;
        Assert.InRange(list.X - caret.X, -1, 1);
        Assert.True(list.Y >= caret.Y + caret.Height, $"The list starts at {list.Y}, above the caret's bottom.");
        await input.FillAsync("mail@an");
        await Expect(Listbox(page)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MentionInput_ShouldChooseWithTheKeys_WhenSuggestionsShow(string browser)
    {
        var (page, input) = await OpenAsync(browser);
        await page.Keyboard.TypeAsync("@");
        var options = Listbox(page).GetByRole(AriaRole.Option);
        await Expect(options).ToHaveCountAsync(3);

        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", (await options.Nth(0).GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", (await options.Nth(2).GetAttributeAsync("id"))!);
        await page.Keyboard.PressAsync("Enter");
        await Expect(input).ToHaveValueAsync("@Priya Shah ");
        await page.Keyboard.TypeAsync("#pi");
        await Expect(Listbox(page).GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Tab");

        await Expect(input).ToHaveValueAsync("@Priya Shah #pine-hollow ");
        await Expect(input).ToBeFocusedAsync();
        await Expect(page.GetByText("Mentioned: @Priya Shah (u3), #pine-hollow (t1)")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MentionInput_ShouldStayClosedUntilTheTextChanges_WhenEscapeIsPressed(string browser)
    {
        var (page, _) = await OpenAsync(browser);
        await page.Keyboard.TypeAsync("@a");
        await Expect(Listbox(page)).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(Listbox(page)).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Listbox(page)).ToBeHiddenAsync();
        await page.Keyboard.TypeAsync("n");

        await Expect(Listbox(page)).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MentionInput_ShouldTakeTheMentionOut_WhenUndone(string browser)
    {
        var (page, input) = await OpenAsync(browser);
        await page.Keyboard.TypeAsync("@an");
        await Expect(Listbox(page).GetByRole(AriaRole.Option)).ToHaveCountAsync(2);
        await page.Keyboard.PressAsync("Enter");
        await Expect(input).ToHaveValueAsync("@Ana Lopez ");

        await page.Keyboard.PressAsync("ControlOrMeta+z");

        await Expect(input).ToHaveValueAsync("@an");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldTypeAndChooseAMention_WhenFoundByLabel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/mention-input");
        var field = page.Tb().MentionInput("Comment");

        await field.TypeAsync("Thanks @an");
        Assert.Equal(["Ana Lopez", "Dan Ng"], await field.SuggestionsAsync());
        await field.ChooseAsync("Dan Ng");

        Assert.Equal("Thanks @Dan Ng ", await field.TextAsync());
        await Expect(field.Input).ToBeFocusedAsync();
        await Expect(page.GetByText("Mentioned: @Dan Ng (u2)")).ToBeVisibleAsync();
    }
}
