using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// Renders content inside the dock only when its version changes. The dock renders on every layout change, and
// content handed down as a fragment would otherwise render again each time.
internal sealed class DockContent : ComponentBase
{
    private int _rendered;

    [Parameter, EditorRequired]
    public int Version { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override bool ShouldRender() => _rendered != Version;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _rendered = Version;
        builder.AddContent(0, ChildContent);
    }
}
