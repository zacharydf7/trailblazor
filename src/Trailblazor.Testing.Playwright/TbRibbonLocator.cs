using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbRibbon</c>, found by its <c>AriaLabel</c>, or the only one in the scope.</summary>
public sealed class TbRibbonLocator(ILocator scope, string? name)
{
    // What is inside a ribbon that is itself inside this one, as in a dialog written in a group, isn't this ribbon's.
    private const string Own = ":not(:scope .tb-ribbon *)";

    /// <summary>The ribbon.</summary>
    public ILocator Ribbon => name is null ? scope.Locator(".tb-ribbon") : scope.Locator(".tb-ribbon").Filter(new()
    {
        // By its own tabs, since a ribbon that holds another one has that one's tabs inside it too.
        Has = scope.Page.Locator(":scope > .tb-tabs > [role=tablist]")
            .And(scope.Page.GetByRole(AriaRole.Tablist, new() { Name = name, Exact = true })),
    });

    private TbTabsLocator TabList => new(Ribbon, Ribbon.Locator("> .tb-tabs > [role=tablist]"));

    // The selected tab's row of commands: the other tabs' rows stay on the page, hidden, once they have been shown.
    private ILocator Commands => Ribbon.Locator("> .tb-tabs > [role=tabpanel]:not([hidden]) > [data-part=commands]");

    /// <summary>The tabs' titles, in order.</summary>
    public Task<IReadOnlyList<string>> TabsAsync() => TabList.TabsAsync();

    /// <summary>The selected tab's title.</summary>
    public Task<string?> SelectedAsync() => TabList.SelectedAsync();

    /// <summary>Clicks the tab with this title and waits until it is selected and the app has run its callbacks,
    /// such as <c>ActiveKeyChanged</c>.</summary>
    public Task SelectAsync(string tab) => TabList.SelectAsync(tab);

    /// <summary>The names of the selected tab's groups, in order.</summary>
    public async Task<IReadOnlyList<string>> GroupsAsync() =>
        [.. (await Commands.Locator($"[data-part=group-label]{Own}").AllTextContentsAsync())
            .Select(text => text.Trim())];

    /// <summary>Whether the ribbon is collapsed to its tabs, read once the ribbon is interactive. On a static page
    /// it's always false, since the ribbon there can't collapse.</summary>
    public async Task<bool> IsCollapsedAsync()
    {
        if (await IsStaticAsync())
        {
            return false;
        }

        // A prerendered ribbon shows its commands until the page is interactive, whatever Collapsed says.
        await Assertions.Expect(Ribbon).ToHaveAttributeAsync("data-ready", "");
        return await Ribbon.GetAttributeAsync("data-collapsed") is not null;
    }

    /// <summary>Collapses the ribbon to its tabs with its collapse button, unless it already is, and waits until it
    /// has. Throws for a ribbon without <c>Collapsible</c>, which has no such button, or on a static page.</summary>
    public Task CollapseAsync() => SetCollapsedAsync(true);

    /// <summary>Shows a collapsed ribbon's commands again with its collapse button, unless they already show, and
    /// waits until they do. Throws for a ribbon without <c>Collapsible</c>, which has no such button, or on a static
    /// page.</summary>
    public Task ExpandAsync() => SetCollapsedAsync(false);

    private async Task<bool> IsStaticAsync()
    {
        await Ribbon.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return await Ribbon.GetAttributeAsync("data-static") is not null;
    }

    private async Task SetCollapsedAsync(bool collapsed)
    {
        if (await IsStaticAsync())
        {
            throw new InvalidOperationException("On a static page the ribbon can't collapse; render the page "
                + "interactively to collapse it.");
        }

        var isCollapsed = await IsCollapsedAsync();
        var button = Ribbon.Locator($"[data-part=collapse]{Own}");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException("The ribbon has no collapse button; set Collapsible to give it one.");
        }

        if (isCollapsed == collapsed)
        {
            return;
        }

