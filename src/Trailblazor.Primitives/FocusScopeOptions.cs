namespace Trailblazor.Primitives;

internal sealed record FocusScopeOptions
{
    public bool Trapped { get; init; }

    public bool AutoFocus { get; init; } = true;

    public bool RestoreFocus { get; init; } = true;
}
