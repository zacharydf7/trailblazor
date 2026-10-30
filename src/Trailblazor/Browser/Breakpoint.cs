namespace Trailblazor;

/// <summary>The width ranges of the <c>--tb-breakpoint-*</c> tokens. Each starts at its token's width and runs up to
/// the next.</summary>
public enum Breakpoint
{
    /// <summary>Narrower than 640px, such as a phone held upright.</summary>
    Xs,

    /// <summary>From 640px.</summary>
    Sm,

    /// <summary>From 768px, such as a tablet held upright.</summary>
    Md,

    /// <summary>From 1024px.</summary>
    Lg,

    /// <summary>From 1280px.</summary>
    Xl,
}
