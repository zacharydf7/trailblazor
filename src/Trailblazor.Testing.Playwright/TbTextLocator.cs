using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbText</c> with <c>ShowFullText</c> or <c>MaxLength</c>, found by its whole text.</summary>
public sealed class TbTextLocator(ILocator scope, string text)
{
    /// <summary>The text's element.</summary>
    public ILocator Element => scope.Locator(".tb-text").Filter(new()
    {
        Has = scope.Page.Locator("xpath=./*[@data-part='full-text']", new() { HasTextRegex = Whole(text) }),
    });

    // The text as the markup may hold it, with line breaks and indents between its words.
    private static Regex Whole(string text)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape);
        return new($@"^\s*{string.Join(@"\s+", words)}\s*$");
    }

    /// <summary>Whether the text is cut off, by <c>MaxLength</c> as it renders or by <c>MaxLines</c> once its script
    /// has measured its box. On an interactive page this waits for the first measurement; after the box or the text
    /// changes, wait for <c>data-cut</c> with <c>Expect</c> instead.</summary>
    public async Task<bool> IsCutAsync()
    {
        if (await Element.GetAttributeAsync("data-measures") is not null)
        {
            await Assertions.Expect(Element).ToHaveAttributeAsync("data-ready", "");
        }

        return await Element.GetAttributeAsync("data-cut") is not null;
    }

    /// <summary>The whole text, which screen readers read and which the tooltip shows when the text is cut off.
    /// </summary>
    public async Task<string> FullTextAsync() => string.Join(' ',
        (await Element.Locator(":scope > [data-part=full-text]").TextContentAsync() ?? "")
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
