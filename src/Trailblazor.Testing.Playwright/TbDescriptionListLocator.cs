using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The only <c>TbDescriptionList</c> in the scope, leaving out one inside another's value: terms and their
/// values. Scope a <see cref="TrailblazorPage"/> to tell several apart.</summary>
public sealed class TbDescriptionListLocator(ILocator scope)
{
    /// <summary>The list, leaving out one inside another's value.</summary>
    public ILocator List => scope.Locator(".tb-description-list:not(.tb-description-list *)");

    /// <summary>The terms, in order.</summary>
    public async Task<IReadOnlyList<string>> TermsAsync() =>
        [.. (await Items.Locator(":scope > [data-part=term]").AllTextContentsAsync()).Select(term => term.Trim())];

    // The list's own pairs, not those of another list inside a value.
    private ILocator Items => List.Locator(":scope > [data-part=list] > [data-part=item]");

    /// <summary>The value of the term with this text, as it shows, or an empty string for a blank value, which shows a
    /// dash. Waits for the term to show.</summary>
    public async Task<string> ValueOfAsync(string term)
    {
        var ownTerm = Items.Locator(":scope > [data-part=term]")
            .Filter(new() { HasTextRegex = new($@"^\s*{Regex.Escape(term)}\s*$") }).First;
        // A blank value's dash and the words screen readers hear for it, and a tooltip's bubble, aren't shown text.
        return await ownTerm.Locator("xpath=following-sibling::*[@data-part='value'][1]").EvaluateAsync<string>("""
            value => {
                const shown = value.cloneNode(true);
                shown.querySelectorAll('[aria-hidden=true], .tb-visually-hidden, [role=tooltip]')
                    .forEach(hidden => hidden.remove());
                return shown.textContent.replace(/\s+/g, ' ').trim();
            }
            """);
    }
}
