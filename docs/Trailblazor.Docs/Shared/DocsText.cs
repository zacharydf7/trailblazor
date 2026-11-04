using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Docs.Shared;

// Notes written as plain text, as the example catalog keeps them.
public static class DocsText
{
    // Renders the catalog's code as a <code> element and its links as <a> elements, using CatalogMarkup's syntax.
    public static RenderFragment Render(string text) => builder =>
    {
        var at = 0;
        foreach (Match match in CatalogMarkup.Pattern().Matches(text))
        {
            builder.AddContent(0, text[at..match.Index]);
            if (match.Groups["code"].Success)
            {
                builder.OpenElement(1, "code");
                builder.AddContent(2, match.Groups["code"].Value);
                builder.CloseElement();
            }
            else
            {
                builder.OpenElement(3, "a");
                builder.AddAttribute(4, "href", match.Groups["path"].Value);
                builder.AddContent(5, match.Groups["link"].Value);
                builder.CloseElement();
            }

            at = match.Index + match.Length;
        }

        builder.AddContent(6, text[at..]);
    };
}
