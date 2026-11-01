using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// What a TbTreeItem needs from its tree, whatever the tree's key type, so items written as markup take one type.
internal interface ITreeItemHost<TItem>
{
    int InitialExpandDepth { get; }

    object KeyOf(TItem item);

    void AddItem(TbTreeItem<TItem> item, TbTreeItem<TItem>? parent);

    void RemoveItem(TbTreeItem<TItem> item, TbTreeItem<TItem>? parent);

    void ItemChanged();

    RenderFragment? RowOf(TbTreeItem<TItem> item);

    bool IsOpen(TbTreeItem<TItem> item);
}
