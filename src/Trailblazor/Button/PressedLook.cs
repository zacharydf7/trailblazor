namespace Trailblazor;

/// <summary>How a pressed toggle button shows that it's on.</summary>
public enum PressedLook
{
    /// <summary>The variant's own look: a solid fill, an inner ring on a filled button, or an edge on a text one.
    /// </summary>
    Default,

    /// <summary>A tint with a bar in the text color along the bottom, for toggles that sit among other content, such
    /// as on a tree row or in a toolbar. The bar stands out from the tint and the page by at least 3:1.</summary>
    Quiet,
}
