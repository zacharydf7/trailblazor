namespace Trailblazor;

/// <summary>The few colors a palette is built from, so an app needs no palette CSS of its own. Each is a hex color,
/// such as <c>"#1F5FAD"</c>, or an <c>oklch()</c> color. Set them on a <see cref="Palette"/>'s
/// <see cref="Palette.Seeds"/>, and <see cref="TbThemeHead"/> writes the palette's CSS.</summary>
public sealed record PaletteSeeds
{
    private readonly string _primary = "";
    private readonly string? _primaryDark;
    private readonly string? _secondary;
    private readonly string? _secondaryDark;
    private readonly string? _neutral;

    /// <summary>The primary color in light mode, for main actions, selection and focus. Required.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public required string Primary { get => _primary; init => _primary = Checked(value)!; }

    /// <summary>The primary color in dark mode. Defaults to a lighter form of <see cref="Primary"/>.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? PrimaryDark { get => _primaryDark; init => _primaryDark = Checked(value); }

    /// <summary>The secondary color in light mode, for <see cref="Tone.Secondary"/>. Defaults to the stylesheet's
    /// secondary.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? Secondary { get => _secondary; init => _secondary = Checked(value); }

    /// <summary>The secondary color in dark mode. Defaults to a lighter form of <see cref="Secondary"/>.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? SecondaryDark { get => _secondaryDark; init => _secondaryDark = Checked(value); }

    /// <summary>The color the grays are tinted from: their hue, and how strongly they lean toward it. The backgrounds,
    /// text, borders and neutral tone follow from it in both modes. Defaults to the stylesheet's cool gray.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? Neutral { get => _neutral; init => _neutral = Checked(value); }

    private static string? Checked(string? value, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        if (value is not null && !OklchColor.TryParse(value, out _))
        {
            throw new ArgumentException($"{name} must be a hex color, such as \"#1F5FAD\", or an oklch() color, "
                + $"such as \"oklch(0.52 0.17 260)\", not \"{value}\".", name);
        }

        return value;
    }
}
