namespace Trailblazor;

internal static class Styles
{
    // Null rather than empty, so Blazor leaves the style attribute out.
    public static string? Join(params string?[] declarations)
    {
        var joined = string.Join("; ", declarations.Where(declaration => !string.IsNullOrWhiteSpace(declaration)));
        return joined.Length == 0 ? null : joined;
    }
}
