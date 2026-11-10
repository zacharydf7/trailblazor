using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// An item's move handle: a menu button that a drag starts from, whose arrow keys move the item while it has focus,
// and whose menu moves it with one click or tap, and in a screen reader's browse mode, which keeps the arrow keys for
// itself.
internal static class MoveHandle
{
    public static void Add(RenderTreeBuilder builder, int sequence, string label, int index, int count, bool disabled,
        ITbLocalizer localizer, Func<int, MouseEventArgs, Task> moveTo)
    {
        builder.OpenComponent<TbMenu>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbMenu.AriaLabel), label);
        builder.AddComponentParameter(sequence + 2, nameof(TbMenu.Icon), BuiltInIcons.DragHandle);
        builder.AddComponentParameter(sequence + 3, nameof(TbMenu.Description), localizer[TbStrings.SortableHint]);
        builder.AddComponentParameter(sequence + 4, nameof(TbMenu.Size), Size.Small);
        builder.AddComponentParameter(sequence + 5, nameof(TbMenu.Disabled), disabled);
        builder.AddComponentParameter(sequence + 6, nameof(TbMenu.Class), "tb-sortable__handle");
        builder.AddComponentParameter(sequence + 7, nameof(TbMenu.AttachOnFirstUse), true);
        builder.AddAttribute(sequence + 8, "data-part", "handle");
        builder.AddComponentParameter(sequence + 9, nameof(TbMenu.ChildContent), (RenderFragment)(items =>
        {
            AddItem(items, 0, localizer[TbStrings.SortableMoveUp], index - 1);
            AddItem(items, 10, localizer[TbStrings.SortableMoveDown], index + 1);
            AddItem(items, 20, localizer[TbStrings.SortableMoveToStart], 0);
            AddItem(items, 30, localizer[TbStrings.SortableMoveToEnd], count - 1);
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
