namespace Trailblazor;

/// <summary>The words a <see cref="TbSortable{TItem}"/>'s move handles show and say to screen readers.</summary>
public sealed record SortableLabels
{
    /// <summary>Names an item's handle, with <c>{0}</c> for the item's text. Leave it null for the app's text for
    /// <see cref="TbStrings.SortableHandle"/>.</summary>
    public string? Handle { get; init; }

    /// <summary>Describes a handle, telling screen reader users how to move the item. Leave it null for the app's text
    /// for <see cref="TbStrings.SortableHint"/>.</summary>
    public string? Hint { get; init; }

    /// <summary>The handle's menu item that moves the item up one place. Leave it null for the app's text for
    /// <see cref="TbStrings.SortableMoveUp"/>.</summary>
    public string? MoveUp { get; init; }

    /// <summary>The handle's menu item that moves the item down one place. Leave it null for the app's text for
    /// <see cref="TbStrings.SortableMoveDown"/>.</summary>
    public string? MoveDown { get; init; }

    /// <summary>The handle's menu item that moves the item to the start. Leave it null for the app's text for
    /// <see cref="TbStrings.SortableMoveToStart"/>.</summary>
    public string? MoveToStart { get; init; }

    /// <summary>The handle's menu item that moves the item to the end. Leave it null for the app's text for
    /// <see cref="TbStrings.SortableMoveToEnd"/>.</summary>
    public string? MoveToEnd { get; init; }

    /// <summary>Read out after a move, with <c>{0}</c> for the item's text, <c>{1}</c> for its new position and
    /// <c>{2}</c> for the number of items. Leave it null for the app's text for <see cref="TbStrings.SortableMoved"/>.
    /// </summary>
    public string? Moved { get; init; }
}
