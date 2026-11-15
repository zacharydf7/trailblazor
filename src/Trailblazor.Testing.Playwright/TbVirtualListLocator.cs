using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbVirtualList</c>, found by its <c>AriaLabel</c>, or the only one in the scope.</summary>
public sealed class TbVirtualListLocator(ILocator scope, string? name)
{
    /// <summary>The list: a feed, or a group while it has no items.</summary>
    public ILocator Feed => Scope.ByRole(scope, AriaRole.Feed, name).Or(Scope.ByRole(scope, AriaRole.Group, name))
        .And(scope.Locator(".tb-virtual-list"));

    /// <summary>How many items the list has, drawn or not, once it has drawn its first items or its empty content.
    /// </summary>
    public async Task<int> CountAsync()
    {
        await Feed.WaitForAsync();
        // Virtualize asks an ItemsProvider for items only once the browser reports the view, so until it draws what
        // came back, the count is still 0.
        if (await Feed.GetAttributeAsync("data-static") is null)
        {
            await Feed.Locator(":scope > :is([data-part=item], [data-part=empty])").First.WaitForAsync();
            await Assertions.Expect(Feed).ToHaveAttributeAsync("aria-busy", "false");
        }

        return int.Parse((await Feed.GetAttributeAsync("data-count"))!, CultureInfo.InvariantCulture);
    }

    /// <summary>The item at this index, from 0. The list draws only the items near its view, so this scrolls the
    /// list to the item, as a user does, and waits until it is drawn.</summary>
    public async Task<ILocator> ItemAsync(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, await CountAsync());
        var item = Feed.Locator($":scope > [data-part=item][data-index='{index}']");
        // A static page draws every item.
        if (await Feed.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Feed).ToHaveAttributeAsync("data-ready", "");
            await Feed.EvaluateAsync(
                "(feed, index) => feed.dispatchEvent(new CustomEvent('tb-scroll-to', { detail: index }))", index);
        }

        await item.WaitForAsync();
        return item;
    }
}
