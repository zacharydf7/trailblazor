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
        ITbLocalizer localizer)
    {
        var labels = dialog.Options.Labels ?? new();
        AddMenu(builder, sequence, dialog, labels.Move ?? localizer[TbStrings.DialogMove], BuiltInIcons.Move,
            labels.MoveHint ?? localizer[TbStrings.DialogMoveHint], "move", null, Enum.GetValues<DialogPosition>(),
            position => PositionName(position, labels, localizer), position => dialog.Position = position);
    }

    public static void AddResize(RenderTreeBuilder builder, int sequence, DialogReference dialog,
        ITbLocalizer localizer)
    {
        var labels = dialog.Options.Labels ?? new();
        AddMenu(builder, sequence, dialog, labels.Resize ?? localizer[TbStrings.DialogResize], BuiltInIcons.ResizeGrip,
            labels.ResizeHint ?? localizer[TbStrings.DialogResizeHint], "resize", "tb-dialog__resize", Sizes,
            size => SizeName(size, labels, localizer), size => dialog.Size = size);
    }

    private static void AddMenu<T>(RenderTreeBuilder builder, int sequence, DialogReference dialog, string label,
        Icon icon, string description, string part, string? cssClass, IEnumerable<T> choices, Func<T, string> name,
        Action<T> choose)
        where T : struct, Enum
    {
        builder.OpenComponent<TbMenu>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbMenu.AriaLabel), label);
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

    private static string PositionName(DialogPosition position, DialogLabels labels, ITbLocalizer localizer) =>
        position switch
        {
            DialogPosition.Center => labels.PositionCenter ?? localizer[TbStrings.DialogPositionCenter],
            DialogPosition.Top => labels.PositionTop ?? localizer[TbStrings.DialogPositionTop],
            DialogPosition.Bottom => labels.PositionBottom ?? localizer[TbStrings.DialogPositionBottom],
            DialogPosition.Start => labels.PositionStart ?? localizer[TbStrings.DialogPositionStart],
            DialogPosition.End => labels.PositionEnd ?? localizer[TbStrings.DialogPositionEnd],
            DialogPosition.TopStart => labels.PositionTopStart ?? localizer[TbStrings.DialogPositionTopStart],
            DialogPosition.TopEnd => labels.PositionTopEnd ?? localizer[TbStrings.DialogPositionTopEnd],
            DialogPosition.BottomStart => labels.PositionBottomStart ?? localizer[TbStrings.DialogPositionBottomStart],
            DialogPosition.BottomEnd => labels.PositionBottomEnd ?? localizer[TbStrings.DialogPositionBottomEnd],
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
        };

    private static string SizeName(DialogSize size, DialogLabels labels, ITbLocalizer localizer) => size switch
    {
        DialogSize.Small => labels.SizeSmall ?? localizer[TbStrings.DialogSizeSmall],
        DialogSize.Medium => labels.SizeMedium ?? localizer[TbStrings.DialogSizeMedium],
        DialogSize.Large => labels.SizeLarge ?? localizer[TbStrings.DialogSizeLarge],
        DialogSize.ExtraLarge => labels.SizeExtraLarge ?? localizer[TbStrings.DialogSizeExtraLarge],
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };
}
