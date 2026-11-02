namespace Trailblazor;

/// <summary>What a custom <see cref="TbPopover"/> trigger needs.</summary>
public sealed record PopoverTrigger
{
    /// <summary>The attributes for the button that opens the popover: its id (the id given to the
    /// <see cref="TbPopover"/>, if any), popovertarget and ARIA state. Put them on it as
    /// <c>@attributes="context.Attributes"</c>.</summary>
    public required IReadOnlyDictionary<string, object> Attributes { get; init; }
}
