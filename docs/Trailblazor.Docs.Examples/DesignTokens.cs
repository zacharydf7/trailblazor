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

    // The tokens an app may set to restyle one component. Other variables read with a fallback, such as a color picker's
    // value, are set by the components' own code, so they aren't listed.
    public static IReadOnlyList<string> ComponentTokens { get; } =
    [
        "--tb-button-height", "--tb-button-radius", "--tb-data-grid-row-height", "--tb-dialog-radius",
        "--tb-dialog-z-index", "--tb-fab-offset", "--tb-fab-radius", "--tb-fab-z-index", "--tb-field-background",
        "--tb-field-border-color", "--tb-field-border-width", "--tb-field-height", "--tb-field-radius",
        "--tb-menu-item-height", "--tb-menu-radius", "--tb-popover-radius", "--tb-table-row-height",
        "--tb-table-sticky-top", "--tb-tabs-indicator-color", "--tb-tabs-indicator-size", "--tb-tooltip-max-width",
        "--tb-tree-item-height", "--tb-tree-item-radius",
    ];

    private static readonly string[] Tones = ["primary", "neutral", "success", "warning", "danger", "info"];

    private static readonly string[] PageColors =
        ["--tb-color-background", "--tb-color-surface", "--tb-color-text", "--tb-border-"];

    private static readonly string[] DensityTokens =
        ["--tb-control-height", "--tb-control-padding-", "--tb-item-height", "--tb-row-height"];

    // Variables a component sets for itself, such as a dialog's width, are its own workings, not theme tokens.
    public static IReadOnlyList<string> Read(IEnumerable<string> blocks) =>
        [.. RulesOf(blocks).SelectMany(rule => Used().Matches(rule.Body)).Select(match => match.Groups[1].Value)
            .Where(Theme.Value.Contains).Distinct().Order(StringComparer.Ordinal)];

    // The component tokens a block's rules read, each with the fallback that is its default until an app sets it. A
    // token the block sets on itself, as a grid does with the table's row height, is left out: an app setting it from
    // outside the block would be overridden.
    public static IReadOnlyList<(string Name, string Default)> Hooks(IEnumerable<string> blocks)
    {
        var rules = RulesOf(blocks).ToList();
        var declared = rules.SelectMany(rule => Declared().Matches(rule.Body)).Select(match => match.Groups[1].Value)
            .ToHashSet();
        return [.. rules.SelectMany(rule => Fallbacks(rule.Body))
            .Where(hook => ComponentTokens.Contains(hook.Name) && !declared.Contains(hook.Name))
            .DistinctBy(hook => hook.Name).OrderBy(hook => hook.Name, StringComparer.Ordinal)];
    }

    // Every theme token with the value the default palette and density give it, leaving out the tonal ramps: the page's
    // own colors first, then each tone's family, then the rest in the stylesheet's order.
    public static IReadOnlyList<(string Name, string Value)> Reference() =>
        [.. Rules.Value.Where(rule => !rule.Selector.Contains(".tb-", StringComparison.Ordinal)
                && rule.Selector.Contains(":root", StringComparison.Ordinal))
            .SelectMany(rule => Declaration().Matches(rule.Body)).Where(match => !RampStep().IsMatch(match.Value))
            .Select(match => (match.Groups[1].Value, Whitespace().Replace(match.Groups[2].Value.Trim(), " ")))
            .DistinctBy(token => token.Item1).OrderBy(token => Group(token.Item1))];

    public static bool IsColor(string name) =>
        name.Contains("color", StringComparison.Ordinal) || name.StartsWith("--tb-border-", StringComparison.Ordinal);

    // What a theme token is for, from its name: the page's colors one by one, a tone's family by its suffix and the
    // scales by their prefix. A token none of these knows gets no role, which a test catches.
    public static string Role(string name) => name switch
    {
        "--tb-color-background" => "The page",
        "--tb-color-surface" => "Cards, fields and panels",
        "--tb-color-surface-raised" => "Menus, dialogs and popovers",
        "--tb-color-surface-sunken" => "Wells and striped rows",
        "--tb-color-text" => "Body text",
        "--tb-color-text-muted" => "Secondary text, such as hints",
        "--tb-color-text-disabled" => "Disabled items' text",
        "--tb-border-subtle" => "Dividers and quiet borders",
        "--tb-border-strong" => "Field and control borders",
        "--tb-color-hover" => "The translucent layer under a hovered row",
        "--tb-color-pressed" => "The translucent layer under a pressed row",
        "--tb-color-scrim" => "Behind a modal dialog or overlay",
        "--tb-focus-ring-color" => "The focus ring",
        "--tb-disabled-opacity" => "How faint a disabled control is",
        _ when Tones.FirstOrDefault(tone => name == $"--tb-color-on-{tone}") is { } tone =>
            $"Text and icons on the {tone} fill",
        _ when Tones.FirstOrDefault(tone => name.StartsWith($"--tb-color-{tone}", StringComparison.Ordinal))
            is { } tone => name[$"--tb-color-{tone}".Length..] switch
            {
                "" => $"The {tone} fill",
                "-hover" => $"The {tone} fill when hovered",
                "-active" => $"The {tone} fill when pressed",
                "-subtle" => $"A low-contrast {tone} fill for selected items and the soft variant",
                "-text" => $"The {tone} color for text on a surface",
                _ => "",
            },
        _ when DensityTokens.Any(token => name.StartsWith(token, StringComparison.Ordinal)) =>
            "Set by density; see Density",
        _ when name.StartsWith("--tb-font-family", StringComparison.Ordinal) => "Typeface",
        _ when name.StartsWith("--tb-font-size", StringComparison.Ordinal) => "Text size",
        _ when name.StartsWith("--tb-font-weight", StringComparison.Ordinal) => "Text weight",
        _ when name.StartsWith("--tb-line-height", StringComparison.Ordinal) => "Line height",
        _ when name.StartsWith("--tb-space", StringComparison.Ordinal) => "Spacing step",
        _ when name.StartsWith("--tb-radius", StringComparison.Ordinal) => "Corner radius",
        _ when name.StartsWith("--tb-shadow", StringComparison.Ordinal) => "Shadow",
        _ when name.StartsWith("--tb-motion-duration", StringComparison.Ordinal) => "Animation length",
        _ when name.StartsWith("--tb-motion-easing", StringComparison.Ordinal) => "Animation curve",
        _ when name.StartsWith("--tb-focus-ring", StringComparison.Ordinal) => "The focus ring's width and offset",
        _ when name.StartsWith("--tb-breakpoint", StringComparison.Ordinal) => "Viewport width for layouts",
        _ => "",
    };

    private static int Group(string name) =>
        Array.FindIndex(Tones, tone => name.StartsWith($"--tb-color-{tone}", StringComparison.Ordinal)
            || name == $"--tb-color-on-{tone}") is var tone and >= 0 ? 1 + tone
        : PageColors.Any(color => name.StartsWith(color, StringComparison.Ordinal)) ? 0
        : Tones.Length + 1;

    // A block's own rules, its modifiers and its elements: tb-button, tb-button--small, tb-button__label.
    private static IEnumerable<(string Selector, string Body)> RulesOf(IEnumerable<string> blocks)
    {
        var pattern = new Regex(string.Join('|',
            blocks.Select(block => $@"\.{Regex.Escape(block)}(?:--|__|(?![\w-]))")));
        return Rules.Value.Where(rule => pattern.IsMatch(rule.Selector));
    }

    // Each var() with a fallback, and the fallback up to its own closing parenthesis, whatever it nests.
    private static IEnumerable<(string Name, string Default)> Fallbacks(string body)
    {
        foreach (Match match in WithFallback().Matches(body))
        {
            var start = match.Index + match.Length;
            var end = start;
            for (var depth = 1; depth > 0; end++)
            {
                depth += body[end] switch { '(' => 1, ')' => -1, _ => 0 };
            }

            yield return (match.Groups[1].Value, Whitespace().Replace(body[start..(end - 1)].Trim(), " "));
        }
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

    [GeneratedRegex(@"(--tb-[\w-]+)\s*:\s*([^;]+);")]
    private static partial Regex Declaration();

    [GeneratedRegex(@"var\(\s*(--tb-[\w-]+)\s*,")]
    private static partial Regex WithFallback();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
