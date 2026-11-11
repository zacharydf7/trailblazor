namespace Trailblazor;

/// <summary>A set of colors for the page, with a light and a dark form. Use a built-in palette, such as
/// <see cref="Cobalt"/>, or name your own by the <c>data-tb-palette</c> value its CSS rule matches.</summary>
public sealed record Palette
{
    /// <summary>A palette named by the <c>data-tb-palette</c> value its CSS rule matches, such as <c>"forest"</c> for
    /// <c>[data-tb-palette="forest"]</c>.</summary>
    /// <exception cref="ArgumentException">The name is empty or white space.</exception>
    public Palette(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>The <c>data-tb-palette</c> value that selects the palette.</summary>
    public string Name { get; }

    /// <summary>The colors to build the palette from. When they're set on the palette in
    /// <see cref="TrailblazorOptions.Theme"/>, <see cref="TbThemeHead"/> writes the palette's CSS, so the app needs
    /// none of its own.</summary>
    public PaletteSeeds? Seeds { get; init; }

    /// <summary>Near-monochrome grays with ink-colored primary actions. The default.</summary>
    public static Palette Graphite { get; } = new("graphite");

    /// <summary>Cool grays with blue primary actions.</summary>
    public static Palette Cobalt { get; } = new("cobalt");

    /// <summary>Blue-tinted grays with slate ink for primary actions.</summary>
    public static Palette Slate { get; } = new("slate");

    /// <summary>Warm, earthy grays with brown-black ink for primary actions.</summary>
    public static Palette Sand { get; } = new("sand");

    /// <summary>Cool grays with deep indigo.</summary>
    public static Palette Indigo { get; } = new("indigo");

    /// <summary>Lilac-tinted grays with violet.</summary>
    public static Palette Violet { get; } = new("violet");

    /// <summary>Lilac-tinted grays with a dusky, muted purple.</summary>
    public static Palette Plum { get; } = new("plum");

    /// <summary>Lilac-tinted grays with a vivid, saturated fuchsia.</summary>
    public static Palette Fuchsia { get; } = new("fuchsia");

    /// <summary>Warm grays with rose pink.</summary>
    public static Palette Rose { get; } = new("rose");

    /// <summary>Warm, earthy grays with a muted terracotta.</summary>
    public static Palette Clay { get; } = new("clay");

    /// <summary>Green-tinted grays with an earthy olive green.</summary>
    public static Palette Moss { get; } = new("moss");

    /// <summary>Green-tinted grays with teal.</summary>
    public static Palette Teal { get; } = new("teal");

    /// <summary>Every built-in palette, starting with the default.</summary>
    public static IReadOnlyList<Palette> BuiltIn { get; } =
        [Graphite, Cobalt, Slate, Sand, Indigo, Violet, Plum, Fuchsia, Rose, Clay, Moss, Teal];

    /// <inheritdoc />
    public override string ToString() => Name;
}
