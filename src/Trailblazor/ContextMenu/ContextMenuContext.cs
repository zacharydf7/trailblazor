namespace Trailblazor;

/// <summary>What a <see cref="TbContextMenu"/>'s items are drawn for.</summary>
public sealed record ContextMenuContext
{
    /// <summary>The <c>data-context-key</c> of the element the menu opened on, such as a row's id, or null when it opened
    /// on the rest of the target or hasn't opened yet.</summary>
    public string? Key { get; init; }
}
