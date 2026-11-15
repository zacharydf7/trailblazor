using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbBreadcrumbs</c> trail, found by its name.</summary>
public sealed class TbBreadcrumbsLocator(ILocator scope, string? name)
{
    // Each item holds its separator, then its link or its text.
    private const string Labels =
        "[data-part=breadcrumb] > [data-part=link], [data-part=breadcrumb] > [data-part=text]";

    /// <summary>The trail's navigation landmark.</summary>
    public ILocator Trail => Scope.ByRole(scope, AriaRole.Navigation, name).And(scope.Locator(".tb-breadcrumbs"));

    /// <summary>The items' texts, from the top of the site down to the current page.</summary>
    public async Task<IReadOnlyList<string>> ItemsAsync()
    {
        await Trail.WaitForAsync();
        return [.. (await Trail.Locator(Labels).AllTextContentsAsync()).Select(text => text.Trim())];
    }

    /// <summary>The text of the item marked as the current page, or null when none is.</summary>
    public async Task<string?> CurrentAsync()
    {
        await Trail.WaitForAsync();
        var current = Trail.Locator(Labels).And(scope.Locator("[aria-current=page]"));
        return await current.CountAsync() == 0 ? null : (await current.TextContentAsync())?.Trim();
    }

    /// <summary>Follows the link of the item with this text, as clicking it does. It throws for an item without a
    /// link.</summary>
    public async Task FollowAsync(string item)
    {
        await Trail.WaitForAsync();
        var label = Trail.Locator(Labels).Filter(new() { HasTextRegex = new Regex($@"^\s*{Regex.Escape(item)}\s*$") });
        var count = await label.CountAsync();
        if (count != 1)
        {
            throw new InvalidOperationException(count == 0
                ? $"The breadcrumb trail has no item that reads \"{item}\"." : $"{count} items read \"{item}\".");
        }

        if (await label.GetAttributeAsync("data-part") != "link")
        {
            throw new InvalidOperationException($"\"{item}\" isn't a link; only an item with an Href is.");
        }

        await label.ClickAsync();
    }
}
