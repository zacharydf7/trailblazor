namespace Trailblazor.BrowserTests;

public class StatesTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // A row-like part that is neither selected nor disabled, and the example it's in.
    private static readonly (string Name, string Example, string Part)[] Rows =
    [
        ("list item", "list", ".tb-list__item:not(.tb-list__item--selected) > .tb-list__action:not(:disabled)"),
        ("tree row", "tree-view", ".tb-tree__item:not(.tb-tree__item--selected, [aria-disabled='true'])"),
        ("accordion header", "accordion", ".tb-accordion__trigger:not(:disabled)"),
        ("grid row", "data-grid", ".tb-data-grid__row:not(.tb-data-grid__row--selected)"),
        ("striped grid row", "data-grid", ".tb-data-grid__row:nth-child(even):not(.tb-data-grid__row--selected)"),
    ];

    public static TheoryData<string, string> RowsInEveryBrowser => new(
        from browser in AllBrowsers.Names from row in Rows select (browser, row.Name));

    [Theory, MemberData(nameof(RowsInEveryBrowser))]
    public async Task Row_ShouldTakeTheHoverLayer_WhenHovered(string browser, string name)
    {
        var (_, example, part) = Rows.Single(row => row.Name == name);
        var page = await OpenAsync(browser, $"/examples/{example}");
        var row = page.Locator(part).First;

        await row.HoverAsync();

        var layer = await row.EvaluateAsync<string>("""
            row => {
                const probe = row.parentElement.appendChild(document.createElement('div'));
                probe.style.background = 'var(--tb-color-hover)';
                const color = getComputedStyle(probe).backgroundColor;
                probe.remove();
                return color;
            }
            """);
        await Expect(row).ToHaveCSSAsync("background-color", layer);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Disabled_ShouldDimChipsAsMuchAsButtons_WhenBothAreDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");

        var opacities = await page.EvaluateAsync<string[]>("""
            () => ['<button class="tb-button" disabled>Off</button>',
                '<span class="tb-chip tb-chip--disabled">Off</span>'].map(html => {
                    const host = document.querySelector('main').appendChild(document.createElement('div'));
                    host.innerHTML = html;
                    const opacity = getComputedStyle(host.firstElementChild).opacity;
                    host.remove();
                    return opacity;
                })
            """);

        Assert.Equal(["0.55", "0.55"], opacities);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Surface_ShouldLightenItsHigherElevations_WhenThePageIsDark(string browser)
    {
        var page = await OpenAsync(browser, "/examples/surface");
        await SetThemeAsync(page, "dark");

        var fills = await page.EvaluateAsync<string[]>("""
            () => {
                const probe = document.querySelector('main').appendChild(document.createElement('div'));
                const read = token => {
                    probe.style.background = `var(${token})`;
                    return getComputedStyle(probe).backgroundColor;
                };
                const tokens = [read('--tb-color-surface'), read('--tb-color-surface-raised')];
                probe.remove();
                const fill = elevation =>
                    getComputedStyle(document.querySelector(`.tb-surface--${elevation}`)).backgroundColor;
                return [...tokens, fill('low'), fill('medium'), fill('high')];
            }
            """);

        Assert.Equal([fills[0], fills[1], fills[1]], fills[2..]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckedBoxes_ShouldLookTheSameEverywhere_WhenChecked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/checkbox");
        await page.GetByLabel("Email me updates").CheckAsync();
        var looks = new List<string> { await LookAsync(page.GetByLabel("Email me updates")) };

        page = await OpenAsync(browser, "/examples/data-grid");
        var row = page.Locator("tbody .tb-data-grid__check").First;
        await row.CheckAsync();
        looks.Add(await LookAsync(row));

        page = await OpenAsync(browser, "/examples/tree-view");
        looks.Add(await LookAsync(page.Locator(".tb-tree__check--checked").First));

        page = await OpenAsync(browser, "/examples/listbox");
        looks.Add(await LookAsync(page.Locator("[aria-selected='true'] > .tb-select__box").First));

        page = await OpenAsync(browser, "/examples/select");
        var notify = page.GetByRole(AriaRole.Combobox, new() { Name = "Notify me about" });
        await Expect(notify).ToHaveAttributeAsync("data-ready", "");
        await notify.ClickAsync();
        var selectAll = page.Locator("[data-part=select-all]");
        await selectAll.ClickAsync();
        await Expect(selectAll).ToHaveAttributeAsync("aria-selected", "true");
        looks.Add(await LookAsync(selectAll.Locator(".tb-select__box")));

        Assert.All(looks, look => Assert.Equal(looks[0], look));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridRadio_ShouldStayTheBrowsersOwn_WhenTheGridSelectsOneRow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/data-grid");

        var radio = page.Locator("input[type=radio].tb-data-grid__check").First;

        await Expect(radio).Not.ToHaveCSSAsync("appearance", "none");
    }

    // A box's size, corners, colors and mark, in one string to compare.
    private static Task<string> LookAsync(ILocator box) => box.EvaluateAsync<string>("""
        box => {
            const style = getComputedStyle(box);
            const mark = getComputedStyle(box, '::before');
            return [style.width, style.height, style.borderTopLeftRadius, style.borderTopColor, style.backgroundColor,
                mark.width, mark.clipPath, mark.backgroundColor].join(' | ');
        }
        """);
}
