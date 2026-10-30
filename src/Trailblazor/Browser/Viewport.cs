namespace Trailblazor;

/// <summary>The viewport's size in CSS pixels, including any scrollbar, as media queries measure it.</summary>
/// <param name="Width">The width, which sets the <see cref="Breakpoint"/>.</param>
/// <param name="Height">The height.</param>
public sealed record Viewport(int Width, int Height)
{
    // The --tb-breakpoint-* tokens, from sm up.
    internal static readonly int[] MinWidths = [640, 768, 1024, 1280];

    /// <summary>The breakpoint the width falls in.</summary>
    public Breakpoint Breakpoint => (Breakpoint)MinWidths.Count(width => Width >= width);
}
