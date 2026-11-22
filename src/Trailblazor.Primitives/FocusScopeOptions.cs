namespace Trailblazor.Primitives;

internal sealed record FocusScopeOptions
{
    public bool Trapped { get; init; }

    public bool AutoFocus { get; init; } = true;

    public bool RestoreFocus { get; init; } = true;

    // Sets data-ready on the container once the scope manages the focus; TbFocusScope sets it, since its element is
    // the scope.
    public bool MarksReady { get; init; }
}
