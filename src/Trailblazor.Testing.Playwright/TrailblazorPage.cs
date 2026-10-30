using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>Playwright helpers that find Trailblazor components by role and label, as a user would, so tests keep
/// working when the markup inside a component changes.</summary>
public static class TrailblazorPageExtensions
{
    /// <summary>The Trailblazor helpers for this page.</summary>
    public static TrailblazorPage Tb(this IPage page) => new(page);
}

/// <summary>The Trailblazor components on a page.</summary>
public sealed class TrailblazorPage(IPage page)
{
    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> with this label.</summary>
    public TbSelectLocator Select(string label) => new(page, label);

    /// <summary>The <c>TbPopover</c> whose trigger has this name.</summary>
    public TbPopoverLocator Popover(string trigger) => new(page, trigger);
}

/// <summary>A <c>TbPopover</c>, found by its trigger's name.</summary>
public sealed class TbPopoverLocator(IPage page, string trigger)
{
    private ILocator Trigger => page.GetByRole(AriaRole.Button, new() { Name = trigger, Exact = true });

    /// <summary>Opens the popover, if it isn't open, and returns its panel. On a statically rendered page it opens
    /// natively, without the script.</summary>
    public async Task<ILocator> OpenAsync()
    {
        var panel = await PanelAsync();
        if (!await panel.IsVisibleAsync())
        {
            await Trigger.ClickAsync();
        }

        await Assertions.Expect(panel).ToBeVisibleAsync();
        return panel;
    }

    /// <summary>Closes the popover with Escape, as a keyboard user would, if it is open.</summary>
    public async Task CloseAsync()
    {
        var panel = await PanelAsync();
        if (await panel.IsVisibleAsync())
        {
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(panel).ToBeHiddenAsync();
        }
    }

    // Focus and hover work once the popover's script has attached, which a static page never waits for.
    private async Task<ILocator> PanelAsync()
    {
        if (await Trigger.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        }

        return page.Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
    }
}

/// <summary>A <c>TbSelect</c> or <c>TbMultiSelect</c>, found by its label.</summary>
public sealed class TbSelectLocator(IPage page, string label)
{
    /// <summary>Chooses the options with these names: the one option of a select, or each one not yet chosen in a
    /// multiple select, keeping what is already chosen. A statically rendered select is a native one, and is chosen
    /// the native way; one still prerendering is waited for.</summary>
    public async Task ChooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        // The list shares the label, so only the combobox, or the native select it replaces, counts as the field.
        var field = page.GetByLabel(label, new() { Exact = true }).And(page.Locator("[role=combobox], select"));
        var native = await field.EvaluateAsync<string?>(
            "field => field.localName !== 'select' || field.hasAttribute('data-prerender') ? null " +
            ": field.multiple ? 'multiple' : 'single'");
        if (native is not null)
        {
            ThrowIfSeveral(options, native == "multiple");
            // Choosing in a native multiple select replaces the selection, so the options already chosen are kept.
            var chosen = await field.EvaluateAsync<string[]>(
                "select => select.multiple ? [...select.selectedOptions].map(option => option.label) : []");
            await field.SelectOptionAsync([.. chosen.Union(options)]);
            return;
        }

        // The keys and clicks work once the select's script has attached.
        await Assertions.Expect(field).ToHaveAttributeAsync("data-ready", "");
        var listbox = page.Locator($"[id='{await field.GetAttributeAsync("aria-controls")}']");
        ThrowIfSeveral(options, await listbox.GetAttributeAsync("aria-multiselectable") == "true");
        if (!await listbox.IsVisibleAsync())
        {
            await field.ClickAsync();
        }

        foreach (var name in options)
        {
            // A single select closes once chosen, so the option is found even while its list is hidden.
            var option = listbox.GetByRole(AriaRole.Option, new() { Name = name, Exact = true, IncludeHidden = true });
            if (await option.GetAttributeAsync("aria-selected") != "true")
            {
                await option.ClickAsync();
                // The server applies the choice, so wait for it before reading the next option's state.
                await Assertions.Expect(option).ToHaveAttributeAsync("aria-selected", "true");
            }
        }

        // A multiple select stays open for more choices.
        if (await listbox.IsVisibleAsync())
        {
            await field.PressAsync("Escape");
        }
    }

    private static void ThrowIfSeveral(string[] options, bool multiple)
    {
        if (options.Length > 1 && !multiple)
        {
            throw new ArgumentException("Only a multiple select takes more than one option.", nameof(options));
        }
    }
}
