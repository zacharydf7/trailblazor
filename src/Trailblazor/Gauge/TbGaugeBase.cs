using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>What <see cref="TbGauge"/> and <see cref="TbLinearGauge"/> share: one value between <see cref="Min"/>
/// and <see cref="Max"/>, the text that says it, and the ranges written inside the gauge.</summary>
public abstract class TbGaugeBase : TbComponentBase
{
    private protected readonly string LabelId = $"tb-gauge-{Guid.NewGuid():N}-label";

    /// <summary>A label this gauge's use is counted under, such as <c>"server-load"</c>, so the app can count how
    /// often people see it through the <c>Trailblazor</c> meter. Controls that share a label count together; a
    /// control without one isn't counted. It names what the control shows, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Inject]
    private protected ITbLocalizer Localizer { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private protected GaugeRanges Ranges => field ??= new(StateHasChanged);

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    /// <summary>The value the gauge shows. One below <see cref="Min"/> or above <see cref="Max"/> is drawn at that
    /// end, and its text still says the value itself. Null, or a value that isn't finite, shows that there is no
    /// value, as for a sensor that hasn't answered.</summary>
    [Parameter]
    public double? Value { get; set; }

    /// <summary>The value at the start of the gauge. Defaults to 0.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>The value at the end of the gauge, which has to be above <see cref="Min"/>. Defaults to 100.
    /// </summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>Text shown with the gauge that says what is measured, such as "Server load". It names the gauge
    /// unless <see cref="AriaLabel"/> is set.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The accessible name, saying what is measured. Required without a <see cref="Label"/>. Screen readers
    /// announce it with the value.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>A .NET number format for the value's text, such as <c>"0'%'"</c> or <c>"N1"</c>, in the current
    /// culture. The ends shown with <see cref="ShowEnds"/> use it too.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>The color of the gauge's filled part while the value is in no range that has a tone of its own.
    /// Defaults to <see cref="Trailblazor.Tone.Primary"/>.</summary>
    [Parameter]
    public Tone Tone { get; set; } = Tone.Primary;

    /// <summary>Shows <see cref="Min"/> and <see cref="Max"/> as text at the gauge's two ends.</summary>
    [Parameter]
    public bool ShowEnds { get; set; }

    /// <summary>The words the gauge shows and says to screen readers.</summary>
    [Parameter]
    public GaugeLabels Labels { get; set; } = new();

    /// <summary>The gauge's ranges: <see cref="TbGaugeRange"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Name is null && string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException(
                $"{GetType().Name} needs a {nameof(Label)} or an {nameof(AriaLabel)} to name it.");
        }

        if (!(Max > Min) || !double.IsFinite(Max - Min))
        {
            throw new InvalidOperationException(
                $"{GetType().Name} needs a {nameof(Max)} above its {nameof(Min)}, and both finite.");
        }
    }

    private protected string? Name => string.IsNullOrWhiteSpace(AriaLabel) ? null : AriaLabel;

    // What the gauge shows: the value held within the ends, how far along the gauge it is (null without a value),
    // its text, what screen readers hear for it, and the range it is in.
    private protected sealed record Reading(double Now, double? Share, string Text, string Spoken,
        TbGaugeRange? Range);

    private protected Reading Read(IReadOnlyList<(double From, double To, TbGaugeRange Range)> bands)
    {
        if (Value is not { } value || !double.IsFinite(value))
        {
            // A meter has to have a value, so it is at its lowest, and screen readers hear the text in its place.
            var none = Labels.NoValue ?? Localizer[TbStrings.GaugeNoValue];
            return new(Min, null, none, none, null);
        }

        // A range holds the value it ends at, so a value on the line between two ranges is in the lower one.
        var now = Math.Clamp(value, Min, Max);
        var range = bands.FirstOrDefault(band => now <= band.To).Range;
        var text = Text(value);
        var spoken = string.IsNullOrWhiteSpace(range?.Name) ? text : string.Format(CultureInfo.CurrentCulture,
            Labels.ValueInRange ?? Localizer[TbStrings.GaugeValueInRange], text, range.Name);
        return new(now, Share(now), text, spoken, range);
    }

    private protected double Share(double value) => (value - Min) / (Max - Min);

    private protected string Text(double value) => value.ToString(Format, CultureInfo.CurrentCulture);

    // CSS and ARIA need a period as the decimal separator whatever the current culture is.
    private protected static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
