using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// TbRoot's mark, set once its hosts run interactively; data-tb-session lets an enhanced navigation take it off.
internal sealed class SessionMark : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RendererInfo.IsInteractive)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "hidden", true);
            builder.AddAttribute(2, "data-tb-interactive", "");
            builder.AddAttribute(3, "data-tb-session", "");
            builder.CloseElement();
        }
    }
}
