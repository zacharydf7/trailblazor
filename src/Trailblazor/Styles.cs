namespace Trailblazor;

internal static class Styles
{
    public static string Join(params string?[] declarations) =>
        string.Join("; ", declarations.Where(declaration => !string.IsNullOrWhiteSpace(declaration)));
}