        await button.ClickAsync();
        var marked = Assertions.Expect(Ribbon);
        await (collapsed ? marked : marked.Not).ToHaveAttributeAsync("data-collapsed", "");
    }

    /// <summary>Presses Alt and then each of these key tips in turn, as a user does: a tab's, then one of its
    /// commands', or the one of a control beside the tabs. Each call starts from Alt, after putting away any key
    /// tips left showing. Where Alt goes to another ribbon on the page, focus moves to this ribbon's selected tab
    /// first, and a command's key then gives focus back to that tab. It waits for each key tip's badge to show before
    /// pressing its key, and for the app's handler to finish after it. Throws for anything but one letter or digit,
    /// for a key after the one that chose a command, and on a static page, where key tips don't work.</summary>
    public async Task PressKeyTipsAsync(params string[] keyTips)
    {
        if (await IsStaticAsync())
        {
            throw new InvalidOperationException("On a static page the ribbon has no key tips; render the page "
                + "interactively to use them.");
        }

        if (keyTips.FirstOrDefault(keyTip => keyTip.Length != 1 || !char.IsLetterOrDigit(keyTip[0])) is string wrong)
        {
            throw new ArgumentException($"A key tip is one letter or digit, and \"{wrong}\" isn't.", nameof(keyTips));
        }

        await Assertions.Expect(Ribbon).ToHaveAttributeAsync("data-ready", "");
        var keyboard = Ribbon.Page.Keyboard;
        var showing = Ribbon.Page.Locator("[data-part=ribbon][data-key-tips]");
        // Alt would only hide key tips that a tab's key left showing in an earlier call, so they're put away first.
        if (await showing.CountAsync() > 0)
        {
            await keyboard.PressAsync("Alt");
            await Assertions.Expect(showing).ToHaveCountAsync(0);
        }

        await keyboard.PressAsync("Alt");
        // Alt goes to the ribbon with focus, or else to the first one on the page. When that is another ribbon, its
        // key tips are put away and focus moves into this one, as a user's would.
        if (await Ribbon.GetAttributeAsync("data-key-tips") is null && await showing.CountAsync() > 0)
        {
            await keyboard.PressAsync("Alt");
            await Ribbon.Locator(":scope > .tb-tabs > [role=tablist] > [role=tab][tabindex='0']").FocusAsync();
            await keyboard.PressAsync("Alt");
        }

        var ended = false;
        foreach (var keyTip in keyTips)
        {
            if (ended)
            {
                throw new InvalidOperationException(
                    $"Key tips end once a command is chosen, so \"{keyTip}\" has nothing to reach.");
            }

            var control = Ribbon.Locator($"[data-key-tip-shown][data-key-tip='{keyTip}' i]");
            await Assertions.Expect(control).ToBeVisibleAsync();
            // A tab's key and a collapsed group's leave the key tips showing, and any other key ends them.
            var isTab = await control.GetAttributeAsync("role") == "tab";
            ended = !isTab && await control.GetAttributeAsync("data-part") != "group-button";
            Func<Task<bool>> press = async () =>
            {
                await keyboard.PressAsync(keyTip);
                return true;
            };
            // A tab's key is answered by the tabs, a button's by the button, and a field's key only moves focus.
            if (isTab)
            {
                await Commit.WaitAsync(control, press);
            }
            else if (await control.GetAttributeAsync("data-clicks") is not null)
            {
                await Commit.CountedAsync(control, "data-clicks", press);
            }
            else
            {
                await press();
            }
        }
    }

    /// <summary>Clicks the button with this name in the selected tab, as a user does, once the tab's commands are
    /// ready, and waits until the app's handler has run. A button in a group that has collapsed is reached by
    /// opening its group first, and one in a ribbon collapsed to its tabs by choosing the selected tab first. Throws
    /// for a disabled button, a name the selected tab doesn't have, or a static page, where the buttons do nothing.
    /// </summary>
    public async Task ClickAsync(string button)
    {
        await Commands.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        // A ribbon collapsed to its tabs shows the selected tab's commands once that tab is chosen.
        if (await IsCollapsedAsync() && await Ribbon.GetAttributeAsync("data-peek") is null)
        {
            await Ribbon.Locator("> .tb-tabs > [role=tablist] > [aria-selected=true]").ClickAsync();
            await Assertions.Expect(Ribbon).ToHaveAttributeAsync("data-peek", "");
        }

        // A collapsed group's buttons are hidden until the group is opened, and the button a group collapses into
        // isn't one of its commands.
        var found = Commands.GetByRole(AriaRole.Button, new() { Name = button, Exact = true })
            .Or(Commands.Locator("[data-collapsed] > [data-part=items]")
                .GetByRole(AriaRole.Button, new() { Name = button, Exact = true, IncludeHidden = true }))
            .And(Commands.Locator($":not([data-part=group-button]){Own}"));
        if (await found.CountAsync() == 0)
        {
            throw new InvalidOperationException($"The ribbon's selected tab has no button named \"{button}\".");
        }

        // A static page's commands never get their script, so this comes before the wait for it.
        if (await found.First.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page the ribbon's buttons do nothing; render the page "
                + "interactively to use them.");
        }

        await Assertions.Expect(Commands).ToHaveAttributeAsync("data-ready", "");
        // A fit still to come may collapse the button's group, or bring it back. The row gets data-fitting only once
        // the browser has reported its new size, a frame after the change, so this waits two frames before it waits
        // for the mark to clear.
        await Commands.EvaluateAsync(
            "() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        await Assertions.Expect(Commands).Not.ToHaveAttributeAsync("data-fitting", "");
        var group = found.Locator("xpath=ancestor::*[@data-part='group'][1]");
        if (await group.GetAttributeAsync("data-collapsed") is not null && !await found.IsVisibleAsync())
        {
            await group.Locator("> [data-part=group-button]").ClickAsync();
            await Assertions.Expect(found).ToBeVisibleAsync();
        }

        if (await found.IsDisabledAsync())
        {
            throw new InvalidOperationException($"The ribbon button \"{button}\" is disabled.");
        }

        await Commit.ClickedAsync(found);
    }
}
