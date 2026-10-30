namespace Trailblazor;

/// <summary>How strongly a <see cref="TbAlert"/> stands out from the page.</summary>
public enum AlertVariant
{
    /// <summary>A pale fill in the tone's color with a bar at the start.</summary>
    Soft,

    /// <summary>A solid fill in the tone's color, for messages that must stand out.</summary>
    Filled,

    /// <summary>A border in the tone's color on the page's own background.</summary>
    Outlined,
}
