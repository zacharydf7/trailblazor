namespace Trailblazor;

/// <summary>The edge of the window a <see cref="TbDrawer"/> slides in from. Start and End follow the reading
/// direction.</summary>
public enum DrawerSide
{
    /// <summary>The start edge: the left in left-to-right text.</summary>
    Start,

    /// <summary>The end edge: the right in left-to-right text.</summary>
    End,

    /// <summary>The top edge, as a sheet across the window.</summary>
    Top,

    /// <summary>The bottom edge, as a sheet across the window, the way phones show a set of choices.</summary>
    Bottom,
}
