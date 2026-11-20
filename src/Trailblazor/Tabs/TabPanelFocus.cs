namespace Trailblazor;

/// <summary>Whether the selected tab's panel is a tab stop of its own.</summary>
public enum TabPanelFocus
{
    /// <summary>The panel is a tab stop unless it starts with a control, which the Tab key reaches anyway. The
    /// library's script makes that check, on a static page too. Until the script has loaded, every shown panel is a
    /// tab stop.</summary>
    Auto,

    /// <summary>The panel is a tab stop, whatever it holds.</summary>
    Always,

    /// <summary>The panel is no tab stop. Use it only when every panel starts with a control.</summary>
    Never,
}
