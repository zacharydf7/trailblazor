namespace Trailblazor;

internal static class Spacing
{
    private static readonly int[] Steps = [0, 1, 2, 3, 4, 5, 6, 8, 10, 12, 16];

    // Turns a step of the spacing scale into its token, e.g. 3 becomes var(--tb-space-3).
    public static string Token(int step, string parameterName) => Steps.Contains(step)
        ? $"var(--tb-space-{step})"
        : throw new ArgumentOutOfRangeException(parameterName, step, $"Spacing steps are {string.Join(", ", Steps)}.");
}
