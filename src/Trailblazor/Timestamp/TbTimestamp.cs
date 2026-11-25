using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor;

/// <summary>A date and time, shown in the app's time zone and the user's culture in a <c>time</c> element that machines
/// can read too, such as when a membership expires.</summary>
public partial class TbTimestamp : IDisposable
{
    // A timer can wait about 49 days at most, so one for a later moment wakes to wait again.
    private static readonly TimeSpan LongestWait = TimeSpan.FromDays(40);

    private ITimer? _turn;

    /// <summary>The moment to show. A null value shows a dash, which screen readers hear as "Not set".</summary>
    [Parameter]
    public DateTimeOffset? Value { get; set; }

    /// <summary>Whether it shows the date, the time, or both. Defaults to <see cref="TimestampFormat.DateTime"/>.
    /// </summary>
    [Parameter]
    public TimestampFormat Format { get; set; } = TimestampFormat.DateTime;

    /// <summary>A .NET date and time format, such as <c>"MMMM d, yyyy"</c>, in place of <see cref="Format"/>.
    /// </summary>
    [Parameter]
    public string? FormatString { get; set; }

    /// <summary>The zone it shows the moment in. Left null, it uses a <see cref="TimeZoneInfo"/> cascaded around it,
    /// then <see cref="TimeSettings.TimeZone"/>, then the <see cref="TimeProvider"/>'s local zone.</summary>
    [Parameter]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>Adds the zone after the time: its UTC offset, such as "UTC−5", or the abbreviation the app gives in
    /// <see cref="TimeSettings.Abbreviations"/>, such as "EST".</summary>
    [Parameter]
    public bool ShowZone { get; set; }

    /// <summary>The words around the moment once it has passed, with <c>{0}</c> for it, such as "Expired {0}".
    /// </summary>
    [Parameter]
    public string? PastText { get; set; }

    /// <summary>The words around the moment while it's still ahead, with <c>{0}</c> for it, such as "Expires {0}".
    /// The timestamp switches to <see cref="PastText"/> as the moment passes.</summary>
    [Parameter]
    public string? FutureText { get; set; }

    [CascadingParameter]
    private TimeZoneInfo? CascadedTimeZone { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private TimeProvider Time => Services.GetService<TimeProvider>() ?? TimeProvider.System;

    private string TextOf(DateTimeOffset value)
    {
        var zone = ShownTime.Zone(TimeZone, CascadedTimeZone, Services);
        var shown = TimeZoneInfo.ConvertTime(value, zone);
        var text = shown.ToString(FormatString ?? Format switch
        {
            TimestampFormat.Date => "d",
            TimestampFormat.Time => "t",
            _ => "g",
        }, CultureInfo.CurrentCulture);
        if (ShowZone)
        {
            text += " " + ShownTime.ZoneName(shown, zone, Services, Localizer[TbStrings.TimestampZone]);
        }

        var words = value <= Time.GetUtcNow() ? PastText : FutureText;
        return words is null ? text : string.Format(CultureInfo.CurrentCulture, words, text);
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => WaitForTheMoment();

    // With FutureText or PastText set, a moment still ahead renders again once it passes.
    private void WaitForTheMoment()
    {
        _turn?.Dispose();
        _turn = null;
        var left = Value - Time.GetUtcNow();
        if (left > TimeSpan.Zero && (PastText ?? FutureText) is not null && RendererInfo.IsInteractive)
        {
            _turn = Time.CreateTimer(_ => _ = InvokeAsync(() =>
            {
                WaitForTheMoment();
                StateHasChanged();
            }), null, left.Value < LongestWait ? left.Value : LongestWait, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _turn?.Dispose();
}
