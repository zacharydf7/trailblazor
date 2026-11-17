namespace Trailblazor;

/// <summary>The words a lightbox shows and says to screen readers. Each one left null uses the app's text.</summary>
public sealed record LightboxLabels
{
    /// <summary>Names the previous button, for the app's text for <see cref="TbStrings.LightboxPrevious"/>.</summary>
    public string? Previous { get; init; }

    /// <summary>Names the next button, for the app's text for <see cref="TbStrings.LightboxNext"/>.</summary>
    public string? Next { get; init; }

    /// <summary>Names the zoom in button, for the app's text for <see cref="TbStrings.LightboxZoomIn"/>.</summary>
    public string? ZoomIn { get; init; }

    /// <summary>Names the zoom out button, for the app's text for <see cref="TbStrings.LightboxZoomOut"/>.</summary>
    public string? ZoomOut { get; init; }

    /// <summary>Names the button that undoes zoom and rotation, for the app's text for
    /// <see cref="TbStrings.LightboxFit"/>.</summary>
    public string? Fit { get; init; }

    /// <summary>Names the button that turns the image counterclockwise, for the app's text for
    /// <see cref="TbStrings.LightboxRotateLeft"/>.</summary>
    public string? RotateLeft { get; init; }

    /// <summary>Names the button that turns the image clockwise, for the app's text for
    /// <see cref="TbStrings.LightboxRotateRight"/>.</summary>
    public string? RotateRight { get; init; }

    /// <summary>Shows which image is shown, with <c>{0}</c> for its number and <c>{1}</c> for the number of images,
    /// for the app's text for <see cref="TbStrings.LightboxPosition"/>.</summary>
    public string? Position { get; init; }

    /// <summary>Read out when another image shows, with <c>{0}</c> for its alt text, <c>{1}</c> for its number and
    /// <c>{2}</c> for the number of images, for the app's text for <see cref="TbStrings.LightboxShown"/>.</summary>
    public string? Shown { get; init; }

    /// <summary>Shows the zoom, with <c>{0}</c> for the percentage, where 100 fits the image to the window, for the
    /// app's text for <see cref="TbStrings.LightboxZoom"/>.</summary>
    public string? Zoom { get; init; }
}
