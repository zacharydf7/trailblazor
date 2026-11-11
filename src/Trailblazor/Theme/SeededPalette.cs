using System.Globalization;
using System.Text;

namespace Trailblazor;

// A palette built from seeds the way the built-in palettes are made: grays from the neutral seed on the same steps as
// the stylesheet's, mapped to the same tokens. The primary's and secondary's states stay derived in CSS.
internal sealed class SeededPalette
{
    // The stylesheet's gray scale: each step's lightness, and its chroma for a neutral of chroma GrayChroma.
    private static readonly Dictionary<int, (double L, double C)> Grays = new()
    {
        [50] = (0.975, 0.003), [100] = (0.945, 0.005), [200] = (0.89, 0.008), [300] = (0.81, 0.012),
        [400] = (0.71, 0.015), [500] = (0.62, 0.017), [600] = (0.52, 0.018), [700] = (0.47, 0.018),
        [800] = (0.4, 0.017), [900] = (0.33, 0.015), [925] = (0.27, 0.014), [950] = (0.25, 0.013),
        [960] = (0.23, 0.012), [975] = (0.19, 0.011), [985] = (0.16, 0.01),
    };

    private const double GrayChroma = 0.018;
    private const double TextMinimum = 4.5;
    private const double NonTextMinimum = 3;
    private static readonly OklchColor DefaultNeutral = new(0.47, GrayChroma, 260);

    private readonly OklchColor _neutral;

    private SeededPalette(string name, PaletteSeeds seeds)
    {
        Name = name;
        _neutral = Parse(seeds.Neutral) ?? DefaultNeutral;
        var primary = Parse(seeds.Primary)!.Value;
        var white = OklchColor.White;
        List<Token> tokens =
        [
            new("--tb-color-background", white, Gray(975)),
            new("--tb-color-surface", white, Gray(960)),
            new("--tb-color-surface-raised", white, Gray(925)),
            new("--tb-color-surface-sunken", Gray(50), Gray(985)),
            new("--tb-color-text", Gray(900), Gray(50)),
            new("--tb-color-text-muted", Gray(600), Gray(300)),
            new("--tb-color-text-disabled", Gray(400), Gray(600)),
            new("--tb-border-subtle", Gray(200), Gray(800)),
            new("--tb-border-strong", Gray(500), Gray(400)),
            .. Accent("primary", primary, Parse(seeds.PrimaryDark) ?? Lighter(primary)),
            new("--tb-color-neutral", Gray(700), Gray(300)),
            new("--tb-color-neutral-hover", Gray(800), Gray(200)),
            new("--tb-color-neutral-active", Gray(900), Gray(100)),
            new("--tb-color-on-neutral", white, Gray(950)),
            new("--tb-color-neutral-subtle", Gray(100), Gray(800)),
            new("--tb-color-neutral-text", Gray(700), Gray(300)),
        ];
        if (Parse(seeds.Secondary) is { } secondary)
        {
            tokens.AddRange(Accent("secondary", secondary, Parse(seeds.SecondaryDark) ?? Lighter(secondary)));
        }

        Tokens = tokens;
    }

    public string Name { get; }

    public IReadOnlyList<Token> Tokens { get; }

    public static SeededPalette Build(Palette palette) =>
        new(palette.Name, palette.Seeds ?? throw new ArgumentException("The palette has no seeds.", nameof(palette)));

    // The palette's rule, in the layer of the built-in palettes, where it outranks the default one on the root. The layer
    // order comes first, as the stylesheet states it, so a style placed before the stylesheet can't reorder the layers.
    public string ToCss()
    {
        var css = new StringBuilder()
            .Append("@layer tb.reset, tb.tokens, tb.themes, tb.components, tb.utilities;\n")
            .Append("@layer tb.themes {\n")
            .Append(CultureInfo.InvariantCulture, $"    [data-tb-palette=\"{CssString(Name)}\"] {{\n");
        foreach (var token in Tokens)
        {
            css.Append(CultureInfo.InvariantCulture,
                $"        {token.Name}: light-dark({token.Light.ToCss()}, {token.Dark.ToCss()});\n");
        }

        return css.Append("    }\n}\n").ToString();
    }

