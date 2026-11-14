using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbList</c>, found by its <c>aria-label</c>, or the only one in the scope. Its items are named by
/// their text, leaving out their description and end content.</summary>
public sealed class TbListLocator(ILocator scope, string? name)
{
    private ILocator List => Scope.ByRole(scope, AriaRole.List, name).And(scope.Locator(".tb-list"));

    private ILocator Items => List.Locator(":scope > li");

    /// <summary>The items' names, in order.</summary>
    public async Task<IReadOnlyList<string>> ItemsAsync() =>
        [.. (await Items.Locator("[data-part=text]").AllTextContentsAsync()).Select(text => text.Trim())];

    /// <summary>Clicks the item with this text. The item must be a button or a link, which it is when it has an
    /// <c>OnClick</c> or an <c>Href</c>. A disabled item throws.</summary>
    public async Task ClickAsync(string item)
    {
        var named = scope.Page.Locator("[data-part=text]")
            .Filter(new() { HasTextRegex = new Regex($@"^\s*{Regex.Escape(item)}\s*$") });
        var row = Items.Filter(new() { Has = named });
        var action = row.Locator(":scope > [data-part=action]");
        if (await action.CountAsync() != 1)
        {
            throw new InvalidOperationException(await row.Locator(":scope > [data-part=content][aria-disabled=true]")
                .CountAsync() > 0 ? $"\"{item}\" is disabled." : $"No button or link in the list reads \"{item}\".");
        }

        if (await action.IsDisabledAsync())
        {
            throw new InvalidOperationException($"\"{item}\" is disabled.");
        }

        await action.ClickAsync();
    }
}
