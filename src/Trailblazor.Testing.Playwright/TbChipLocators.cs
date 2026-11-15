using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbChip</c>, found by its text.</summary>
public sealed class TbChipLocator
{
    private readonly ILocator _chips;
    private readonly string _text;

    internal TbChipLocator(ILocator chips, string text) => (_chips, _text) = (chips, text);

    /// <summary>The chip.</summary>
    public ILocator Chip => _chips.Filter(new()
    {
        Has = _chips.Page.Locator("[data-part=label]", new()
        {
            HasTextRegex = new Regex($"^\\s*{Regex.Escape(_text)}\\s*$"),
        }),
    });

    /// <summary>Whether a chip that toggles is selected.</summary>
    public async Task<bool> SelectedAsync() =>
        await Chip.Locator("[data-part=action]").GetAttributeAsync("aria-pressed") == "true";

    /// <summary>Clicks the chip, as a user does, once the page is interactive: a chip that toggles is selected or
    /// cleared, and the call waits until it has changed; one with an <c>OnClick</c> runs it, and one with an
    /// <c>Href</c> follows its link. It throws for a chip that does nothing when clicked, a disabled chip, or one on a
    /// static page.</summary>
    public async Task ClickAsync()
    {
        var action = (await UnlockedAsync()).Locator("[data-part=action]");
        if (await action.EvaluateAsync<string>("action => action.localName") is not ("button" or "a"))
        {
            throw new InvalidOperationException("This chip does nothing when clicked; give it an OnClick, an Href or a "
                + "SelectedChanged.");
        }

        var pressed = await action.GetAttributeAsync("aria-pressed");
        await action.ClickAsync();
        if (pressed is not null)
        {
            await Assertions.Expect(action).ToHaveAttributeAsync("aria-pressed", pressed == "true" ? "false" : "true");
        }
    }

    /// <summary>Presses the chip's remove button, which raises <c>OnRemove</c>, once the page is interactive. A chip
    /// with no <c>OnRemove</c> has no remove button, so this throws, as it does for a disabled chip or one on a static
    /// page.</summary>
    public async Task RemoveAsync()
    {
        var remove = (await UnlockedAsync()).Locator("[data-part=remove]");
        if (await remove.CountAsync() == 0)
        {
            throw new InvalidOperationException("This chip has no remove button; it shows only when OnRemove is set.");
        }

        await remove.ClickAsync();
    }

    private async Task<ILocator> UnlockedAsync()
    {
        var chip = Chip;
        if (await chip.EvaluateAsync<bool>("chip => chip.classList.contains('tb-chip--disabled')"))
        {
            throw new InvalidOperationException("This chip is disabled, so a user can't click it.");
        }

        if (await chip.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page a chip's buttons do nothing; render the page "
                + "interactively to use them.");
        }

        // A prerendered chip is replaced once the page is interactive, and its buttons would lose the click.
        await Assertions.Expect(chip).ToHaveAttributeAsync("data-ready", "");
        return chip;
    }
}

/// <summary>A <c>TbChipGroup</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbChipGroupLocator(ILocator scope, string name)
{
    /// <summary>The group's list.</summary>
    public ILocator Group => scope.GetByRole(AriaRole.List, new() { Name = name, Exact = true })
        .And(scope.Locator(".tb-chip-group"));

    /// <summary>The chips' texts, in order.</summary>
    public async Task<IReadOnlyList<string>> ChipsAsync() =>
        [.. (await Group.Locator(".tb-chip [data-part=label]").AllInnerTextsAsync()).Select(text => text.Trim())];

    /// <summary>The chip in the group whose text is this.</summary>
    public TbChipLocator Chip(string text) => new(Group.Locator(".tb-chip"), text);
}
