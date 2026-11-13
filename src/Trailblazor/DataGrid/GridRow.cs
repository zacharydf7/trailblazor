using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// A grid row, drawn by the grid's own markup, that draws again only when its item, its selection, its class, its label,
// whether its details are open, its place in a tree, the cell it edits or the grid's rows changed, so a click on one
// row doesn't redraw the rest. C#, so it can stay internal.
internal sealed class GridRow<TItem> : ComponentBase
{
    private TItem _item = default!;
    private bool _selected;
    private string? _class;
    private string? _label;
    private int _version;
    private object? _editing;
    private bool _expanded;
    private TreePlace? _tree;
    private bool _changed;

    [Parameter, EditorRequired]
    public TItem Item { get; set; } = default!;

    [Parameter]
    public bool Selected { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public int Version { get; set; }

    [Parameter]
    public object? Editing { get; set; }

    [Parameter]
    public bool Expanded { get; set; }

    [Parameter]
    public TreePlace? Tree { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    protected override void OnParametersSet()
    {
        _changed = !Same(Item, _item) || Selected != _selected || Class != _class || Label != _label
            || Version != _version || !Equals(Editing, _editing) || Expanded != _expanded || Tree != _tree;
        (_item, _selected, _class, _label, _version, _editing, _expanded, _tree) =
            (Item, Selected, Class, Label, Version, Editing, Expanded, Tree);
    }

    protected override bool ShouldRender() => _changed;

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);

    // An item whose Equals reads only its id can still change, so a class is compared by reference.
    private static bool Same(TItem item, TItem other) => typeof(TItem).IsValueType
        ? EqualityComparer<TItem>.Default.Equals(item, other) : ReferenceEquals(item, other);
}
