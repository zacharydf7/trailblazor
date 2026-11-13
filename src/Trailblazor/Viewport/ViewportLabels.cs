namespace Trailblazor;

/// <summary>The words a <see cref="TbViewport"/>'s controls show and say to screen readers.</summary>
public sealed record ViewportLabels
{
    /// <summary>Names the button that zooms in. Leave it null for the app's text for
    /// <see cref="TbStrings.ViewportZoomIn"/>.</summary>
    public string? ZoomIn { get; init; }

    /// <summary>Names the button that zooms out. Leave it null for the app's text for
    /// <see cref="TbStrings.ViewportZoomOut"/>.</summary>
    public string? ZoomOut { get; init; }

    /// <summary>Names the zoom menu and is read out as the zoom changes from the keys, the buttons or the menu, with <c>{0}</c>
    /// for the zoom, such as "125%". Leave it null for the app's text for <see cref="TbStrings.ViewportZoom"/>.
    /// </summary>
    public string? Zoom { get; init; }

    /// <summary>The menu item that fits the content's width. Leave it null for the app's text for
    /// <see cref="TbStrings.ViewportFitWidth"/>.</summary>
    public string? FitWidth { get; init; }

    /// <summary>The menu item that fits the whole content. Leave it null for the app's text for
    /// <see cref="TbStrings.ViewportFitPage"/>.</summary>
    public string? FitPage { get; init; }

    /// <summary>The menu item that shows the content at 100%. Leave it null for the app's text for
    /// <see cref="TbStrings.ViewportActualSize"/>.</summary>
    public string? ActualSize { get; init; }
}
