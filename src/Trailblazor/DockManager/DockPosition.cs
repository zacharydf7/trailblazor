namespace Trailblazor;

/// <summary>Where <see cref="DockLayout.Move"/> puts a panel beside another panel, or
/// <see cref="DockLayout.MoveToEdge"/> along an edge of the dock. Start and End follow the reading direction.
/// </summary>
public enum DockPosition
{
    /// <summary>A tab before the other panel's tab, in its group.</summary>
    Before,

    /// <summary>A tab after the other panel's tab, in its group.</summary>
    After,

    /// <summary>A new group on the side where text starts.</summary>
    Start,

    /// <summary>A new group on the side where text ends.</summary>
    End,

    /// <summary>A new group above.</summary>
    Top,

    /// <summary>A new group below.</summary>
    Bottom,
}
