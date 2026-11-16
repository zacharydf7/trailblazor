using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbNavMenu</c>, found by its navigation landmark's name.</summary>
public sealed class TbNavMenuLocator(ILocator scope, string? name)
{
    /// <summary>The menu's list, as shown.</summary>
    public ILocator Menu => (name is null ? scope.Locator("ul.tb-nav-menu")
        : Scope.ByRole(scope, AriaRole.Navigation, name).Locator("ul.tb-nav-menu")).Locator("visible=true");

    /// <summary>The text of the link marked as the current page, or null when none is. A link stays current on the
    /// pages under its address, so when several are current, it's the one with the longest address.</summary>
    public async Task<string?> CurrentAsync()
    {
        await Menu.WaitForAsync();
        var current = await Menu.Locator("[data-part=link][aria-current=page]").EvaluateAllAsync<string[][]>(
            "links => links.map(link => [link.getAttribute('href'), "
            + "link.querySelector(':scope > [data-part=label]').textContent.trim()])");
        return current.MaxBy(link => link[0].Length)?[1];
    }

    /// <summary>Follows the link with this text, through the groups named before it, as in
    /// <c>FollowAsync("Settings", "Profile")</c>. Each closed group opens as clicking it does, once the page is
    /// interactive, and the app's <c>ExpandedChanged</c> runs before the link is clicked. Throws for a link in a
    /// closed group the path doesn't name, or for a closed group on a static page, which can't open.</summary>
    public async Task FollowAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var within = await OpenAsync(path[..^1]);
        var link = await OnlyAsync(within, "link", path[^1], "links");
        if (!await link.IsVisibleAsync())
        {
            throw InClosedGroup(path[^1]);
        }

        await link.ClickAsync();
    }

    /// <summary>Whether the group at the end of this path is open, as in <c>ExpandedAsync("Settings", "Advanced")
    /// </c>. The groups before it are left as they are.</summary>
    public async Task<bool> ExpandedAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        await Menu.WaitForAsync();
        var within = Menu;
        foreach (var group in path[..^1])
        {
            within = Items(await OnlyAsync(within, "group-toggle", group, "groups"));
        }

        var toggle = await OnlyAsync(within, "group-toggle", path[^1], "groups");
        return await toggle.GetAttributeAsync("aria-expanded") == "true";
    }

    /// <summary>Opens each closed group on this path, as in <c>ExpandAsync("Settings", "Advanced")</c>. Each opens as
    /// clicking it does, once the page is interactive, and waits for the app's <c>ExpandedChanged</c>. Throws for a
    /// group in a closed group the path doesn't name, or for a closed group on a static page.</summary>
    public async Task ExpandAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        await OpenAsync(path);
    }

    /// <summary>Closes the group at the end of this path, if it's open, as clicking it does once the page is
    /// interactive, and waits for the app's <c>ExpandedChanged</c>. The groups before it open first, as
    /// <c>ExpandAsync</c> opens them. Throws for a group in a closed group the path doesn't name, or on a static page
    /// for a group it would have to open or close.</summary>
    public async Task CollapseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var toggle = await VisibleToggleAsync(await OpenAsync(path[..^1]), path[^1]);
        if (await toggle.GetAttributeAsync("aria-expanded") == "true")
        {
            await ToggleAsync(toggle, path[^1], false);
        }
    }

    // Opens the groups in order and returns the list of the last one's items, or the menu when given no groups.
    private async Task<ILocator> OpenAsync(string[] groups)
    {
        await Menu.WaitForAsync();
        var within = Menu;
        foreach (var group in groups)
        {
            var toggle = await VisibleToggleAsync(within, group);
            if (await toggle.GetAttributeAsync("aria-expanded") != "true")
            {
                await ToggleAsync(toggle, group, true);
            }

            within = Items(toggle);
        }

        return within;
    }

    private async Task<ILocator> VisibleToggleAsync(ILocator within, string group)
    {
        var toggle = await OnlyAsync(within, "group-toggle", group, "groups");
        return await toggle.IsVisibleAsync() ? toggle : throw InClosedGroup(group);
    }

    private async Task ToggleAsync(ILocator toggle, string group, bool expanded)
    {
        if (await Menu.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException($"On a static page the group \"{group}\" doesn't "
                + $"{(expanded ? "open" : "close")}; render the page interactively to use it.");
        }

        await Assertions.Expect(Menu).ToHaveAttributeAsync("data-ready", "");
        await Commit.CountedAsync(toggle, "data-commits", async () =>
        {
            await toggle.ClickAsync();
            return true;
        });
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", expanded ? "true" : "false");
    }

    private static ILocator Items(ILocator toggle) => toggle.Locator("xpath=../*[@data-part='group-items']");

    private static InvalidOperationException InClosedGroup(string item) => new($"\"{item}\" is in a closed group; "
        + "name the groups that contain it earlier in the path.");

    // A match at this level wins over same-labeled ones in nested groups, so a label repeated deeper doesn't hide it.
    private async Task<ILocator> OnlyAsync(ILocator within, string part, string text, string kind)
    {
        var label = new Regex($@"^\s*{Regex.Escape(text)}\s*$");
        var labeled = new LocatorFilterOptions
        {
            Has = scope.Page.Locator("[data-part=label]", new() { HasTextRegex = label }),
        };
        var found = within.Locator($"xpath=./li/*[@data-part='{part}']").Filter(labeled);
        if (await found.CountAsync() == 0)
        {
            found = within.Locator($"[data-part={part}]").Filter(labeled);
        }

        var count = await found.CountAsync();
        return count == 1 ? found : throw new InvalidOperationException(count == 0
            ? $"The nav menu has no {kind[..^1]} that reads \"{text}\"." : $"{count} {kind} read \"{text}\".");
    }
}
