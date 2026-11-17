namespace Trailblazor;

/// <summary>One choice in a <see cref="TbCascader{TValue}"/>, such as a country, with the choices nested under it,
/// such as its regions.</summary>
/// <param name="Text">What the choice shows, and its part of the field's path.</param>
/// <param name="Value">The field's value once this choice is made.</param>
public sealed record CascaderItem<TValue>(string Text, TValue Value)
{
    /// <summary>The choices under this one, shown in the next list when it opens. A choice without any ends the path.
    /// </summary>
    public IReadOnlyList<CascaderItem<TValue>> Children { get; init; } = [];

    /// <summary>Shows the choice but keeps it from being opened or chosen.</summary>
    public bool Disabled { get; init; }
}
