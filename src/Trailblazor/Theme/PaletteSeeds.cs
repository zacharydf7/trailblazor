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
    private readonly string? _success;
    private readonly string? _successDark;
    private readonly string? _warning;
    private readonly string? _warningDark;
    private readonly string? _danger;
    private readonly string? _dangerDark;
    private readonly string? _info;
    private readonly string? _infoDark;

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

    /// <summary>The color of success in light mode, such as a step done or a check passed, for
    /// <see cref="Tone.Success"/>. Defaults to the stylesheet's green.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? Success { get => _success; init => _success = Checked(value); }

    /// <summary>The success color in dark mode. Defaults to a lighter form of <see cref="Success"/>.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? SuccessDark { get => _successDark; init => _successDark = Checked(value); }

    /// <summary>The color of a warning in light mode, such as something due soon, for <see cref="Tone.Warning"/>.
    /// Defaults to the stylesheet's amber. Choose a light color that takes dark text, as amber does.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? Warning { get => _warning; init => _warning = Checked(value); }

    /// <summary>The warning color in dark mode. Defaults to <see cref="Warning"/>, which takes dark text in both
    /// modes.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? WarningDark { get => _warningDark; init => _warningDark = Checked(value); }

    /// <summary>The color of danger in light mode, such as an error or an action that deletes, for
    /// <see cref="Tone.Danger"/>. Defaults to the stylesheet's red.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? Danger { get => _danger; init => _danger = Checked(value); }

    /// <summary>The danger color in dark mode. Defaults to a lighter form of <see cref="Danger"/>.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? DangerDark { get => _dangerDark; init => _dangerDark = Checked(value); }

    /// <summary>The color of information in light mode, such as a tip or a notice, for <see cref="Tone.Info"/>.
    /// Defaults to the stylesheet's sky blue.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? Info { get => _info; init => _info = Checked(value); }

    /// <summary>The info color in dark mode. Defaults to a lighter form of <see cref="Info"/>.</summary>
    /// <exception cref="ArgumentException">The value isn't a hex or <c>oklch()</c> color.</exception>
    public string? InfoDark { get => _infoDark; init => _infoDark = Checked(value); }

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
