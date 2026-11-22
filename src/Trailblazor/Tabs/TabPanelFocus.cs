namespace Trailblazor;

/// <summary>Whether a showing panel of a <see cref="TbTabs"/> or a <see cref="TbDockManager"/> is a tab stop of
/// its own.</summary>
public enum TabPanelFocus
{
    /// <summary>The panel is a tab stop unless it starts with a control, which the Tab key reaches anyway. The
    /// library's script makes that check: for <see cref="TbTabs"/> on a static page too, and for
    /// <see cref="TbDockManager"/> once the page is interactive. Until then, every showing panel is a tab stop.
    /// </summary>
    Auto,

    /// <summary>The panel is a tab stop, whatever it holds.</summary>
    Always,

    /// <summary>The panel is no tab stop. Use it only when every panel starts with a control.</summary>
    Never,
}
