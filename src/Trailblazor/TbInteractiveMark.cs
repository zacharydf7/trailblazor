using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Marks the page with <c>data-tb-interactive</c> once this component renders interactively, which
/// <c>page.Tb().InteractiveAsync()</c> waits for. When interactivity is set page by page, put one inside the part of
/// each page that runs interactively: after an enhanced navigation, <c>TbRoot</c>'s own mark comes off, and this one
/// tells a test that its part of the new page is ready.</summary>
public sealed class TbInteractiveMark : ComponentBase
{
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RendererInfo.IsInteractive)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "hidden", true);
            builder.AddAttribute(2, "data-tb-interactive", "");
            builder.CloseElement();
        }
    }
}
