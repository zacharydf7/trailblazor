namespace Trailblazor;

/// <summary>The words a <see cref="TbColorPicker"/> shows and says to screen readers.</summary>
public sealed record ColorPickerLabels
{
    /// <summary>Names the button that opens the picker, and titles the picker. Leave it null for the app's text for
    /// <see cref="TbStrings.ColorPickerOpen"/>.</summary>
    public string? Open { get; init; }

    /// <summary>Names the eyedropper button. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerEyedropper"/>.</summary>
    public string? Eyedropper { get; init; }

    /// <summary>Names the button that clears the color. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>Names the group of swatches. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerSwatches"/>.</summary>
    public string? Swatches { get; init; }

    /// <summary>Names the hue slider. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerHue"/>.</summary>
    public string? Hue { get; init; }

    /// <summary>Names the saturation slider. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerSaturation"/>.</summary>
    public string? Saturation { get; init; }

    /// <summary>Names the brightness slider. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerBrightness"/>.</summary>
    public string? Brightness { get; init; }

    /// <summary>Names the opacity slider. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerAlpha"/>.</summary>
    public string? Alpha { get; init; }

    /// <summary>Names the red box. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerRed"/>.</summary>
    public string? Red { get; init; }

    /// <summary>Names the green box. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerGreen"/>.</summary>
    public string? Green { get; init; }

    /// <summary>Names the blue box. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerBlue"/>.</summary>
    public string? Blue { get; init; }

    /// <summary>The message for a typed color that can't be read. Leave it null for the app's text for <see
    /// cref="TbStrings.ColorPickerBadColor"/>.</summary>
    public string? BadColor { get; init; }
}
