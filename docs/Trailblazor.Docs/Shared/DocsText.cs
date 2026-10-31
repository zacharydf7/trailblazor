using Microsoft.AspNetCore.Components;

namespace Trailblazor.Docs.Shared;

// Notes written as plain text, as the example catalog keeps them.
public static class DocsText
{
    // Every other piece of the text sits between backticks and shows as code.
    public static RenderFragment WithCode(string text) => builder =>
    {
        var pieces = text.Split('`');
        for (var index = 0; index < pieces.Length; index++)
        {
            if (index % 2 == 1)
            {
                builder.OpenElement(0, "code");
                builder.AddContent(1, pieces[index]);
                builder.CloseElement();
            }
            else
            {
                builder.AddContent(2, pieces[index]);
            }
        }
    };
}
