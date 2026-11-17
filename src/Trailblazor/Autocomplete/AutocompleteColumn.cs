namespace Trailblazor;

/// <summary>One column of a <see cref="TbAutocomplete{TValue}"/>'s suggestions, such as a trail's length beside its
/// name. Screen readers hear each suggestion as its columns' titles and values.</summary>
/// <param name="Title">The column's heading, shown above the suggestions.</param>
/// <param name="Value">The text the column shows for a suggestion.</param>
public sealed record AutocompleteColumn<TValue>(string Title, Func<TValue, string> Value)
{
    /// <summary>The column's width, as a CSS grid track such as <c>"6rem"</c> or <c>"2fr"</c>. Columns without one
    /// share the remaining width evenly.</summary>
    public string? Width { get; init; }
}
