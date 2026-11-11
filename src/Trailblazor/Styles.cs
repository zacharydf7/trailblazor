namespace Trailblazor;

internal static class Styles
{
    // Null rather than empty, so Blazor leaves the style attribute out.
    public static string? Join(params ReadOnlySpan<string?> declarations) =>
        ClassNames.Joined(declarations, "; ");
}
