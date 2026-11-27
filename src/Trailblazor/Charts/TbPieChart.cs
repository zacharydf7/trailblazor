using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A pie or donut chart: each item's value as its share of the total, for a few parts of one whole, such as
/// jobs by status. It is drawn as markup that needs no script to show, with a legend that gives each share and a
/// data table under it for anyone who can't read the drawing. On an interactive page a tooltip follows the pointer,
/// and the arrow keys move between its slices.</summary>
public partial class TbPieChart<TItem>
{
    // The series colors the theme has; a ninth slice starts over at the first.
    private const int Slots = 8;

    // How much of a donut's radius its hole takes.
    private const double Hole = 0.6;

    private static readonly string Box =
        string.Create(CultureInfo.InvariantCulture, $"0 0 {ChartArc.Box} {ChartArc.Box}");

    private readonly string _plotId = $"tb-pie-{Guid.NewGuid():N}-plot";
    private IReadOnlyList<TItem> _items = [];
    private IReadOnlyList<double?> _values = [];
    private ElementReference _plot;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<ChartEvents>? _events;
    private (string Point, string NoValue, bool Activates) _sent;
    private int _commits;
    private bool _disposed;

    /// <summary>A label this chart's use is counted under, such as <c>"jobs-by-status"</c>, so the app can count how
    /// often people see it through the <c>Trailblazor</c> meter. Controls that share a label count together; a control
    /// without one isn't counted. It names what the control shows, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    /// <summary>The items the chart draws, a slice for each, clockwise from the top in the order given. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEnumerable<TItem>? Items { get; set; }

    /// <summary>What each item is called, in the legend, the tooltip and the data table, such as
    /// <c>status => status.Name</c>. Required.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, object?>? Label { get; set; }

    /// <summary>An item's value, such as <c>status => status.Count</c>. Required. Any number type works. An item
    /// whose value is null, zero, below zero or not finite has no slice, and stays in the data table. A value that
    /// isn't a number throws.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, object?>? Value { get; set; }

    /// <summary>How the values are written in the tooltip, the data table and a slice's name for screen readers, as
    /// a .NET number format such as <c>"N0"</c>, in the current culture.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>The chart's name, shown above it, such as "Jobs by status". Give this or <see cref="AriaLabel"/>.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Names the chart for screen readers when it has no <see cref="Title"/>, such as when a heading beside
    /// it already says what it shows.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Whether the pie has a hole in its middle, which <see cref="ChildContent"/> can fill. False by
    /// default.</summary>
    [Parameter]
    public bool Donut { get; set; }

    /// <summary>What the middle of a <see cref="Donut"/> shows, such as the total. Keep it to a few words: it has
    /// about half the pie's width.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>How wide and high the pie is, as a CSS length such as <c>"12rem"</c>. Without it, the
    /// <c>--tb-pie-diameter</c> token sets it, 16rem by default. The pie is never wider than its container.
    /// </summary>
    [Parameter]
    public string? Diameter { get; set; }

    /// <summary>A tone for single items, such as <c>status => status.Failed ? Tone.Danger : null</c>: the item's
    /// slice takes the tone's color in place of the one its position gives it.</summary>
    [Parameter]
    public Func<TItem, Tone?>? ItemTone { get; set; }

    /// <summary>Raised when the user clicks or taps a slice, or presses Enter or Space on one, with its item, its
    /// label and its value. With it, the pie shows a pointer cursor once the page is interactive.</summary>
    [Parameter]
    public EventCallback<ChartPoint<TItem>> OnSliceClick { get; set; }

