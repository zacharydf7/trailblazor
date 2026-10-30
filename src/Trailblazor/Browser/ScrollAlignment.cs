namespace Trailblazor;

/// <summary>Where <see cref="IBrowserService.ScrollIntoViewAsync"/> puts the element in its container.</summary>
public enum ScrollAlignment
{
    /// <summary>Scrolls as little as possible, and not at all when the element is already in view.</summary>
    Nearest,

    /// <summary>Lines the element up with the top.</summary>
    Start,

    /// <summary>Puts the element in the middle.</summary>
    Center,

    /// <summary>Lines the element up with the bottom.</summary>
    End,
}
