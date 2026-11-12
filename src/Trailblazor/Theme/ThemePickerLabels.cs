namespace Trailblazor;

/// <summary>The words a <see cref="TbThemePicker"/> says to screen readers. The picker's own <c>AriaLabel</c>
/// names it.</summary>
public sealed record ThemePickerLabels
{
    /// <summary>Names the light mode's button. Leave it null for the app's text for
    /// <see cref="TbStrings.ThemeLight"/>.</summary>
    public string? Light { get; init; }

    /// <summary>Names the dark mode's button. Leave it null for the app's text for <see cref="TbStrings.ThemeDark"/>.
    /// </summary>
    public string? Dark { get; init; }

    /// <summary>Names the button that follows the device's mode. Leave it null for the app's text for
    /// <see cref="TbStrings.ThemeSystem"/>.</summary>
    public string? System { get; init; }
}
