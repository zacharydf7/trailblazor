namespace Trailblazor;

/// <summary>Where a <see cref="TbFab"/> or <see cref="TbSpeedDial"/> sits. A corner or edge keeps it in view as the page
/// scrolls, clear of a phone's rounded corners and home bar. Start and End follow the reading direction.</summary>
public enum FabPosition
{
    BottomEnd,
    BottomStart,
    BottomCenter,
    TopEnd,
    TopStart,

    /// <summary>Where the layout puts it, such as in a card's corner or an app bar.</summary>
    Inline,
}
