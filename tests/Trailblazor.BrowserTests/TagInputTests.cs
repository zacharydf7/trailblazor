using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TagInputTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Features = "Trail features";

    private static ILocator Remove(IPage page, string tag) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Remove {tag}", Exact = true });

    // A synthetic paste carries its own clipboard, so the test doesn't need clipboard permission. Firefox ignores
    // clipboardData given to the constructor, so it's defined on the event instead.
    private static Task PasteAsync(ILocator input, string text) => input.EvaluateAsync("""
        (input, text) => {
            const data = new DataTransfer();
            data.setData('text/plain', text);
            const paste = new ClipboardEvent('paste', { bubbles: true, cancelable: true });
            Object.defineProperty(paste, 'clipboardData', { value: data });
            input.dispatchEvent(paste);
        }
        """, text);

    private async Task<IPage> OpenFeaturesAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tag-input");
        await Expect(page.Tb().TagInput(Features).Input).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldAddTheTypedText_WhenEnterOrACommaIsPressed(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);

        await features.AddAsync("Views");
        await features.Input.PressSequentiallyAsync("Bench,");

        await Expect(features.Tags).ToHaveTextAsync(["Shade", "Water", "Views", "Bench"]);
        await Expect(features.Input).ToHaveValueAsync("");
        await Expect(page.GetByText("Features: Shade, Water, Views, Bench")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldAddTheTypedText_WhenOneOfItsOwnSeparatorsIsTyped(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var packing = page.Tb().TagInput("Packing list");

        await packing.Input.PressSequentiallyAsync("Stove;");

        await Expect(packing.Tags).ToHaveTextAsync(["Tent", "Stove"]);
        await Expect(packing.Input).ToHaveValueAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldAddTheTypedText_WhenFocusLeavesTheField(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);

        await features.Input.FillAsync("Campsite");
        await features.Input.BlurAsync();

        await Expect(features.Tags).ToHaveTextAsync(["Shade", "Water", "Campsite"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldKeepTheTypedText_WhenTheWholeWindowLosesFocus(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);
        await features.Input.FillAsync("Wat");

        // Switching windows blurs the input but leaves it the active element, which a test can only imitate.
        await features.Input.EvaluateAsync("input => input.dispatchEvent(new FocusEvent('blur'))");

        await Expect(features.Input).ToHaveValueAsync("Wat");
        await Expect(features.Tags).ToHaveTextAsync(["Shade", "Water"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldRemoveTheLastTag_WhenBackspaceIsPressedInTheEmptyInput(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);
        await features.Input.FocusAsync();

        await page.Keyboard.PressAsync("Backspace");

        await Expect(features.Tags).ToHaveTextAsync(["Shade"]);
        await Expect(features.Input).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldMoveAlongTheTagsAndRemoveThem_WhenArrowAndDeleteKeysArePressed(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);
        await features.Input.FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Remove(page, "Water")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(features.Input).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("Home");
        await Expect(Remove(page, "Shade")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Delete");

        await Expect(features.Tags).ToHaveTextAsync(["Water"]);
        await Expect(Remove(page, "Water")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(features.Input).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldRemoveTheTagAndMoveBack_WhenBackspaceIsPressedOnATag(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);
        await features.Input.FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");

        await page.Keyboard.PressAsync("Backspace");

        await Expect(features.Tags).ToHaveTextAsync(["Shade"]);
        await Expect(Remove(page, "Shade")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldRemoveTheTagAndGoBackToTyping_WhenItsButtonIsClicked(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);

        await features.RemoveAsync("Shade");

        await Expect(features.Tags).ToHaveTextAsync(["Water"]);
        await Expect(features.Input).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldKeepARefusedTagInTheInputWithItsMessage_WhenItIsAlreadyThere(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);

        await features.AddAsync("shade");

        await Expect(features.Input).ToHaveValueAsync("shade");
        await Expect(features.Input).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.Locator("[data-part=error]")).ToHaveTextAsync("shade is already added.");
        await Expect(features.Tags).ToHaveTextAsync(["Shade", "Water"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldAddEachPart_WhenAListIsPasted(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);

        await PasteAsync(features.Input, "Views, Bench\nCampsite");

        await Expect(features.Tags).ToHaveTextAsync(["Shade", "Water", "Views", "Bench", "Campsite"]);
        await Expect(features.Input).ToHaveValueAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldCheckEachPutBackPartAgain_WhenTheyAreAddedAgain(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var features = page.Tb().TagInput(Features);
        await PasteAsync(features.Input, "water, shade, Views");
        await Expect(features.Input).ToHaveValueAsync("water, shade");

        await features.Input.PressAsync("Enter");

        await Expect(features.Tags).ToHaveTextAsync(["Shade", "Water", "Views"]);
        await Expect(features.Input).ToHaveValueAsync("water, shade");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldSubmitTheForm_WhenEnterIsPressedInTheEmptyInput(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        var wildlife = page.Tb().TagInput("Wildlife seen");
        await wildlife.AddAsync("Elk");

        await wildlife.Input.PressAsync("Enter");

        await Expect(page.GetByText("Sent: Elk.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldMoveToTheLastTagWithRight_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenFeaturesAsync(browser);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await page.Tb().TagInput(Features).Input.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Remove(page, "Water")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TagInput_ShouldPostItsTags_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-tag-input");

        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Shade, Water");
        await Expect(page.Tb().TagInput("Features").Tags).ToHaveTextAsync(["Shade", "Water"]);
    }
}
