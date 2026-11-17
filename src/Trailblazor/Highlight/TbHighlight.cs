using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Text with the parts that match a search marked, such as a search result's title, so people see why it
/// matched. Each word of the search is marked wherever it appears.</summary>
public sealed class TbHighlight : TbComponentBase
{
    /// <summary>The text to show.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = "";

    /// <summary>What was searched for, such as <c>"pine creek"</c>. Each word in it is marked wherever it appears in
    /// <see cref="Text"/>; null or blank marks nothing.</summary>
    [Parameter]
    public string? Search { get; set; }

    /// <summary>Matches only words with the same capitals. Defaults to false, so <c>"pine"</c> marks "Pine".</summary>
    [Parameter]
    public bool CaseSensitive { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", ClassNames.Join("tb-highlight", Class));
        builder.AddAttribute(3, "style", Style);
        foreach (var (start, length, match) in Runs(Text, Search, CaseSensitive))
        {
            var text = Text.Substring(start, length);
            if (match)
            {
                builder.OpenElement(4, "mark");
                builder.AddAttribute(5, "data-part", "match");
                builder.AddAttribute(6, "class", "tb-highlight__match");
                builder.AddContent(7, text);
                builder.CloseElement();
            }
            else
            {
                builder.AddContent(8, text);
            }
        }

        builder.CloseElement();
    }

    // The text cut into runs that match a word of the search or don't, in order; overlapping matches join.
    internal static IEnumerable<(int Start, int Length, bool Match)> Runs(string text, string? search,
        bool caseSensitive)
    {
        var marked = new bool[text.Length];
        var compare = CultureInfo.CurrentCulture.CompareInfo;
        var options = caseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase;
        foreach (var word in (search ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            for (var from = 0; from < text.Length;)
            {
                var at = compare.IndexOf(text.AsSpan(from), word, options, out var length);
                if (at < 0 || length == 0)
                {
                    break;
                }

                Array.Fill(marked, true, from + at, length);
                from += at + length;
            }
        }

        for (var start = 0; start < text.Length;)
        {
            var end = start;
            while (end < text.Length && marked[end] == marked[start])
            {
                end++;
            }

            yield return (start, end - start, marked[start]);
            start = end;
        }
    }
}
