using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Text in the type scale. Its look (<see cref="TextStyle"/>) and its element (<see cref="As"/>) are set
/// separately, so restyling a heading never means changing its level.</summary>
public sealed class TbText : TbComponentBase, IAsyncDisposable
{
    private readonly string _tooltipId = $"tb-text-{Guid.NewGuid():N}";
    private ElementReference _element;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<TextCut>? _cut;
    // What the script was last started to do, or null while it isn't running.
    private (bool Box, bool Middle)? _watched;
    // Whether the script found the box cutting the text off, which only a browser can measure, and whether it has
    // measured yet.
    private bool _overflows;
    private bool _measured;
    // Whether the script found the text inside something that takes focus, such as a link or a grid's cell.
    private bool _held;
    private string? _shortened;
    private bool _disposed;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

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

    /// <summary>Cuts the text to at most this many characters and adds an ellipsis, moving the cut back to the last
    /// space when that keeps more than half of them. Use it for text that has to fit a fixed width before the page can
    /// measure it, such as an ID in a narrow column. Screen readers still read all of it. Needs <see cref="Text"/>.
    /// Characters differ in width, so prefer <see cref="MaxLines"/> where it fits.</summary>
    [Parameter]
    public int? MaxLength { get; set; }

    /// <summary>Where the text is cut: at the end, the default, or in the middle, keeping its start and end, for file
    /// names, IDs and paths. A middle cut needs <see cref="Text"/>, and <see cref="MaxLength"/> or a
    /// <see cref="MaxLines"/> of 1. With <see cref="MaxLines"/>, the text is cut to fit its box once the page is
    /// interactive; before then it's cut to <see cref="MaxLength"/> when given, and at the end when not.</summary>
    [Parameter]
    public TextTruncation Truncation { get; set; }

    /// <summary>When <see cref="MaxLines"/> or <see cref="MaxLength"/> cuts the text off, shows all of it in a tooltip,
    /// on hover and on keyboard focus. Only text that is cut off gets the tooltip and becomes a tab stop, unless it
    /// sits alone in something that takes focus, such as a link or a grid's cell, whose focus shows the tooltip.
    /// Screen readers hear the text once. Before the page is interactive, the text is cut off with no tooltip.
    /// </summary>
    [Parameter]
    public bool ShowFullText { get; set; }

    /// <summary>Which side of the text the tooltip of <see cref="ShowFullText"/> appears on. Defaults to
    /// <see cref="Placement.Top"/>.</summary>
    [Parameter]
    public Placement FullTextPlacement { get; set; } = Placement.Top;

    /// <summary>The text, as plain text, in place of <see cref="ChildContent"/>. <see cref="MaxLength"/> needs it.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>The text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    // The script cuts one line in the middle to fit its box.
    private bool CutsMiddle => Truncation == TextTruncation.Middle && MaxLines == 1 && RendererInfo.IsInteractive;

    // The script measures the box where a tooltip could show for text MaxLines cuts off, or where it cuts the middle.
    private bool MeasuresBox => ShowFullText && MaxLines is not null && RendererInfo.IsInteractive || CutsMiddle;

    // The script also tells text that only MaxLength cuts whether it sits in something that takes focus.
    private bool Measures => MeasuresBox || ShowFullText && _shortened is not null && RendererInfo.IsInteractive;

    // Once the script has cut the middle, it alone says whether the text is cut; MaxLength only cut it until then.
    private bool Cut => CutsMiddle && _measured ? _overflows : _shortened is not null || (MeasuresBox && _overflows);

    private bool ShowsTooltip => ShowFullText && Cut && RendererInfo.IsInteractive;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (MaxLines < 1 || MaxLength < 1)
        {
            var name = MaxLines < 1 ? nameof(MaxLines) : nameof(MaxLength);
            throw new ArgumentOutOfRangeException(name, MaxLines < 1 ? MaxLines : MaxLength,
                $"{name} must be at least 1.");
        }

        if (Text is not null && ChildContent is not null)
        {
            throw new InvalidOperationException($"A {nameof(TbText)} takes its text from {nameof(Text)} or from "
                + $"{nameof(ChildContent)}, not both.");
        }

        if (MaxLength is not null && Text is null)
        {
            throw new InvalidOperationException($"{nameof(MaxLength)} needs {nameof(Text)}, since it counts the "
                + "text's characters.");
        }

        if (ShowFullText && MaxLines is null && MaxLength is null)
        {
            throw new InvalidOperationException($"{nameof(ShowFullText)} needs {nameof(MaxLines)} or "
                + $"{nameof(MaxLength)}, which cut the text off.");
        }

        if (Truncation == TextTruncation.Middle
            && (Text is null || MaxLines > 1 || MaxLines is null && MaxLength is null))
        {
            throw new InvalidOperationException($"A middle {nameof(Truncation)} needs {nameof(Text)}, and "
                + $"{nameof(MaxLength)} or a {nameof(MaxLines)} of 1, since it cuts one line.");
        }