    // Each text pair under 4.5:1 and each non-text pair under 3:1, in either mode, with the states CSS derives.
    public IEnumerable<ContrastProblem> ContrastProblems()
    {
        foreach (var dark in new[] { false, true })
        {
            var colors = Tokens.ToDictionary(token => token.Name, token => dark ? token.Dark : token.Light);
            foreach (var accent in new[] { "primary", "secondary" }.Where(accent => colors.ContainsKey(Color(accent))))
            {
                foreach (var (state, color) in States(colors[Color(accent)], dark))
                {
                    colors[Color($"{accent}-{state}")] = color;
                }
            }

            var primary = colors[Color("primary")];
            colors["--tb-focus-ring-color"] = dark ? primary.With(Math.Max(primary.L, 0.81), primary.C * 0.72)
                : primary.With(Math.Min(primary.L, 0.55));
            foreach (var (foreground, background, minimum) in Pairs(colors))
            {
                var ratio = colors[foreground].ContrastWith(colors[background]);
                if (ratio < minimum)
                {
                    // Cut, not rounded, so a ratio just under the minimum never reads as the minimum itself.
                    yield return new(Name, dark ? "dark" : "light", foreground, background,
                        Math.Floor(ratio * 100) / 100, minimum);
                }
            }
        }
    }

    private static IEnumerable<(string Foreground, string Background, double Minimum)> Pairs(
        Dictionary<string, OklchColor> colors)
    {
        foreach (var surface in new[] { "background", "surface", "surface-raised", "surface-sunken" })
        {
            yield return (Color("text"), Color(surface), TextMinimum);
        }

        yield return (Color("text-muted"), Color("background"), TextMinimum);
        yield return (Color("text-muted"), Color("surface"), TextMinimum);
        foreach (var tone in new[] { "primary", "secondary", "neutral" }.Where(tone => colors.ContainsKey(Color(tone))))
        {
            foreach (var fill in new[] { tone, $"{tone}-hover", $"{tone}-active" })
            {
                yield return (Color($"on-{tone}"), Color(fill), TextMinimum);
            }

            yield return (Color($"{tone}-text"), Color("background"), TextMinimum);
            yield return (Color($"{tone}-text"), Color($"{tone}-subtle"), TextMinimum);
        }

        yield return ("--tb-border-strong", Color("background"), NonTextMinimum);
        yield return (Color("primary"), Color("background"), NonTextMinimum);
        yield return ("--tb-focus-ring-color", Color("background"), NonTextMinimum);
    }

    // The same steps as the stylesheet's rules for a primary's hover, press, subtle fill and text.
    private static IEnumerable<(string State, OklchColor Color)> States(OklchColor color, bool dark)
    {
        var (l, c) = (color.L, color.C);
        var nearBlack = Math.Clamp((0.2 - l) * 1000, 0, 1);
        var nearWhite = Math.Clamp((l - 0.79) * 1000, 0, 1);
        var colorful = Math.Min(c / 0.05, 1);
        yield return ("hover", dark ? color.With(l + 0.1 - 0.16 * nearWhite, c * 0.72)
            : color.With(l - 0.06 + 0.12 * nearBlack, c * 0.92));
        yield return ("active", dark ? color.With(l + 0.18 - 0.32 * nearWhite, c * 0.48)
            : color.With(l - 0.12 + 0.24 * nearBlack, c * 0.78));
        yield return ("subtle", dark ? color.With(0.47 - 0.14 * colorful, c * 0.75)
            : color.With(0.89 + 0.055 * colorful, c * 0.3));
        yield return ("text", dark ? color.With(Math.Max(l, 0.81), c * 0.72) : color.With(Math.Min(l, 0.47), c * 0.92));
    }

    private IEnumerable<Token> Accent(string name, OklchColor light, OklchColor dark) =>
    [
        new(Color(name), light, dark),
        new(Color($"on-{name}"), OnColor(light), OnColor(dark)),
    ];

    // White or near-black, whichever reads better on the color.
    private OklchColor OnColor(OklchColor color) =>
        OklchColor.White.ContrastWith(color) >= Gray(950).ContrastWith(color) ? OklchColor.White : Gray(950);

    // A dark-mode form, as the built-in palettes step from their 600 shade to their 400 shade.
    private static OklchColor Lighter(OklchColor color) =>
        color.With(Math.Clamp(color.L + 0.19, 0.65, 0.85), color.C * 0.82);

    private OklchColor Gray(int step) =>
        new(Grays[step].L, Grays[step].C * _neutral.C / GrayChroma, _neutral.H);

    private static string Color(string name) => $"--tb-color-{name}";

    private static OklchColor? Parse(string? value) => OklchColor.TryParse(value, out var color) ? color : null;

    // A palette name inside a quoted CSS string, which also can't end the style element it sits in.
    private static string CssString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("<", "\\3C ").Replace("\n", "\\A ");

    internal sealed record Token(string Name, OklchColor Light, OklchColor Dark);

    internal sealed record ContrastProblem(string Palette, string Mode, string Foreground, string Background,
        double Ratio, double Minimum);
}
