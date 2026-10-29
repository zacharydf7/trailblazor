namespace Trailblazor;

/// <summary>Whether moving focus between tabs with the arrow keys also selects them.</summary>
public enum TabActivation
{
    /// <summary>The arrow keys select each tab they reach.</summary>
    Automatic,

    /// <summary>The arrow keys only move focus; Enter or Space selects. Use it when showing a panel is slow.</summary>
    Manual,
}
