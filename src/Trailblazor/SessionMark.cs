using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// TbRoot's mark, set once its hosts run interactively. When only the hosts are interactive, data-tb-session says the
// mark speaks for the session and the first page, not for pages an enhanced navigation brings, so it comes off then.
internal sealed class SessionMark : ComponentBase
{
    // True when TbRoot renders statically and gives the hosts its HostRenderMode, as when interactivity is per page.
    [Parameter]
    public bool PerPage { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RendererInfo.IsInteractive)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "hidden", true);
            builder.AddAttribute(2, "data-tb-interactive", "");
            builder.AddAttribute(3, "data-tb-session", PerPage ? "" : null);
            builder.CloseElement();
        }
    }
}
