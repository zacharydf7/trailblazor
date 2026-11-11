using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Trailblazor;

// Writes the app's seeded palette's CSS once, and warns once about any pair of its colors that's hard to read.
internal sealed class SeededPalettes(IOptions<TrailblazorOptions> options, ILogger<SeededPalettes>? logger = null)
{
    private readonly Lazy<string?> _css = new(() => Build(options.Value, logger));

    // The CSS for the palette in the options, or null when it has no seeds.
    public string? Css => _css.Value;

    private static string? Build(TrailblazorOptions options, ILogger? logger)
    {
        if (options.Theme.Palette.Seeds is null)
        {
            return null;
        }

        var seeded = SeededPalette.Build(options.Theme.Palette);
        foreach (var problem in options.WarnAboutContrast ? seeded.ContrastProblems() : [])
        {
            logger?.LogWarning("Palette {Palette} in {Mode} mode: {Foreground} on {Background} has a contrast of "
                + "{Ratio}:1, under the {Minimum}:1 minimum.", problem.Palette, problem.Mode, problem.Foreground,
                problem.Background, problem.Ratio, problem.Minimum);
        }

        return seeded.ToCss();
    }
}
