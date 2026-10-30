using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// The Move and Resize menus. They let a single click or tap place and size a dialog, with no dragging (WCAG 2.5.7),
// and the script moves and resizes it with the arrow keys while their buttons have focus.
internal static class DialogMenus
{
    // Full is left out: it fills the window, which is what Maximize is for.
    private static readonly DialogSize[] Sizes =
        [DialogSize.Small, DialogSize.Medium, DialogSize.Large, DialogSize.ExtraLarge];

    public static void AddMove(RenderTreeBuilder builder, int sequence, DialogReference dialog,
        ITbLocalizer localizer) =>
        AddMenu(builder, sequence, dialog, localizer[TbStrings.DialogMove], BuiltInIcons.Move,
            localizer[TbStrings.DialogMoveHint], "move", null, Enum.GetValues<DialogPosition>(),
            position => localizer[PositionKey(position)], dialog.MoveTo);

    public static void AddResize(RenderTreeBuilder builder, int sequence, DialogReference dialog,
        ITbLocalizer localizer) =>
        AddMenu(builder, sequence, dialog, localizer[TbStrings.DialogResize], BuiltInIcons.ResizeGrip,
            localizer[TbStrings.DialogResizeHint], "resize", "tb-dialog__resize", Sizes,
            size => localizer[SizeKey(size)], dialog.ResizeTo);

    private static void AddMenu<T>(RenderTreeBuilder builder, int sequence, DialogReference dialog, string label,
        Icon icon, string description, string part, string? cssClass, IEnumerable<T> choices, Func<T, string> name,
        Action<T> choose)
        where T : struct, Enum
    {
        builder.OpenComponent<TbMenu>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbMenu.Label), label);
        builder.AddComponentParameter(sequence + 2, nameof(TbMenu.Icon), icon);
        builder.AddComponentParameter(sequence + 3, nameof(TbMenu.Description), description);
        builder.AddComponentParameter(sequence + 4, nameof(TbMenu.Size), Size.Small);
        builder.AddComponentParameter(sequence + 5, nameof(TbMenu.Class), cssClass);
        builder.AddAttribute(sequence + 6, "data-part", part);
        builder.AddComponentParameter(sequence + 7, nameof(TbMenu.ChildContent), (RenderFragment)(items =>
        {
            foreach (var choice in choices)
            {
                items.OpenComponent<TbMenuItem>(0);
                items.SetKey(choice);
                items.AddComponentParameter(1, nameof(TbMenuItem.OnClick),
                    EventCallback.Factory.Create<MouseEventArgs>(dialog, () => choose(choice)));
                items.AddComponentParameter(2, nameof(TbMenuItem.ChildContent), (RenderFragment)(text =>
                    text.AddContent(0, name(choice))));
                items.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }

    private static string PositionKey(DialogPosition position) => position switch
    {
        DialogPosition.Center => TbStrings.DialogPositionCenter,
        DialogPosition.Top => TbStrings.DialogPositionTop,
        DialogPosition.Bottom => TbStrings.DialogPositionBottom,
        DialogPosition.Start => TbStrings.DialogPositionStart,
        DialogPosition.End => TbStrings.DialogPositionEnd,
        DialogPosition.TopStart => TbStrings.DialogPositionTopStart,
        DialogPosition.TopEnd => TbStrings.DialogPositionTopEnd,
        DialogPosition.BottomStart => TbStrings.DialogPositionBottomStart,
        DialogPosition.BottomEnd => TbStrings.DialogPositionBottomEnd,
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
    };

    private static string SizeKey(DialogSize size) => size switch
    {
        DialogSize.Small => TbStrings.DialogSizeSmall,
        DialogSize.Medium => TbStrings.DialogSizeMedium,
        DialogSize.Large => TbStrings.DialogSizeLarge,
        DialogSize.ExtraLarge => TbStrings.DialogSizeExtraLarge,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };
}
