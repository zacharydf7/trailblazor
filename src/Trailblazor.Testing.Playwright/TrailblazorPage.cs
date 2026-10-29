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
