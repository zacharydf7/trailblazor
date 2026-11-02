using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Text in the type scale. Its look (<see cref="TextStyle"/>) and its element (<see cref="As"/>) are set
/// separately, so restyling a heading never means changing its level.</summary>
public sealed class TbText : TbComponentBase
{
    /// <summary>The typographic style. Defaults to <see cref="TextStyle.Body"/>.</summary>
    [Parameter]
    public TextStyle TextStyle { get; set; } = TextStyle.Body;

    /// <summary>The element to render. Defaults to <see cref="TextElement.P"/>.</summary>
    [Parameter]
    public TextElement As { get; set; } = TextElement.P;

    /// <summary>Colors the text by meaning. When null, the text inherits its color.</summary>
    [Parameter]
    public Tone? Tone { get; set; }

    /// <summary>Uses the quieter text color, for secondary text such as hints and timestamps. A
    /// <see cref="Tone"/> takes its place.</summary>
    [Parameter]
    public bool Muted { get; set; }

    /// <summary>Cuts the text off with an ellipsis after this many lines; 1 keeps it on one line. The text then
    /// shows as a block, even in an inline element, and screen readers still read all of it. When null, the text
    /// wraps freely.</summary>
    [Parameter]
    public int? MaxLines { get; set; }

    /// <summary>The text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (MaxLines < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxLines), MaxLines,
                $"{nameof(MaxLines)} must be at least 1.");
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As.ToString().ToLowerInvariant());
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", ClassNames.Join("tb-text", ClassNames.Modifier("tb-text", TextStyle),
            Tone is { } tone ? ClassNames.Modifier("tb-text", tone) : Muted ? "tb-text--muted" : null,
            MaxLines switch { 1 => "tb-text--truncate", > 1 => "tb-text--clamp", _ => null }, Class));
        builder.AddAttribute(3, "style", Styles.Join(
            MaxLines > 1 ? $"--tb-text-lines: {MaxLines}" : null, Style));
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }
}
