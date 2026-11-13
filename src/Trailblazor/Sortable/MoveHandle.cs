using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// Another list an item's handle can move it to, as its menu offers it. Its menu item carries the list's label in
// data-list, so the testing helpers can find it whatever the app's labels call the move.
internal sealed record MoveTarget(string Label, string List, bool Disabled, Func<MouseEventArgs, Task> Move);

// An item's move handle: a menu button that a drag starts from, whose arrow keys move the item while it has focus,
// and whose menu moves it with one click or tap, and in a screen reader's browse mode, which keeps the arrow keys for
// itself. The other lists are read as the menu draws its items, since lists join their group as they first render.
internal static class MoveHandle
{
    public static void Add(RenderTreeBuilder builder, int sequence, string label, int index, int count, bool disabled,
        SortableLabels labels, ITbLocalizer localizer, Func<int, MouseEventArgs, Task> moveTo,
        Func<IReadOnlyList<MoveTarget>>? targets = null, string? hint = null)
    {
        builder.OpenComponent<TbMenu>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbMenu.AriaLabel), label);
        builder.AddComponentParameter(sequence + 2, nameof(TbMenu.Icon), BuiltInIcons.DragHandle);
        builder.AddComponentParameter(sequence + 3, nameof(TbMenu.Description),
            labels.Hint ?? hint ?? localizer[TbStrings.SortableHint]);
        builder.AddComponentParameter(sequence + 4, nameof(TbMenu.Size), Size.Small);
        builder.AddComponentParameter(sequence + 5, nameof(TbMenu.Disabled), disabled);
        builder.AddComponentParameter(sequence + 6, nameof(TbMenu.Class), "tb-sortable__handle");
        builder.AddComponentParameter(sequence + 7, nameof(TbMenu.AttachOnFirstUse), true);
        builder.AddAttribute(sequence + 8, "data-part", "handle");
        builder.AddComponentParameter(sequence + 9, nameof(TbMenu.ChildContent), (RenderFragment)(items =>
        {
            AddItem(items, 0, labels.MoveUp ?? localizer[TbStrings.SortableMoveUp], index - 1);
            AddItem(items, 10, labels.MoveDown ?? localizer[TbStrings.SortableMoveDown], index + 1);
            AddItem(items, 20, labels.MoveToStart ?? localizer[TbStrings.SortableMoveToStart], 0);
            AddItem(items, 30, labels.MoveToEnd ?? localizer[TbStrings.SortableMoveToEnd], count - 1);
            foreach (var target in targets?.Invoke() ?? [])
            {
                items.OpenComponent<TbMenuItem>(40);
                items.AddComponentParameter(41, nameof(TbMenuItem.Disabled), target.Disabled);
                items.AddComponentParameter(42, nameof(TbMenuItem.OnClick),
                    EventCallback.Factory.Create<MouseEventArgs>(builder, target.Move));
                items.AddComponentParameter(43, nameof(TbMenuItem.ChildContent), (RenderFragment)(content =>
                    content.AddContent(0, target.Label)));
                items.AddAttribute(44, "data-list", target.List);
                items.CloseComponent();
            }
        }));
        builder.CloseComponent();

        void AddItem(RenderTreeBuilder items, int at, string text, int to)
        {
            items.OpenComponent<TbMenuItem>(at);
            items.AddComponentParameter(at + 1, nameof(TbMenuItem.Disabled), to < 0 || to >= count || to == index);
            items.AddComponentParameter(at + 2, nameof(TbMenuItem.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(builder, args => moveTo(to, args)));
            items.AddComponentParameter(at + 3, nameof(TbMenuItem.ChildContent), (RenderFragment)(content =>
                content.AddContent(0, text)));
            items.CloseComponent();
        }
    }
}
