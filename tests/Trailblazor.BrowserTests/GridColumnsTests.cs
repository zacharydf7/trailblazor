using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class GridColumnsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scroll_ShouldDrawTheColumnsComingIntoView_WhenTheBoxScrollsSideways(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Expect(page.Locator("th[data-column=s30]")).ToHaveCountAsync(0);

        await page.Locator("[data-part=scroll]").EvaluateAsync("box => box.scrollLeft = 30 * 160");

        await Expect(page.Locator("th[data-column=s30]")).ToHaveCountAsync(1);
        await Expect(page.Locator("th[data-column=s0]")).ToHaveCountAsync(0);
        await Expect(page.Locator("th[data-column=Id]")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldReachColumnsNotYetDrawn_WhenTheArrowKeysMovePastTheDrawnOnes(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Locator("tr[data-key='1'] td[data-column=s0]").FocusAsync();

        for (var sensor = 1; sensor <= 16; sensor++)
        {
            await page.Keyboard.PressAsync("ArrowRight");
            await Expect(page.Locator($"tr[data-key='1'] td[data-column=s{sensor}]")).ToBeFocusedAsync();
        }

        // A short scroll moves which columns are drawn while the focused one stays in view.
        var table = page.GetByRole(AriaRole.Grid, new() { Name = "Readings" });
        var drawn = await table.GetAttributeAsync("data-column-window");
        await page.Locator("[data-part=scroll]").EvaluateAsync("box => box.scrollLeft += 160");
        await Expect(table).Not.ToHaveAttributeAsync("data-column-window", drawn!);
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.Locator("tr[data-key='2'] td[data-column=s16]")).ToBeFocusedAsync();
        await Expect(page.Locator("tr[data-key='2'] td[data-column=s16]")).ToHaveAttributeAsync("aria-colindex", "18");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EndAndHome_ShouldReachTheLastAndFirstColumns_WhenPressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Locator("tr[data-key='1'] td[data-column=s0]").FocusAsync();

        await page.Keyboard.PressAsync("End");
        await Expect(page.Locator("tr[data-key='1'] td[data-column=total]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(page.Locator("tr[data-key='1'] td[data-column=s39]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.Locator("tr[data-key='1'] td[data-column=total]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");

        await Expect(page.Locator("tr[data-key='1'] td[data-column=Id]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.Locator("tr[data-key='1'] td[data-column=s0]")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(page.Locator("tr[data-key='1'] td[data-column=Id]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ArrowLeft_ShouldShowTheColumnItReachesAsSoonAsFocusLands_WhenTheColumnWasNotDrawn(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // WebKit scrolls a cell focused from script into view on a later frame. Columns sent for the box's scroll
        // position before then leave the cell out, and it loses the focus as it goes. So the cell has to show by the
        // time the script that focused it is done, which is when this microtask runs.
        await page.EvaluateAsync("""
            () => document.querySelector('[role=grid]').addEventListener('focusin', event => {
                if (event.target.dataset.column === 's39') {
                    queueMicrotask(() => {
                        const box = event.target.getBoundingClientRect();
                        window.shown = event.target.contains(
                            document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2));
                    });
                }
            })
            """);
        await page.Locator("tr[data-key='1'] td[data-column=s0]").FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(page.Locator("tr[data-key='1'] td[data-column=total]")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(page.Locator("tr[data-key='1'] td[data-column=s39]")).ToBeFocusedAsync();
        Assert.True(await page.EvaluateAsync<bool>("window.shown"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scroll_ShouldDrawTheColumnsComingIntoView_WhenTheGridRunsRightToLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");

        await page.Locator("[data-part=scroll]").EvaluateAsync("box => box.scrollLeft = -30 * 160");

        await Expect(page.Locator("th[data-column=s30]")).ToHaveCountAsync(1);
        await Expect(page.Locator("th[data-column=s0]")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldStayOffOtherColumns_WhenTheFocusedColumnScrollsAway(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Locator("tr[data-key='1'] td[data-column=s1]").FocusAsync();

        await page.Locator("[data-part=scroll]").EvaluateAsync("box => box.scrollLeft = 30 * 160");
        await Expect(page.Locator("th[data-column=s1]")).ToHaveCountAsync(0);

        Assert.Null(await page.EvaluateAsync<string?>("() => document.activeElement?.dataset.column ?? null"));
        await page.Locator("tr[data-key='2'] td[data-column=Id]").FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(page.Locator("tr[data-key='1'] td[data-column=Id]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldReachHeadersNotYetDrawn_WhenTheGridHasNoRows(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove the readings" }).ClickAsync();
        await Expect(page.Locator("tbody tr[data-key]")).ToHaveCountAsync(0);
        await page.Locator("th[data-column=s8]").FocusAsync();

        for (var sensor = 9; sensor <= 13; sensor++)
        {
            await page.Keyboard.PressAsync("ArrowRight");
            await Expect(page.Locator($"th[data-column=s{sensor}]")).ToBeFocusedAsync(new() { Timeout = 2000 });
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Widths_ShouldFollowThePagesFontSize_WhenItChangesWithoutTheColumnsInViewChanging(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var table = page.GetByRole(AriaRole.Grid, new() { Name = "Readings" });
        await Expect(table).ToHaveAttributeAsync("data-column-widths", new Regex("^160,"));

        await page.EvaluateAsync("() => document.documentElement.style.fontSize = '20px'");
        await page.Locator("[data-part=scroll]").EvaluateAsync("box => box.scrollLeft += 1");

        await Expect(table).ToHaveAttributeAsync("data-column-widths", new Regex("^200,"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldLeaveFocusWhereItWent_WhenItLeavesTheGridBeforeTheColumnIsDrawn(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var table = page.GetByRole(AriaRole.Grid, new() { Name = "Readings" });
        await Expect(table).Not.ToHaveAttributeAsync("data-column-window", "0,9");

        await page.EvaluateAsync($$"""
            () => {
                {{RecordColumnWindows}}
                const last = [...document.querySelectorAll("tr[data-key='1'] td[data-column^=s]")].at(-1);
                last.focus();
                last.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
                const button = [...document.querySelectorAll('button')].find(one => one.textContent.includes('Remove'));
                button.focus();
            }
            """);

        await WaitForTheColumnToComeAndGoAsync(page);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Remove the readings" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldEndOnTheChosenCell_WhenACellIsChosenBeforeTheColumnIsDrawn(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var table = page.GetByRole(AriaRole.Grid, new() { Name = "Readings" });
        await Expect(table).Not.ToHaveAttributeAsync("data-column-window", "0,9");

        await page.EvaluateAsync($$"""
            () => {
                {{RecordColumnWindows}}
                const last = [...document.querySelectorAll("tr[data-key='1'] td[data-column^=s]")].at(-1);
                last.focus();
                last.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
                document.querySelector("tr[data-key='2'] td[data-column=s0]").focus();
            }
            """);

        await WaitForTheColumnToComeAndGoAsync(page);
        await Expect(page.Locator("tr[data-key='2'] td[data-column=s0]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldKeepEveryColumnInViewDrawn_WhenTheyReachAColumnAlreadyInView(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var table = page.GetByRole(AriaRole.Grid, new() { Name = "Readings" });
        await Expect(table).Not.ToHaveAttributeAsync("data-column-window", "0,9");
        var shown = (await table.GetAttributeAsync("data-column-window"))!.Split(',').Select(int.Parse).ToArray();

        // Scrolled five columns, the box shows the column after the last one drawn, clear of the pinned total, before
        // its new window is sent.
        await page.EvaluateAsync("""
            () => {
                const last = [...document.querySelectorAll("tr[data-key='1'] td[data-column^=s]")].at(-1);
                last.focus();
                document.querySelector('[data-part=scroll]').scrollLeft += 5 * 160;
                last.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
            }
            """);

        await Expect(page.Locator($"tr[data-key='1'] td[data-column=s{shown[1] + 3}]")).ToBeFocusedAsync();
        await Expect(page.Locator($"th[data-column=s{shown[0] + 5}]")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldReachTheLastCell_WhenControlEndIsPressedInAGridThatDrawsRowsAndColumnsInView(
        string browser)
    {
        var page = await OpenReadyAsync(browser, "/grid-columns-rows");
        var table = page.GetByRole(AriaRole.Grid, new() { Name = "Readings" });
        await table.Locator("tr[aria-rowindex='2'] td[data-column=Id]").ClickAsync();
        for (var sensor = 0; sensor <= 9; sensor++)
        {
            await page.Keyboard.PressAsync("ArrowRight");
            await Expect(table.Locator($"tr[aria-rowindex='2'] td[data-column=s{sensor}]")).ToBeFocusedAsync();
        }

        await page.Keyboard.PressAsync("Control+End");

        await Expect(table.Locator("tr[aria-rowindex='501'] td[data-column=total]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CellNavigation_ShouldReachTheLastCell_WhenControlEndIsPressedWhileAColumnIsStillBeingDrawn(
        string browser)
    {
        var page = await OpenReadyAsync(browser, "/grid-columns-rows");
        var table = page.GetByRole(AriaRole.Grid, new() { Name = "Readings" });
        await Expect(table.Locator("tr[aria-rowindex='2'] td[data-column=total]")).ToBeVisibleAsync();

        // Left from the pinned total asks for the last sensor, which isn't drawn, and Control+End follows at once.
        // WebKit scrolls a cell focused from script into view on the next frame, so wait for that scroll first, or it
        // would undo Control+End's jump.
        await table.EvaluateAsync("""
            async table => {
                const total = table.querySelector("tr[aria-rowindex='2'] td[data-column=total]");
                total.focus();
                await new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)));
                total.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
                total.dispatchEvent(new KeyboardEvent('keydown', { key: 'End', ctrlKey: true, bubbles: true }));
            }
            """);

        await Expect(table.Locator("tr[aria-rowindex='501'] td[data-column=total]")).ToBeFocusedAsync();
    }

    // The window narrows to the column the keys ask for, and then goes back to the columns the box shows, within a few
    // milliseconds: too briefly for a check that polls, so the page records each window the table is given.
    private const string RecordColumnWindows = """
        const table = document.querySelector("[role=grid][aria-label=Readings]");
        window.columnWindows = [];
        new MutationObserver(() => window.columnWindows.push(table.dataset.columnWindow))
            .observe(table, { attributes: true, attributeFilter: ['data-column-window'] });
        """;

    private static Task WaitForTheColumnToComeAndGoAsync(IPage page) => page.WaitForFunctionAsync("""
        () => {
            const alone = shown => shown.split(',')[0] === shown.split(',')[1];
            return window.columnWindows.some(alone) && !alone(window.columnWindows.at(-1));
        }
        """);

    private async Task<IPage> OpenReadyAsync(string browser, string path = "/grid-columns")
    {
        var page = await OpenAsync(browser, path);
        await Expect(page.Locator("[data-part=grid]")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }
}
