namespace Trailblazor;

/// <summary>The words a <see cref="TbColorPicker"/> shows and says to screen readers.</summary>
public sealed record ColorPickerLabels
{
    /// <summary>Names the button that opens the picker, and titles the picker. Defaults to "Choose a color".</summary>
    public string Open { get; init; } = "Choose a color";

    /// <summary>Names the eyedropper button. Defaults to "Pick a color from the screen".</summary>
    public string Eyedropper { get; init; } = "Pick a color from the screen";

    /// <summary>Names the button that clears the color. Defaults to "Clear".</summary>
    public string Clear { get; init; } = "Clear";

    /// <summary>Names the group of swatches. Defaults to "Swatches".</summary>
    public string Swatches { get; init; } = "Swatches";

    /// <summary>Names the hue slider. Defaults to "Hue".</summary>
    public string Hue { get; init; } = "Hue";

    /// <summary>Names the saturation slider. Defaults to "Saturation".</summary>
    public string Saturation { get; init; } = "Saturation";

    /// <summary>Names the brightness slider. Defaults to "Brightness".</summary>
    public string Brightness { get; init; } = "Brightness";

    /// <summary>Names the opacity slider. Defaults to "Opacity".</summary>
    public string Alpha { get; init; } = "Opacity";

    /// <summary>Names the red box. Defaults to "Red".</summary>
    public string Red { get; init; } = "Red";

    /// <summary>Names the green box. Defaults to "Green".</summary>
    public string Green { get; init; } = "Green";

    /// <summary>Names the blue box. Defaults to "Blue".</summary>
    public string Blue { get; init; } = "Blue";

    /// <summary>The message for a typed color that can't be read. Defaults to "Enter a color as #RRGGBB.".</summary>
    public string BadColor { get; init; } = "Enter a color as #RRGGBB.";
}
