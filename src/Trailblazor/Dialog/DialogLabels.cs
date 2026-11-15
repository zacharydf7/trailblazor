namespace Trailblazor;

/// <summary>The words a dialog's header buttons and its Move and Resize menus show and say to screen readers, for
/// <see cref="TbDialog"/>, <see cref="TbDrawer"/> or <see cref="DialogOptions.Labels"/>.</summary>
public sealed record DialogLabels
{
    /// <summary>Names the header's close button. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogClose"/>.</summary>
    public string? Close { get; init; }

    /// <summary>Names the header's minimize button. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogMinimize"/>.</summary>
    public string? Minimize { get; init; }

    /// <summary>Names the header's maximize button. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogMaximize"/>.</summary>
    public string? Maximize { get; init; }

    /// <summary>Names the minimize or maximize button while it restores the dialog. Leave it null for the app's text
    /// for <see cref="TbStrings.DialogRestore"/>.</summary>
    public string? Restore { get; init; }

    /// <summary>Names the Move button and its menu. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogMove"/>.</summary>
    public string? Move { get; init; }

    /// <summary>Describes the Move button, telling people the arrow keys move the dialog. Leave it null for the app's
    /// text for <see cref="TbStrings.DialogMoveHint"/>.</summary>
    public string? MoveHint { get; init; }

    /// <summary>Names the Resize button and its menu. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogResize"/>.</summary>
    public string? Resize { get; init; }

    /// <summary>Describes the Resize button, telling people the arrow keys resize the dialog. Leave it null for the
    /// app's text for <see cref="TbStrings.DialogResizeHint"/>.</summary>
    public string? ResizeHint { get; init; }

    /// <summary>Names the Move menu's center position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionCenter"/>.</summary>
    public string? PositionCenter { get; init; }

    /// <summary>Names the Move menu's top position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionTop"/>.</summary>
    public string? PositionTop { get; init; }

    /// <summary>Names the Move menu's bottom position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionBottom"/>.</summary>
    public string? PositionBottom { get; init; }

    /// <summary>Names the Move menu's start position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionStart"/>.</summary>
    public string? PositionStart { get; init; }

    /// <summary>Names the Move menu's end position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionEnd"/>.</summary>
    public string? PositionEnd { get; init; }

    /// <summary>Names the Move menu's top start position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionTopStart"/>.</summary>
    public string? PositionTopStart { get; init; }

    /// <summary>Names the Move menu's top end position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionTopEnd"/>.</summary>
    public string? PositionTopEnd { get; init; }

    /// <summary>Names the Move menu's bottom start position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionBottomStart"/>.</summary>
    public string? PositionBottomStart { get; init; }

    /// <summary>Names the Move menu's bottom end position. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogPositionBottomEnd"/>.</summary>
    public string? PositionBottomEnd { get; init; }

    /// <summary>Names the Resize menu's small size. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogSizeSmall"/>.</summary>
    public string? SizeSmall { get; init; }

    /// <summary>Names the Resize menu's medium size. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogSizeMedium"/>.</summary>
    public string? SizeMedium { get; init; }

    /// <summary>Names the Resize menu's large size. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogSizeLarge"/>.</summary>
    public string? SizeLarge { get; init; }

    /// <summary>Names the Resize menu's extra large size. Leave it null for the app's text for
    /// <see cref="TbStrings.DialogSizeExtraLarge"/>.</summary>
    public string? SizeExtraLarge { get; init; }
}
