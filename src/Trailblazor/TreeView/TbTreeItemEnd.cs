using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Shows buttons, menus or a count at the end of the tree row it's in. Put it anywhere in a tree's
/// <c>ItemTemplate</c> or a <see cref="TbTreeItem{TItem}"/>'s <see cref="TbTreeItem{TItem}.TextContent"/>, so one
/// component of your own draws a row's text and its controls, with one set of state. Its content shows after the
/// row's <c>ItemEndTemplate</c> or <see cref="TbTreeItem{TItem}.End"/>, and like them, clicks and keys there never
/// select, open or activate the row. Use one per row; with more, only the one rendered last shows.</summary>
public sealed class TbTreeItemEnd : ComponentBase, IDisposable
{
    [CascadingParameter]
    private ContentSlot? Slot { get; set; }

    /// <summary>What the end of the row shows.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() => (Slot ?? throw new InvalidOperationException($"Put "
        + $"{nameof(TbTreeItemEnd)} inside a tree's {nameof(TbTreeView<object, object>.ItemTemplate)} or a "
        + $"{nameof(TbTreeItem<object>)}'s {nameof(TbTreeItem<object>.TextContent)}.")).Set(this, ChildContent);

    /// <inheritdoc />
    public void Dispose() => Slot?.Clear(this);
}
