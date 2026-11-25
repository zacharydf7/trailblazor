using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// A missing value, as a timestamp or a description list shows it: a dash, which screen readers hear as notSet.
internal static class EmptyValue
{
    public static RenderFragment Of(string notSet) => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "aria-hidden", "true");
        builder.AddContent(2, "—");
        builder.CloseElement();
        builder.OpenElement(3, "span");
        builder.AddAttribute(4, "class", "tb-visually-hidden");
        builder.AddContent(5, notSet);
        builder.CloseElement();
    };
}
