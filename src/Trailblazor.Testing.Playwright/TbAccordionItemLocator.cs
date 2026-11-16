using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbAccordionItem</c>, in an accordion or on its own, found by its header's text or a locator.
/// </summary>
public sealed class TbAccordionItemLocator(ILocator scope, ILocator item)
{
    /// <summary>The header's button, whose <c>aria-expanded</c> says whether the item is open.</summary>
    // An id goes on the item around the button, so the first button inside the match counts; the item's own comes
    // before any nested item's.
    public ILocator Trigger => item.Locator("xpath=descendant-or-self::*[@data-part='trigger'][1]")
        .And(scope.Locator("[data-part=trigger]"));

    /// <summary>Opens the item, if it's closed, and waits until the app's <c>ExpandedChanged</c> has run and its panel
    /// has finished opening. Returns its panel. Throws for a disabled item that is closed.</summary>
    public async Task<ILocator> ExpandAsync()
    {
        await SetAsync(true);
        return Scope.Document(scope).Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
    }

    /// <summary>Closes the item, if it's open, and waits until the app's <c>ExpandedChanged</c> has run and its panel
    /// has finished closing. Throws for a disabled item that is open.</summary>
    public Task CollapseAsync() => SetAsync(false);

    private async Task SetAsync(bool expanded)
    {
        // A prerendered item is replaced once the page is interactive, and would lose the click.
        await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        var state = expanded ? "true" : "false";
        if (await Trigger.GetAttributeAsync("aria-expanded") != state)
        {
            if (await Trigger.IsDisabledAsync())
            {
                throw new InvalidOperationException("This accordion item is disabled.");
            }

            await Commit.CountedAsync(Trigger, "data-commits", async () =>
            {
                await Trigger.ClickAsync();
                return true;
            });
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("aria-expanded", state);
        }

        // The panel grows or shrinks after aria-expanded changes, and the item's data-state says when it has stopped.
        await Assertions.Expect(Trigger.Locator("xpath=ancestor::*[@data-part='item'][1]"))
            .ToHaveAttributeAsync("data-state", expanded ? "open" : "closed");
    }
}