    /// <summary>The words the chart shows and says to screen readers.</summary>
    [Parameter]
    public PieChartLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the chart's parts.</summary>
    [Parameter]
    public ChartClasses? Classes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Label is null || Value is null)
        {
            throw new InvalidOperationException(
                $"{nameof(TbPieChart<>)} needs a {nameof(Label)} and a {nameof(Value)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        try
        {
            if (firstRender)
            {
                await AttachAsync();
            }
            else if (_handle is not null && ScriptSettings() is var settings && settings != _sent)
            {
                _sent = settings;
                await _handle.InvokeVoidAsync("update", settings.Point, settings.NoValue, settings.Activates);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the chart went with it.
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/chart.js");
        if (_disposed)
        {
            return;
        }

        _sent = ScriptSettings();
        _events = DotNetObjectReference.Create(new ChartEvents(DispatchExceptionAsync, OnActivateAsync, _plotId));
        var handle = await module.InvokeAsync<IJSObjectReference?>("attachChart", _plot, _events, _sent.Point,
            _sent.NoValue, _sent.Activates);
        if (_disposed && handle is not null)
        {
            await handle.DisposeHandleAsync();
            return;
        }

        _handle = handle;
    }

    // What the script needs beyond the markup: how a slice is named, and whether activating one is heard. The script
    // fills a name as a chart of several series does, with the series as {1}; a pie has none, so its {1} to {3}
    // become the script's {2} to {4}.
    private (string Point, string NoValue, bool Activates) ScriptSettings() =>
        (string.Format(CultureInfo.InvariantCulture, Labels.Slice ?? Localizer[TbStrings.ChartSlice], "{0}", "{2}",
            "{3}", "{4}"), "", OnSliceClick.HasDelegate);

    // The script names the slice by its item's place among the items. A call that arrives after the data changed may
    // name a slice that is gone, and is passed over.
    private async Task OnActivateAsync(int _, int place, string input)
    {
        if (place < 0 || place >= _items.Count || _values[place] is not { } value || value <= 0)
        {
            return;
        }

        Telemetry.Used(TelemetryName, "activate", input == "keyboard" ? "keyboard" : "pointer");
        try
        {
            await OnSliceClick.InvokeAsync(new(_items[place], LabelOf(_items[place]), value));
        }
        finally
        {
            // Counted once the app's handler has run, which the testing helpers wait for.
            _commits++;
            StateHasChanged();
        }
    }

    // One item as the chart shows it: its label, its value and its share as text, its slice when it has one, and
    // its slice's tone.
    private sealed record Row(string Label, string Value, string Share, ChartSlice? Slice, Tone? Tone);

    // Everything the drawing shows, read from the items as they stand when it is drawn.
    private List<Row> Rows()
    {
        var culture = CultureInfo.CurrentCulture;
        var items = _items = Items as IReadOnlyList<TItem> ?? [.. Items ?? []];
        List<string> labels = [.. items.Select(LabelOf)];
        var values = _values = [.. items.Select((item, place) => Number(Value!(item), labels[place]))];
        var slices = ChartArc.Slices(values, Donut ? Hole : 0);
        return [.. items.Select((item, place) => new Row(labels[place],
            values[place]?.ToString(Format, culture) ?? "", slices[place] is { } slice ? Percent(slice.Share) : "",
            slices[place], slices[place] is null ? null : ItemTone?.Invoke(item)))];
    }

    private string LabelOf(TItem item) => ChartValues.Text(Label!(item), null, CultureInfo.CurrentCulture);

    private static double? Number(object? value, string label) => value is not null && !ChartValues.IsNumber(value)
        ? throw new InvalidOperationException($"{nameof(TbPieChart<>)} got a value of type "
            + $"{value.GetType().Name} for \"{label}\", which isn't a number.")
        : ChartValues.Number(value, label);

    // A share under one percent shows one decimal.
    private static string Percent(double share) =>
        share.ToString(share < 0.01 ? "P1" : "P0", CultureInfo.CurrentCulture);

    // A slice's classes: the color its item's place gives it, and its tone's color in place of that when it has one.
    private static string? Color(int place, Row row) => ClassNames.Join($"tb-chart__series--{place % Slots + 1}",
        row.Tone is { } tone ? ClassNames.Modifier("tb-chart__tone", tone) : null);

    // One line for each item, for the script to read. A name with a line break in it would count as two.
    private static string Places(List<Row> rows) =>
        string.Join('\n', rows.Select(row => row.Label.ReplaceLineEndings(" ")));

    // One line for each item, for the tooltip and a slice's name: its value and its share, as "42 (60%)".
    private string Values(List<Row> rows)
    {
        var format = Labels.ValueAndShare ?? Localizer[TbStrings.ChartValueAndShare];
        return string.Join('\n', rows.Select(row => row.Slice is null ? row.Value
            : string.Format(CultureInfo.CurrentCulture, format, row.Value, row.Share)));
    }

    // The middle of each slice, where the keys' focus ring and the tooltip sit, as whole steps of the 10,000 grid.
    // An item with no slice has a dash.
    private static string Points(List<Row> rows) => string.Join(' ', rows.Select(row => row.Slice is { } slice
        ? string.Create(CultureInfo.InvariantCulture, $"{Grid(slice.Middle.X)},{Grid(slice.Middle.Y)}") : "-"));

    // How far across the pie each slice's middle is, which decides the side its tooltip opens on.
    private static string At(List<Row> rows) =>
        string.Join(' ', rows.Select(row => Grid(row.Slice?.Middle.X ?? 0.5).ToString(CultureInfo.InvariantCulture)));

    private static double Grid(double share) => Math.Round(share * ChartPath.Grid);

    // What a screen reader hears for the drawing: the kind of chart and its slices, as "Pie chart: Done, Failed".
    private string Summary(List<Row> rows) => string.Format(CultureInfo.CurrentCulture,
        Labels.Summary ?? Localizer[TbStrings.ChartSummary],
        Donut ? Labels.Donut ?? Localizer[TbStrings.ChartDonut] : Labels.Pie ?? Localizer[TbStrings.ChartPie],
        string.Join(", ", rows.Where(row => row.Slice is not null).Select(row => row.Label)));

    /// <summary>Stops the chart's script from following the pointer and the keys.</summary>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
