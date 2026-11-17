using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class MenubarTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Trigger(IPage page, string name) =>
        page.GetByRole(AriaRole.Menubar).GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true });

    private static ILocator Item(IPage page, string name) =>
        page.GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true });

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menubar");
        await Expect(page.GetByRole(AriaRole.Menubar)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menubar_ShouldMoveAlongTheBarWithTheArrowKeys_WhenTheBarHasFocus(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Trigger(page, "File").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Trigger(page, "Edit")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Trigger(page, "View")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Trigger(page, "File")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Trigger(page, "View")).ToBeFocusedAsync();

        Assert.Equal(["-1", "-1", "0"], await page.GetByRole(AriaRole.Menubar).Locator("[data-part=trigger]")
            .EvaluateAllAsync<string[]>("triggers => triggers.map(trigger => trigger.getAttribute('tabindex'))"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menubar_ShouldMoveToTheMenuBeside_WhenRightOrLeftIsPressedInAnOpenMenu(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Trigger(page, "File").FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item(page, "New trail")).ToBeFocusedAsync();
        await page.EvaluateAsync("() => document.addEventListener('focusin', event => "
            + "(window.focused ??= []).push(event.target.textContent.trim()))");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item(page, "Undo")).ToBeFocusedAsync();
        await Expect(Item(page, "New trail")).ToBeHiddenAsync();
        Assert.Equal(["Edit", "Undo"], await page.EvaluateAsync<string[]>("() => window.focused"));
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item(page, "New trail")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(Trigger(page, "File")).ToBeFocusedAsync();
        await Expect(Item(page, "New trail")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menubar_ShouldOpenTheMenuPointedAt_WhenAnotherIsOpen(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Trigger(page, "File").ClickAsync();
        await Expect(Item(page, "New trail")).ToBeVisibleAsync();
        await Trigger(page, "View").HoverAsync();

        await Expect(Item(page, "Zoom in")).ToBeVisibleAsync();
        await Expect(Item(page, "New trail")).ToBeHiddenAsync();
    }

    // Only Chromium can drive touch from Playwright.
    [Fact]
    public async Task Menubar_ShouldOpenTheMenuTapped_WhenAnotherIsOpen()
    {
        var page = await OpenAsync("chromium", "/examples/menubar", new() { HasTouch = true });
        await Expect(page.GetByRole(AriaRole.Menubar)).ToHaveAttributeAsync("data-ready", "");

        await Trigger(page, "File").TapAsync();
        await Expect(Item(page, "New trail")).ToBeVisibleAsync();
        await Trigger(page, "Edit").TapAsync();

        await Expect(Item(page, "Undo")).ToBeVisibleAsync();
        await Expect(Item(page, "New trail")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menubar_ShouldKeepOneTabStop_WhenTheMenuHoldingItGoesAway(string browser)
    {
        var page = await OpenAsync(browser, "/menubar-changes");
        await Expect(page.GetByRole(AriaRole.Menubar)).ToHaveAttributeAsync("data-ready", "");
        await Trigger(page, "File").FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Trigger(page, "View")).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Remove View" }).ClickAsync();
        await Expect(Trigger(page, "View")).ToHaveCountAsync(0);

        await Expect(page.GetByRole(AriaRole.Menubar).Locator("[data-part=trigger][tabindex='0']")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menubar_ShouldSwapTheArrows_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await Trigger(page, "File").FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Trigger(page, "Edit")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldChooseAMenusItem_WhenUsedThroughTheBar(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var bar = page.Tb().Menubar("Trail editor");

        Assert.Equal(["File", "Edit", "View"], await bar.MenusAsync());
        await bar.Menu("Edit").ChooseAsync("Redo");

        await Expect(page.GetByText("Last command: Redo")).ToBeVisibleAsync();
    }
}
