using System.Text.RegularExpressions;

namespace Trailblazor.Docs.Examples;

// The theme tokens a component's CSS reads, found in the library's own stylesheet so the docs can't drift from it.
public static partial class DesignTokens
{
    private static readonly Lazy<IReadOnlyList<(string Selector, string Body)>> Rules = new(LoadRules);

    // The theme declares its tokens outside any component's rules: on :root, a palette or a density.
    private static readonly Lazy<IReadOnlySet<string>> Theme = new(() => Rules.Value
        .Where(rule => !rule.Selector.Contains(".tb-", StringComparison.Ordinal))
        .SelectMany(rule => Declared().Matches(rule.Body)).Select(match => match.Groups[1].Value).ToHashSet());

    // Variables a component sets for itself, such as a dialog's width, are its own workings, not theme tokens.
    public static IReadOnlyList<string> Read(IEnumerable<string> blocks)
    {
        // A block's own rules, its modifiers and its elements: tb-button, tb-button--small, tb-button__label.
        var pattern = new Regex(string.Join('|',
            blocks.Select(block => $@"\.{Regex.Escape(block)}(?:--|__|(?![\w-]))")));
        return [.. Rules.Value.Where(rule => pattern.IsMatch(rule.Selector))
            .SelectMany(rule => Used().Matches(rule.Body)).Select(match => match.Groups[1].Value)
            .Where(Theme.Value.Contains).Distinct().Order(StringComparer.Ordinal)];
    }

    // The reference tier's tonal ramps, such as --tb-gray-50 to --tb-gray-950, in the stylesheet's order.
    public static IReadOnlyList<(string Name, IReadOnlyList<string> Tokens)> Ramps() =>
        [.. Rules.Value.SelectMany(rule => RampStep().Matches(rule.Body))
            .GroupBy(match => match.Groups[2].Value, match => match.Groups[1].Value)
            .Select(ramp => (ramp.Key, (IReadOnlyList<string>)[.. ramp]))];

    // Each innermost block is one rule; a block that holds others, such as a layer or media query, has no tokens.
    private static IReadOnlyList<(string, string)> LoadRules()
    {
        using var stream = typeof(DesignTokens).Assembly.GetManifestResourceStream("Css/trailblazor.css")
            ?? throw new InvalidOperationException("The library's stylesheet isn't embedded.");
        using var reader = new StreamReader(stream);
        var css = Comment().Replace(reader.ReadToEnd(), "");
        return [.. Rule().Matches(css).Select(match => (match.Groups[1].Value, match.Groups[2].Value))];
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex(@"([^{}]+)\{([^{}]*)\}")]
    private static partial Regex Rule();

    [GeneratedRegex(@"(--tb-([a-z]+)-\d+)\s*:\s*oklch\(")]
    private static partial Regex RampStep();

    [GeneratedRegex(@"var\(\s*(--tb-[\w-]+)")]
    private static partial Regex Used();

    [GeneratedRegex(@"(?:^|[;{\s])(--tb-[\w-]+)\s*:")]
    private static partial Regex Declared();
}
