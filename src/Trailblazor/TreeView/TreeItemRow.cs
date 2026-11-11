using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// A row drawn from Items that draws again only when its state changes. The tree builds the row's markup, so events still
// reach the tree.
internal sealed class TreeItemRow : ComponentBase
{
    private TreeRowState _drawn;

    [Parameter]
    public TreeRowState State { get; set; }

    [Parameter]
    public RenderFragment? Content { get; set; }

    protected override bool ShouldRender() => State != _drawn;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _drawn = State;
        builder.AddContent(0, Content);
    }
}

// What a tree row shows that the tree can change by itself, with the row's template context compared by value. The
// version goes up when the tree's parameters are set, RefreshAsync runs or a drop is applied, which covers the rest: the
// items, the templates and the functions that style them.
internal readonly record struct TreeRowState(int Version, int Index, int? Place, int? Inside, bool TabStop, int SetSize,
    int Position, object Context);
