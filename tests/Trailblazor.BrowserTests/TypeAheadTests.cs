using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TypeAheadTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // A stopped clock keeps a slow machine's keys within the pause that ends a word.
    private async Task<IPage> OpenStoppedAsync(string browser)
    {
        var page = await OpenAsync(browser, "/type-ahead");
        await page.Clock.SetFixedTimeAsync(DateTime.Now);
        return page;
    }

    private async Task<IPage> OpenListBoxAsync(string browser)
    {
        var page = await OpenStoppedAsync(browser);
        var list = page.GetByRole(AriaRole.Listbox, new() { Name = "Snack", Exact = true });
        await Expect(list).ToHaveAttributeAsync("data-ready", "");
        await list.FocusAsync();
        return page;
    }

    private async Task<IPage> OpenTreeAsync(string browser)
    {
        var page = await OpenStoppedAsync(browser);
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Pantry" });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await Row(page, "Anna").FocusAsync();
        return page;
    }

    private async Task<IPage> OpenSelectAsync(string browser)
    {
        var page = await OpenStoppedAsync(browser);
        var select = page.GetByRole(AriaRole.Combobox, new() { Name = "Food" });
        await Expect(select).ToHaveAttributeAsync("data-ready", "");
        await select.FocusAsync();
        return page;
    }

    private static ILocator Row(IPage page, string text) => page.Locator($"[role=treeitem][data-text='{text}']");

    private static async Task ExpectActiveOptionAsync(IPage page, string name)
    {
        var option = page.GetByRole(AriaRole.Listbox, new() { Name = "Food" })
            .GetByRole(AriaRole.Option, new() { Name = name, Exact = true });
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Food" }))
            .ToHaveAttributeAsync("aria-activedescendant", (await option.GetAttributeAsync("id"))!);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldStepThroughTheOptionsStartingWithALetter_WhenItIsPressedQuickly(string browser)
    {
        var page = await OpenListBoxAsync(browser);
        var snack = page.GetByTestId("snack");

        await page.Keyboard.PressAsync("b");
        await Expect(snack).ToHaveTextAsync("Banana");
        await page.Keyboard.PressAsync("b");
        await Expect(snack).ToHaveTextAsync("Bread");
        await page.Keyboard.PressAsync("b");

        await Expect(snack).ToHaveTextAsync("Banana");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldFindTheOptionWithADoubledLetter_WhenOneStartsWithIt(string browser)
    {
        var page = await OpenListBoxAsync(browser);

        await page.Keyboard.TypeAsync("aa");

        await Expect(page.GetByTestId("snack")).ToHaveTextAsync("Aaron");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldStepPastTheOptionWithADoubledLetter_WhenItIsAlreadyOnIt(string browser)
    {
        var page = await OpenListBoxAsync(browser);
        var snack = page.GetByTestId("snack");
        await page.Keyboard.TypeAsync("al");
        await Expect(snack).ToHaveTextAsync("Alan");
        await page.Clock.SetFixedTimeAsync(DateTime.Now.AddSeconds(5));
        await page.Keyboard.PressAsync("a");
        await Expect(snack).ToHaveTextAsync("Aaron");

        await page.Keyboard.PressAsync("a");

        await Expect(snack).ToHaveTextAsync("Anna");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldFindTheOptionWithADoubledLetter_WhenTypingGoesOnFromIt(string browser)
    {
        var page = await OpenListBoxAsync(browser);
        var snack = page.GetByTestId("snack");
        await page.Keyboard.TypeAsync("al");
        await Expect(snack).ToHaveTextAsync("Alan");
        await page.Clock.SetFixedTimeAsync(DateTime.Now.AddSeconds(5));

        await page.Keyboard.TypeAsync("aar");

        await Expect(snack).ToHaveTextAsync("Aaron");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldStayOnTheOption_WhenDifferentLettersMatchNothing(string browser)
    {
        var page = await OpenListBoxAsync(browser);
        var snack = page.GetByTestId("snack");
        await page.Keyboard.PressAsync("b");
        await Expect(snack).ToHaveTextAsync("Banana");

        await page.Keyboard.PressAsync("x");
        // Up from Banana reaches Aaron, while up from Bread would only come back to Banana.
        await page.Keyboard.PressAsync("ArrowUp");

        await Expect(snack).ToHaveTextAsync("Aaron");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldStepThroughTheOptionsStartingWithALetter_WhenItIsPressedQuickly(string browser)
    {
        var page = await OpenSelectAsync(browser);

        await page.Keyboard.PressAsync("b");
        await ExpectActiveOptionAsync(page, "Banana");
        await page.Keyboard.PressAsync("b");
        await ExpectActiveOptionAsync(page, "Bread");
        await page.Keyboard.PressAsync("b");

        await ExpectActiveOptionAsync(page, "Banana");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldFindTheOptionWithADoubledLetter_WhenOneStartsWithIt(string browser)
    {
        var page = await OpenSelectAsync(browser);

        await page.Keyboard.TypeAsync("aa");

        await ExpectActiveOptionAsync(page, "Aaron");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldStepThroughTheRowsStartingWithALetter_WhenItIsPressedQuickly(string browser)
    {
        var page = await OpenTreeAsync(browser);

        await page.Keyboard.PressAsync("b");
        await Expect(Row(page, "Banana")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("b");
        await Expect(Row(page, "Bread")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("b");

        await Expect(Row(page, "Banana")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldFindTheRowWithADoubledLetter_WhenOneStartsWithIt(string browser)
    {
        var page = await OpenTreeAsync(browser);

        await page.Keyboard.TypeAsync("aa");

        await Expect(Row(page, "Aaron")).ToBeFocusedAsync();
    }
}
