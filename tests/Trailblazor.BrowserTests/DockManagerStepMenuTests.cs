using System.Globalization;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerStepMenuTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grip_ShouldTurnOffTheStepThatCannotMove_WhenItsSeparatorIsAtALimit(string browser)
    {
        var page = await OpenDockAsync(browser);
        var trails = Separator(page, "Resize Trails");
        Assert.Empty(await TurnedOffAsync(page, "Size of Trails"));

        await ChangeAsync(page, () => trails.PressAsync("Home"));
        Assert.Equal(["smaller"], await TurnedOffAsync(page, "Size of Trails"));

        await ChangeAsync(page, () => trails.PressAsync("End"));
        Assert.Equal(["larger"], await TurnedOffAsync(page, "Size of Trails"));

        // A step back from the limit is one the menu offers again.
        await page.Locator(".tb-dock").Tb().Menu("Size of Trails").ChooseAsync("Make Trails smaller");
        Assert.Empty(await TurnedOffAsync(page, "Size of Trails"));

        // In a dock too narrow for its groups, no separator can move either way.
        await NarrowAsync(page, "6rem");
        await Expect(trails).ToHaveAttributeAsync("aria-valuemin", (await trails.GetAttributeAsync("aria-valuenow"))!);
        Assert.Equal(["larger", "smaller"], await TurnedOffAsync(page, "Size of Trails"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grip_ShouldDoNothingAndStayOpen_WhenAStepItTurnedOffIsChosen(string browser)
    {
        var page = await OpenDockAsync(browser);
        var trails = Separator(page, "Resize Trails");
        await ChangeAsync(page, () => trails.PressAsync("Home"));
        var sides = await trails.GetAttributeAsync("data-before");

        await ChooseTurnedOffAsync(page, "Size of Trails", "Make Trails smaller", "Make Trails larger",
            () => Expect(trails).Not.ToHaveAttributeAsync("data-before", sides!));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideGrip_ShouldDoNothingAndStayOpen_WhenAStepItTurnedOffIsChosen(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        await page.Tb().Dock().RevealAsync("Weather");
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Weather", Exact = true });
        await ChangeAsync(page, () => edge.PressAsync("End"));

        await ChooseTurnedOffAsync(page, "Size of Weather", "Make Weather larger", "Make Weather smaller",
            () => Expect(edge).ToHaveAttributeAsync("aria-valuenow", "80"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldDoNothingAndKeepItsMenuOpen_WhenAStepItTurnedOffIsChosen(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        await dock.MoveWindowAsync("Gear", -25, 0);

        await ChooseTurnedOffAsync(page, "Move the Gear window", "Left", "Right", () => page.WaitForFunctionAsync("""
            () => document.querySelector('.tb-dock__group--floating').style.getPropertyValue('--tb-dock-x') === '10'
            """));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Grip_ShouldTurnOffTheStepThatCouldOnlyComeBack_WhenASmallerDockLeavesItsSeparatorPastALimit(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        await ChangeAsync(page, () => Separator(page, "Resize Trails").PressAsync("End"));

        // In the narrower dock Trails is larger than it may be, so Larger could only bring it back, which is smaller.
        await NarrowAsync(page, "20rem");

        Assert.Equal(["larger"], await TurnedOffAsync(page, "Size of Trails"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideGrip_ShouldTurnOffSmaller_WhenASmallerDockLeavesThePanelUnderItsLeastSize(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        await page.Tb().Dock().RevealAsync("Gear");

        // Gear's quarter of a dock this narrow is less than a group may be, so Smaller could only make it larger.
        await NarrowAsync(page, "12rem");

        Assert.Equal(["smaller"], await TurnedOffAsync(page, "Size of Gear"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldTurnOffNarrower_WhenASmallerDockLeavesItUnderItsLeastSize(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        await dock.ResizeWindowAsync("Gear", -40, 0);
        Assert.Equal(["narrower"], await TurnedOffAsync(page, "Resize the Gear window"));

        // A tenth of a dock this narrow is less than a group may be. Wider still widens, and Narrower could only widen.
        await NarrowAsync(page, "20rem");

        Assert.Equal(["narrower"], await TurnedOffAsync(page, "Resize the Gear window"));
        await page.Locator(".tb-dock").Tb().Menu("Resize the Gear window").ChooseAsync("Wider");
        Assert.Equal(["narrower"], await TurnedOffAsync(page, "Resize the Gear window"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideGrip_ShouldTurnOffTheStepThatCannotMove_WhenTheSlidOutPanelIsAtALimit(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        await page.Tb().Dock().RevealAsync("Weather");
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Weather", Exact = true });
        Assert.Empty(await TurnedOffAsync(page, "Size of Weather"));

        await ChangeAsync(page, () => edge.PressAsync("End"));
        Assert.Equal(["larger"], await TurnedOffAsync(page, "Size of Weather"));

        await ChangeAsync(page, () => edge.PressAsync("Home"));
        Assert.Equal(["smaller"], await TurnedOffAsync(page, "Size of Weather"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldTurnOffTheStepsThatCannotMove_WhenItIsAtTheDocksEdgeOrItsSmallest(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        var move = page.Locator(".tb-dock").Tb().Menu("Move the Gear window");
        Assert.Empty(await TurnedOffAsync(page, "Move the Gear window"));
        Assert.Empty(await TurnedOffAsync(page, "Resize the Gear window"));

        // The window starts a quarter of the dock in. The third step goes only the 5% that is left, and then
        // there is no further to go.
        for (var step = 0; step < 3; step++)
        {
            await move.ChooseAsync("Left");
            await move.ChooseAsync("Up");
        }

        Assert.Equal((0, 0, 50, 50), await dock.WindowAsync("Gear"));
        Assert.Equal(["left", "up"], await TurnedOffAsync(page, "Move the Gear window"));
        Assert.Empty(await TurnedOffAsync(page, "Resize the Gear window"));

        await dock.MoveWindowAsync("Gear", 50, 50);
        Assert.Equal(["right", "down"], await TurnedOffAsync(page, "Move the Gear window"));
        Assert.Equal(["wider", "taller"], await TurnedOffAsync(page, "Resize the Gear window"));

        // Fifty steps of a percent take the window down to the smallest it may be.
        await dock.ResizeWindowAsync("Gear", -50, -50);
        Assert.Equal(["narrower", "shorter"], await TurnedOffAsync(page, "Resize the Gear window"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldTurnOffTheStepTowardTheSideItIsAgainst_WhenTextRunsRightToLeft(string browser)
    {
        var page = await OpenDockAsync(browser, "?direction=rtl");
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");

        // The window's box counts from the right here, so the right arrow takes it to where the box starts.
        await dock.MoveWindowAsync("Gear", 25, 0);

        Assert.Equal(0, (await dock.WindowAsync("Gear")).X);
        Assert.Equal(["right"], await TurnedOffAsync(page, "Move the Gear window"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveTheStepsOfADockInsideAPanelAlone_WhenTheirMenuOpensOrOneIsChosen(string browser)
    {
        var page = await OpenDockAsync(browser);
        var trails = Separator(page, "Resize Trails");
        await ChangeAsync(page, () => trails.PressAsync("Home"));
        // A dock inside a panel has grips of its own, numbered like this dock's. Its steps are its own to turn off,
        // and a click on one it turned off is its own to stop.
        await Panel(page, "Notes").EvaluateAsync("""
            (panel, index) => {
                const inner = document.createElement('div');
                inner.className = 'tb-dock';
                inner.innerHTML = `<div class="tb-dock__grip" data-part="grip" data-index="${index}">`
                    + '<div popover role="menu"><button type="button" data-step="smaller">Smaller</button>'
                    + '<button type="button" data-step="larger" aria-disabled="true">Larger</button></div></div>';
                panel.prepend(inner);
                inner.addEventListener('click', event => inner.dataset.chose = event.target.dataset.step);
                inner.querySelector('[popover]').showPopover();
            }
            """, await trails.GetAttributeAsync("data-index"));
        var inner = page.Locator(".tb-dock .tb-dock");

        await Expect(inner.Locator("[data-step=smaller]")).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await inner.Locator("[data-step=larger]").ClickAsync(new() { Force = true });
        await Expect(inner).ToHaveAttributeAsync("data-chose", "larger");
    }

    // Opens the dock's menu with this name and gives the steps it shows as turned off.
    private static async Task<string[]> TurnedOffAsync(IPage page, string name)
    {
        var menu = page.Locator(".tb-dock").Tb().Menu(name);
        await menu.OpenAsync();
        var off = await page.Locator(".tb-dock [role=menu]:popover-open [data-step][aria-disabled=true]")
            .EvaluateAllAsync<string[]>("items => items.map(item => item.dataset.step)");
        await menu.CloseAsync();
        return off;
    }

    // Chooses a step its menu has turned off, then one that moves, and waits for what that one changes. The dock
    // counts its script's reports in data-report, and the two choices add just one, so the step turned off sent none.
    private static async Task ChooseTurnedOffAsync(IPage page, string name, string off, string on, Func<Task> moved)
    {
        var dock = page.Locator(".tb-dock");
        var reports = await ReportsAsync(dock);
        var menu = dock.Tb().Menu(name);
        await menu.OpenAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = off, Exact = true }).ClickAsync(new() { Force = true });

        // The menu stays open, as it does for any disabled item, and the menu helper refuses the step.
        var other = page.GetByRole(AriaRole.Menuitem, new() { Name = on, Exact = true });
        await Expect(other).ToBeVisibleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => menu.ChooseAsync(off));
        await other.ClickAsync();
        await moved();
        Assert.Equal(reports + 1, await ReportsAsync(dock));
    }

    private static async Task<int> ReportsAsync(ILocator dock) =>
        int.Parse((await dock.GetAttributeAsync("data-report"))!, CultureInfo.InvariantCulture);
}
