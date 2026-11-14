using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// Marks the page once TbRoot's hosts run interactively, so a test can wait for that before using the page. It renders
// in the hosts' own render mode, so it appears when the session that serves the interactive pages starts.
internal sealed class InteractiveMark : ComponentBase
{
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
