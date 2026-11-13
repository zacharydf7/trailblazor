namespace Trailblazor;

/// <summary>How a <see cref="TbViewport"/> sets its zoom from the size of its box and of its content.</summary>
public enum ViewportFit
{
    /// <summary>The zoom stays as set, by <see cref="TbViewport.Zoom"/> or by the user.</summary>
    None,

    /// <summary>The content's width fills the box's width, and the content scrolls up and down.</summary>
    Width,

    /// <summary>The whole content shows in the box, as large as fits.</summary>
    Page,
}
