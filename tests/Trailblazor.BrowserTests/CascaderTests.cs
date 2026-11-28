using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class CascaderTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Field(IPage page) =>
        page.GetByRole(AriaRole.Combobox, new() { Name = "Trailhead", Exact = true });

    private static ILocator Active(IPage page) => page.Locator("[data-part=popup] [data-part=option][data-active]");

    private async Task<IPage> OpenReadyAsync(string browser, string path = "/examples/cascader",
        BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, path, options);
        await Expect(Field(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // Focus stays in the field, so the outline is all that shows which option Enter would choose.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ActiveOption_ShouldBeOutlined_WhenTheKeysMoveToIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Field(page).FocusAsync();

        await Field(page).PressAsync("ArrowDown");
        await Field(page).PressAsync("ArrowRight");

        await Expect(Active(page)).ToHaveAccessibleNameAsync("Pine Hollow, more choices");
        await Expect(Active(page)).ToHaveCSSAsync("outline-style", "solid");
        await Expect(page.GetByRole(AriaRole.Option, new() { Name = "Eagle Ridge, more choices" }))
            .ToHaveCSSAsync("outline-style", "none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OpenOption_ShouldTakeTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenReadyAsync(browser, options: new() { ForcedColors = ForcedColors.Active });
        await Field(page).FocusAsync();

        await Field(page).PressAsync("ArrowDown");
        await Field(page).PressAsync("ArrowRight");

        var open = page.GetByRole(AriaRole.Option, new() { Name = "North ridge, more choices" });
        await Expect(open).ToHaveAttributeAsync("data-open", "");
        await Expect(open).ToHaveCSSAsync("background-color", await SystemColorAsync(page, "Highlight"));
        await Expect(page.Locator("[data-part=popup]")).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(Active(page)).ToHaveCSSAsync("outline-style", "solid");

        // Back on the open option, it is both open and active.
        await Field(page).PressAsync("ArrowLeft");

        await Expect(Active(page)).ToHaveAccessibleNameAsync("North ridge, more choices");
        await Expect(open).ToHaveCSSAsync("background-color", await SystemColorAsync(page, "Highlight"));
        await Expect(open).ToHaveCSSAsync("color", await SystemColorAsync(page, "HighlightText"));
        await Expect(open).ToHaveCSSAsync("outline-style", "solid");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldChooseAPathWithTheKeys_WhenRightOpensEachLevel(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var field = Field(page);
        await field.FocusAsync();

        await field.PressAsync("ArrowDown");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("North ridge, more choices");
        await field.PressAsync("ArrowRight");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Pine Hollow, more choices");
        await field.PressAsync("ArrowDown");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Eagle Ridge, more choices");
        await field.PressAsync("ArrowLeft");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("North ridge, more choices");
        await field.PressAsync("ArrowRight");
        await field.PressAsync("Enter");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Lower lot");
        var active = (await Active(page).GetAttributeAsync("id"))!;
        await Expect(field).ToHaveAttributeAsync("aria-activedescendant", active);
        await field.PressAsync("Enter");

        await Expect(field).ToHaveValueAsync("North ridge / Pine Hollow / Lower lot");
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(field).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldChooseAPathByClicking_WhenEachLevelIsClicked(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Tb().Cascader("Trailhead").ChooseAsync("River valley", "Cedar Flats", "Boat ramp");

        Assert.Equal("River valley / Cedar Flats / Boat ramp", await page.Tb().Cascader("Trailhead").TextAsync());
        await Expect(page.GetByText("Trailhead value: cedar-ramp")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldOpenItsLists_WhenTheChevronIsClicked(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Locator("[data-part=chevron]").First.ClickAsync(new() { Force = true });

        await Expect(page.Locator("[data-part=popup]").First).ToBeVisibleAsync();
        await Expect(Field(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldSearchAndChooseAPath_WhenTheUserTypes(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cascader = page.Tb().Cascader("Trailhead");

        await cascader.SearchAsync("lot");
        Assert.Equal(["North ridge / Pine Hollow / Lower lot", "North ridge / Pine Hollow / Upper lot",
            "North ridge / Eagle Ridge / Main lot", "North ridge / Eagle Ridge / Overflow lot"],
            await cascader.ResultsAsync());
        await cascader.ChooseResultAsync("upper", "North ridge / Pine Hollow / Upper lot");

        await Expect(page.GetByText("Trailhead value: pine-upper")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldRefuseAPartlyMatchingOrDisabledChoice_WhenChoosing(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var cascader = page.Tb().Cascader("Trailhead");

        var partial = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cascader.ChooseAsync("North ridge", "Pine Hollow", "lot"));
        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(() => cascader.ChooseAsync("High peaks"));

        Assert.Equal(("Level 3 has no choice \"lot\".", "\"High peaks\" is disabled."),
            (partial.Message, disabled.Message));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldHideItsLists_WhenClosed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var popup = page.Locator("[data-part=popup]");
        var field = Field(page);

        await Expect(popup).ToBeHiddenAsync();
        await field.ClickAsync();
        await Expect(popup).ToBeVisibleAsync();
        await field.PressAsync("Escape");

        await Expect(popup).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldMoveIntoTheNewList_WhenRightOpensASiblingOfTheSameShape(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var field = Field(page);
        await field.FocusAsync();

        await field.PressAsync("ArrowDown");
        await field.PressAsync("ArrowRight");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Pine Hollow, more choices");
        await field.PressAsync("ArrowRight");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Lower lot");
        await field.PressAsync("ArrowLeft");
        await field.PressAsync("ArrowDown");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Eagle Ridge, more choices");
        await field.PressAsync("ArrowRight");

        await Expect(Active(page)).ToHaveAccessibleNameAsync("Main lot");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldOpenWithoutChoosing_WhenRightBrowsesAParentThatCanBeChosen(string browser)
    {
        var page = await OpenReadyAsync(browser, "/cascader-parents");
        var field = Field(page);
        await field.FocusAsync();

        await field.PressAsync("ArrowDown");
        await field.PressAsync("ArrowRight");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Pine Hollow, more choices");
        await Expect(page.GetByTestId("value")).ToHaveTextAsync("none");
        await field.PressAsync("Enter");

        await Expect(page.GetByTestId("value")).ToHaveTextAsync("pine");
        await Expect(field).ToHaveValueAsync("North ridge / Pine Hollow");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldSwapTheArrows_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        var field = Field(page);
        await field.FocusAsync();

        await field.PressAsync("ArrowDown");
        await field.PressAsync("ArrowLeft");
        await Expect(Active(page)).ToHaveAccessibleNameAsync("Pine Hollow, more choices");
        await field.PressAsync("ArrowRight");

        await Expect(Active(page)).ToHaveAccessibleNameAsync("North ridge, more choices");
    }
}
