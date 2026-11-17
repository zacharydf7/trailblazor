namespace Trailblazor;

/// <summary>The icons a field shows for its validation status. Each one left null falls back to the app's, and then
/// to the built-in one.</summary>
public sealed record FieldStatusIcons
{
    /// <summary>Shown while a check runs, in place of the built-in spinner.</summary>
    public Icon? Checking { get; init; }

    /// <summary>Shown once a check passes. A check mark by default.</summary>
    public Icon? Valid { get; init; }

    /// <summary>Shown beside each of the field's errors. An error mark by default.</summary>
    public Icon? Invalid { get; init; }
}
