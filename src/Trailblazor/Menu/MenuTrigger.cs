namespace Trailblazor;

/// <summary>What a custom <see cref="TbMenu"/> trigger needs.</summary>
public sealed record MenuTrigger
{
    /// <summary>The attributes for the button that opens the menu: its id (the id given to the
    /// <see cref="TbMenu"/>, if any), popovertarget, ARIA state and, when the menu is disabled, <c>disabled</c>. Put
    /// them on it as <c>@attributes="context.Attributes"</c>.</summary>
    public required IReadOnlyDictionary<string, object> Attributes { get; init; }
}
