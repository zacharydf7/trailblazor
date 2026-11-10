using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbAccordionItem</c>, in an accordion or on its own, found by its header's text.</summary>
public sealed class TbAccordionItemLocator(ILocator scope, string header)
{
    /// <summary>The header's button, whose <c>aria-expanded</c> says whether the item is open.</summary>
    public ILocator Trigger => scope.GetByRole(AriaRole.Button, new() { Name = header, Exact = true })
        .And(scope.Locator("[data-part=trigger]"));

    /// <summary>Opens the item, if it's closed, and waits until it is open. Returns its panel. Throws for a disabled
    /// item that is closed.</summary>
    public async Task<ILocator> ExpandAsync()
    {
        await SetAsync(true);
        return Scope.Document(scope).Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
    }

    /// <summary>Closes the item, if it's open, and waits until it is closed. Throws for a disabled item that is open.
    /// </summary>
    public Task CollapseAsync() => SetAsync(false);

    private async Task SetAsync(bool expanded)
    {
        // A prerendered item is replaced once the page is interactive, and would lose the click.
        await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        var state = expanded ? "true" : "false";
        if (await Trigger.GetAttributeAsync("aria-expanded") == state)
        {
            return;
        }

        if (await Trigger.IsDisabledAsync())
        {
            throw new InvalidOperationException($"The accordion item \"{header}\" is disabled.");
        }

        await Trigger.ClickAsync();
        await Assertions.Expect(Trigger).ToHaveAttributeAsync("aria-expanded", state);
    }
}
