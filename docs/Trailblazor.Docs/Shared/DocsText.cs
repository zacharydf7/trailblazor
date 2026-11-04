using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Docs.Shared;

// Notes written as plain text, as the example catalog keeps them.
public static partial class DocsText
{
    // Text between backticks shows as code, and [text](path) links to another page, such as
    // [text field](components/text-field).
    public static RenderFragment Render(string text) => builder =>
    {
        var at = 0;
        foreach (Match match in Markup().Matches(text))
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

    [GeneratedRegex(@"`(?<code>[^`]*)`|\[(?<link>[^\]]+)\]\((?<path>[^)\s]+)\)")]
    private static partial Regex Markup();
}
