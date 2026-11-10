using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The <c>TbTooltip</c> around a trigger, which can be any locator, such as one row's button among several
/// that share a name.</summary>
public sealed class TbTooltipLocator(ILocator trigger)
{
    // The nearest anchor around the trigger is its own tooltip's, even when tooltips are nested.
    private ILocator Anchor => trigger.Locator(
        "xpath=ancestor::*[contains(concat(' ', normalize-space(@class), ' '), ' tb-tooltip-anchor ')][1]");

    /// <summary>The tooltip's bubble.</summary>
    public ILocator Bubble => Anchor.Locator(":scope > [role=tooltip]");

    /// <summary>Rests the pointer on the trigger, waits for the tooltip to show and returns its text. Another tooltip
    /// already showing is closed first.</summary>
    public async Task<string> TextAsync()
    {
        await Assertions.Expect(Anchor).ToHaveAttributeAsync("data-ready", "");
        if (await Bubble.IsVisibleAsync())
        {
            return (await Bubble.InnerTextAsync()).Trim();
        }

        // Close a shown bubble, which can cover the trigger; Escape with none open would close an enclosing dialog.
        var shown = trigger.Page.Locator(".tb-tooltip:popover-open");
        if (await shown.CountAsync() > 0)
        {
            await trigger.Page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(shown).ToHaveCountAsync(0);
        }

        // A tooltip shows only when the pointer enters its trigger, so move a pointer already on it away first.
        if (await Anchor.EvaluateAsync<bool>("anchor => anchor.matches(':hover')"))
        {
            await trigger.Page.Mouse.MoveAsync(0, 0);
        }

        await trigger.HoverAsync();
        await Assertions.Expect(Bubble).ToBeVisibleAsync();
        return (await Bubble.InnerTextAsync()).Trim();
    }
}