        _shortened = MaxLength is not { } most ? null
            : Truncation == TextTruncation.Middle ? ShortenedInTheMiddle(Text!, most) : Shortened(Text!, most);
    }

    // The text cut to at most this many characters, counting what a reader sees as one character once, with an
    // ellipsis; null when it fits. A cut inside a word moves back to the last space, unless that loses half of them.
    internal static string? Shortened(string text, int most)
    {
        text = text.TrimEnd();
        var end = 0;
        for (var count = 0; count < most && end < text.Length; count++)
        {
            end += StringInfo.GetNextTextElementLength(text, end);
        }

        if (end >= text.Length)
        {
            return null;
        }

        var kept = text[..end];
        var space = kept.LastIndexOf(' ');
        if (!char.IsWhiteSpace(text[end]) && space > kept.Length / 2)
        {
            kept = kept[..space];
        }

        return $"{kept.TrimEnd()}…";
    }

    // The text cut to at most this many characters, counting what a reader sees as one character once, with as many
    // kept from its start as from its end, one more from the start for an odd count, and an ellipsis between; null
    // when it fits.
    internal static string? ShortenedInTheMiddle(string text, int most)
    {
        var starts = StringInfo.ParseCombiningCharacters(text);
        if (starts.Length <= most)
        {
            return null;
        }

        var head = text[..starts[(most + 1) / 2]];
        var tail = most / 2 == 0 ? "" : text[starts[^(most / 2)]..];
        return $"{head.TrimEnd()}…{tail.TrimStart()}";
    }

    internal void OnCut(bool cut, bool held)
    {
        if (_overflows != cut || _held != held || !_measured)
        {
            (_overflows, _held, _measured) = (cut, held, true);
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As.ToString().ToLowerInvariant());
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", ClassNames.Join("tb-text", ClassNames.Modifier("tb-text", TextStyle),
            Tone is { } tone ? ClassNames.Modifier("tb-text", tone) : Muted ? "tb-text--muted" : null,
            MaxLines switch { 1 => "tb-text--truncate", > 1 => "tb-text--clamp", _ => null },
            Truncation == TextTruncation.Middle ? "tb-text--middle" : null,
            ShowsTooltip ? "tb-text--full-text" : null, Class));
        builder.AddAttribute(3, "style", Styles.Join(
            MaxLines > 1 ? $"--_tb-text-lines: {MaxLines}" : null, Style));
        // Cut-off text with a tooltip is a tab stop, so keyboard users can reach the tooltip, unless it sits in
        // something that takes focus, such as a link or a grid's cell, whose focus shows the tooltip instead.
        builder.AddAttribute(4, "tabindex", ShowsTooltip && !_held ? "0" : null);
        builder.AddAttribute(5, "data-cut", Cut);
        builder.AddAttribute(6, "data-measures", Measures);
        builder.AddAttribute(7, "data-ready", Measures && _measured);
        builder.AddElementReferenceCapture(8, element => _element = element);
        if (_shortened is not null || CutsMiddle)
        {
            // Screen readers read the whole text from the hidden copy, and skip the shortened one.
            builder.OpenElement(9, "span");
            builder.AddAttribute(10, "class", "tb-visually-hidden");
            builder.AddAttribute(11, "data-part", "full-text");
            builder.AddContent(12, Text);
            builder.CloseElement();
            builder.OpenElement(13, "span");
            builder.AddAttribute(14, "aria-hidden", "true");
            builder.AddAttribute(15, "data-part", "shortened");
            builder.AddContent(16, _shortened ?? Text);
            builder.CloseElement();
            if (CutsMiddle)
            {
                // The script fills this with the text cut to fit, and the render never touches what it holds.
                builder.OpenElement(17, "span");
                builder.AddAttribute(18, "aria-hidden", "true");
                builder.AddAttribute(19, "data-part", "middle");
                builder.CloseElement();
            }
        }
        else if (ShowFullText || MaxLength is not null)
        {
            builder.OpenElement(20, "span");
            builder.AddAttribute(21, "data-part", "full-text");
            AddText(builder, 22);
            builder.CloseElement();
        }
        else
        {
            AddText(builder, 23);
        }

        if (ShowsTooltip)
        {
            var tooltips = Services.GetRequiredService<IOptions<TrailblazorOptions>>().Value.Tooltips;
            // It only repeats the text screen readers have already read, so it is hidden from them.
            builder.OpenElement(24, "span");
            builder.AddAttribute(25, "id", _tooltipId);
            builder.AddAttribute(26, "popover", "manual");
            builder.AddAttribute(27, "role", "tooltip");
            builder.AddAttribute(28, "aria-hidden", "true");
            builder.AddAttribute(29, "data-part", "tooltip");
            builder.AddAttribute(30, "data-placement", Anchors.Side(FullTextPlacement));
            builder.AddAttribute(31, "data-align", Anchors.Align(PlacementAlign.Center));
            builder.AddAttribute(32, "data-offset", 6);
            builder.AddAttribute(33, "data-show-delay", Milliseconds(tooltips.ShowDelay));
            builder.AddAttribute(34, "data-hide-delay", Milliseconds(tooltips.HideDelay));
            builder.AddAttribute(35, "class", "tb-tooltip");
            AddText(builder, 36);
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private void AddText(RenderTreeBuilder builder, int sequence)
    {
        if (Text is not null)
        {
            builder.AddContent(sequence, Text);
        }
        else
        {
            builder.AddContent(sequence, ChildContent);
        }
    }

    private static string Milliseconds(TimeSpan delay) =>
        delay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The script starts again when what it does changes, such as a middle cut turned on or off.
        (bool Box, bool Middle)? wanted = Measures ? (MeasuresBox, CutsMiddle) : null;
        if (wanted == _watched)
        {
            return;
        }

        (_watched, _measured) = (wanted, false);
        if (_handle is { } old)
        {
            _handle = null;
            await old.DisposeHandleAsync();
        }

        if (wanted is not { } mode)
        {
            return;
        }

        try
        {
            var module = await Services.GetRequiredService<JsModuleLoader>()
                .ImportAsync("./_content/Trailblazor/text.js");
            if (_disposed || _watched != wanted)
            {
                return;
            }

            _cut ??= DotNetObjectReference.Create(new TextCut(DispatchExceptionAsync, OnCut));
            var handle = await module.InvokeAsync<IJSObjectReference?>("watchTextCut", _element, _cut, mode.Box,
                mode.Middle);
            if (handle is not null && (_disposed || _watched != wanted))
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the text went with it.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _cut?.Dispose();
    }
}
