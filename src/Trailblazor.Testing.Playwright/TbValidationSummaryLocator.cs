using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The <c>TbValidationSummary</c> in the scope, which shows only while its form has messages.</summary>
public sealed class TbValidationSummaryLocator(ILocator scope)
{
    /// <summary>The summary.</summary>
    public ILocator Element => scope.Locator(".tb-validation-summary");

    /// <summary>The messages it lists, in order, or none while it doesn't show. It reads what shows now, without
    /// waiting.</summary>
    public async Task<IReadOnlyList<string>> MessagesAsync() =>
        [.. (await Element.Locator("[data-part=item]").AllTextContentsAsync()).Select(text => text.Trim())];

    /// <summary>Presses the link of this message, the first when two fields show the same one, and returns once focus
    /// has reached the field.</summary>
    public async Task FollowAsync(string message)
    {
        var link = Element.Locator("[data-part=link]").GetByText(message, new() { Exact = true }).First;
        var target = (await link.GetAttributeAsync("href"))![1..];
        await link.ClickAsync();
        // Focus lands on the field's control, which for a group sits inside the element the link points at.
        await scope.Page.WaitForFunctionAsync("""
            id => {
                const target = document.getElementById(id);
                return target !== null && target.contains(document.activeElement);
            }
            """, target);
    }
}
