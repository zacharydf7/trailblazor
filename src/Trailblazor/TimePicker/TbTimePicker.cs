using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Chooses a time of day, typed in the box in the <see cref="Culture"/>'s form or picked from a list of times.
/// Bind it with <c>@bind-Value</c> to a <see cref="TimeOnly"/>, or to a <see cref="TimeSpan"/> from midnight that is
/// under 24 hours. Before the page is interactive it is the browser's own time box, so a static form posts the time.
/// </summary>
public partial class TbTimePicker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TTime>
    : IAsyncDisposable where TTime : struct
{
    private readonly ItemHandlers<TimeOnly, Func<Task>> _pick;
    private ElementReference _input;
    private ElementReference _listbox;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<AutocompleteEvents>? _events;

    /// <summary>Creates the picker.</summary>
    public TbTimePicker() => _pick = new(time => () => Disabled || ReadOnly ? Task.CompletedTask
        : SetValueAsync(FromTime(time)));

    /// <summary>The earliest time that can be chosen. A <see cref="Min"/> after <see cref="Max"/> is a range across
    /// midnight, such as 10 PM to 6 AM.</summary>
    [Parameter]
    public TTime? Min { get; set; }

    /// <summary>The latest time that can be chosen.</summary>
    [Parameter]
    public TTime? Max { get; set; }

    /// <summary>How far apart the times in the list are, such as 15 minutes. Without a step, or with one under five
    /// minutes, the list is every half hour; a step that isn't a whole number of minutes adds seconds to the box.
    /// </summary>
    [Parameter]
    public TimeSpan? Step { get; set; }

    /// <summary>The culture whose form the box shows and reads times in, such as <c>9:30 PM</c> or <c>21:30</c>.
    /// Defaults to the current culture. The box reads a 24-hour time, such as <c>21:30</c>, in any culture.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>The words the picker shows.</summary>
    [Parameter]
    public TimePickerLabels Labels { get; set; } = new();

    /// <summary>Content before the input, usually an icon.</summary>
    [Parameter]
    public RenderFragment? Start { get; set; }

    /// <summary>Content after the input, such as a unit or a button.</summary>
    [Parameter]
    public RenderFragment? End { get; set; }

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    private bool ShowsSeconds => Step is { } step && step.TotalSeconds % 60 != 0;

    private string? StepSeconds => Step is { } step
        ? step.TotalSeconds.ToString(CultureInfo.InvariantCulture)
        : null;

    private CultureInfo TimeCulture => Culture ?? CultureInfo.CurrentCulture;

    private string TimePattern => ShowsSeconds ? "T" : "t";

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    private string ListboxId => $"{InputId}-times";

    private string OpenLabel => Labels.Open ?? Localizer[TbStrings.TimePickerOpen];

    // The list runs from Min, or midnight, and keeps the times within Min and Max.
    private List<TimeOnly> ListTimes => TimeList.From(Min is { } min ? ToTime(min) : TimeOnly.MinValue, Step,
        time => ValueError(FromTime(time)) is null);

    private TimeOnly? StartTime(List<TimeOnly> times) =>
        TimeList.Start(times, Value is { } value ? ToTime(value) : null);

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _input.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender || !RendererInfo.IsInteractive)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/time-picker.js");
            if (IsDisposed)
            {
                return;
            }

            // The list's opening and closing need nothing from the server.
            _events = DotNetObjectReference.Create(
                new AutocompleteEvents(DispatchExceptionAsync, _ => Task.CompletedTask, () => Task.CompletedTask));
            var handle = await module.InvokeAsync<IJSObjectReference?>("createTimePicker", _input, _listbox, _events);
            if (handle is not null && IsDisposed)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (typeof(TTime) != typeof(TimeOnly) && typeof(TTime) != typeof(TimeSpan))
        {
            throw new InvalidOperationException(
                $"{nameof(TbTimePicker<TTime>)} chooses TimeOnly or TimeSpan, not {typeof(TTime).Name}.");
        }
    }

    private protected override bool Pastable => SmartPasteField.Takes(typeof(TTime));

    private protected override string? FormatValue(TTime? value) =>
        value is { } time ? ToTime(time).ToString(TimePattern, TimeCulture) : null;

    private protected override bool TryParse(string? text, out TTime? value, out string? error)
    {
        value = null;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!TimeList.TryRead(text, TimeCulture, out var time))
        {
            error = Message(TbStrings.FieldInvalid);
            return false;
        }

        value = FromTime(time);
        return true;
    }

    // A time outside Min and Max shows a message, however it arrived, since the browser lets one be typed. A Min after
    // Max is a range across midnight, as the browser's time box reads it.
    private protected override string? ValueError(TTime? value)
    {
        if (value is not { } chosen)
        {
            return null;
        }

        var time = ToTime(chosen);
        if (Min is { } min && Max is { } max)
        {
            var (first, last) = (ToTime(min), ToTime(max));
            var inside = first <= last ? time >= first && time <= last : time >= first || time <= last;
            return inside ? null : string.Format(CultureInfo.CurrentCulture, OutOfRangeLabel, Shown(min), Shown(max));
        }

        if (Min is { } earliest && time < ToTime(earliest))
        {
            return string.Format(CultureInfo.CurrentCulture, AtLeastLabel, Shown(earliest));
        }

        return Max is { } latest && time > ToTime(latest)
            ? string.Format(CultureInfo.CurrentCulture, AtMostLabel, Shown(latest))
            : null;
    }

    private string? IsoOf(TTime? value) =>
        value is { } time ? ToTime(time).ToString(ShowsSeconds ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture)
            : null;

    private string Shown(TTime time) => ToTime(time).ToString("t", TimeCulture);

    private string Shown(TimeOnly time) => time.ToString(TimePattern, TimeCulture);

    private static TimeOnly ToTime(TTime value) => value switch
    {
        TimeSpan span when span < TimeSpan.Zero || span >= TimeSpan.FromDays(1) => throw new InvalidOperationException(
            $"{nameof(TbTimePicker<TTime>)} takes a TimeSpan from midnight that is under 24 hours, not {span}."),
        TimeSpan span => TimeOnly.FromTimeSpan(span),
        _ => (TimeOnly)(object)value,
    };

    private static TTime FromTime(TimeOnly time) =>
        typeof(TTime) == typeof(TimeSpan) ? (TTime)(object)time.ToTimeSpan() : (TTime)(object)time;

    private string OutOfRangeLabel => Labels.OutOfRange ?? Localizer[TbStrings.TimePickerOutOfRange];
    private string AtLeastLabel => Labels.AtLeast ?? Localizer[TbStrings.TimePickerAtLeast];
    private string AtMostLabel => Labels.AtMost ?? Localizer[TbStrings.TimePickerAtMost];

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync when both exist, so the field's own cleanup has to run from here.
        Dispose();
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
