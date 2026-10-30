namespace Trailblazor;

/// <summary>How a <see cref="TbSkeleton"/> moves while it waits.</summary>
public enum SkeletonAnimation
{
    /// <summary>Fades in and out.</summary>
    Pulse,

    /// <summary>A light sweeps across it in the reading direction.</summary>
    Wave,

    /// <summary>Stays still.</summary>
    None,
}
