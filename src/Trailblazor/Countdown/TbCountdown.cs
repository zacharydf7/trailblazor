using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor;

/// <summary>A timed action the user can see coming and stop or extend, such as going to the sign-in page after a
/// password reset or signing out after a while idle, as WCAG 2.2.1 asks. It shows the time left with buttons to go now,
/// stay or extend, and pauses while focus or the pointer is on it.</summary>
public partial class TbCountdown : IDisposable
{
    // Screen readers hear a long countdown once more this long before its end.
    private static readonly TimeSpan Warning = TimeSpan.FromSeconds(20);

    private ElementReference _root;
    private ITimer? _tick;
    private TimeSpan _left;
    private DateTimeOffset? _resumedAt;
    private bool _focusInside;
    private bool _pointerInside;
    private bool _warned;
    private bool _started;
    private bool _disposed;
    private CountdownState _state;

    /// <summary>How long it counts down before it acts: more than zero, and at least 20 seconds so people can read it
    /// and act. Required.</summary>
    [Parameter, EditorRequired]
    public TimeSpan Duration { get; set; }

    /// <summary>The sentence it shows, with <c>{0}</c> for the time left, such as "Going to sign in in {0}.". Required.
    /// </summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = "";

    /// <summary>Where it goes when the time runs out or the user presses Go now.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Runs when the time runs out or the user presses Go now, such as signing the user out. It gets which
    /// of the two happened, so an app that stays on the page can move focus after Go now.</summary>
    [Parameter]
    public EventCallback<CountdownEnd> OnElapsed { get; set; }

    /// <summary>Adds a button that runs this, such as refreshing the session, and then starts the countdown over.
    /// <see cref="ExtendText"/> names the button.</summary>
    [Parameter]
    public EventCallback OnExtend { get; set; }

    /// <summary>The name of the button <see cref="OnExtend"/> adds, such as "Stay signed in". Required with it.
    /// </summary>
    [Parameter]
    public string? ExtendText { get; set; }

    /// <summary>Whether it shows the button that stops it, true by default. Set it false only with
    /// <see cref="OnExtend"/>, such as for a session that ends on the server whether or not the page stays.</summary>
    [Parameter]
    public bool ShowStay { get; set; } = true;

    /// <summary>The words it shows, in place of the app's text for the <see cref="TbStrings"/> keys.</summary>
    [Parameter]
    public CountdownLabels Labels { get; set; } = new();

    /// <summary>A label this countdown is counted under, such as <c>"reset-redirect"</c>, through the
    /// <c>Trailblazor</c> meter: in <c>trailblazor.control.shown</c> when it first renders, and in
    /// <c>trailblazor.control.used</c> with the action <c>go_now</c>, <c>stay</c>, <c>extend</c> or <c>elapsed</c>.
    /// A high share of <c>stay</c> says people don't want to be taken away.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private TimeProvider Time => Services.GetService<TimeProvider>() ?? TimeProvider.System;

    private TimeSpan Left
    {
        get
        {
            var left = _resumedAt is { } resumed ? _left - (Time.GetUtcNow() - resumed) : _left;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }

    private bool Acts => Href is not null || OnElapsed.HasDelegate;

    private string DataState => _state == CountdownState.Running && _started && _resumedAt is null ? "paused"
        : _state.ToString().ToLowerInvariant();

    private string GoNowText => Labels.GoNow ?? Localizer[TbStrings.CountdownGoNow];

    private string StayText => Labels.Stay ?? Localizer[TbStrings.CountdownStay];

    private string StayedText => Labels.Stayed ?? Localizer[TbStrings.CountdownStayed];

    // The time left in whole seconds under a minute, and whole minutes, rounded up, from a minute on.
    private string LeftText
    {
        get
        {
            var seconds = (int)Math.Ceiling(Left.TotalSeconds);
            var (key, count) = seconds < 60 ? (TbStrings.CountdownSeconds, seconds)
                : (TbStrings.CountdownMinutes, (int)Math.Ceiling(seconds / 60.0));
            var words = string.Format(CultureInfo.CurrentCulture, Localizer.Plural(key, count), count);
            return string.Format(CultureInfo.CurrentCulture, Text, words);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Duration, TimeSpan.Zero, nameof(Duration));
        if (string.IsNullOrWhiteSpace(Text))
        {
            throw new InvalidOperationException(
                $"{nameof(TbCountdown)} needs a {nameof(Text)}, such as \"Going to sign in in {{0}}.\".");
        }

        if (OnExtend.HasDelegate && string.IsNullOrWhiteSpace(ExtendText))
        {
            throw new InvalidOperationException(
                $"{nameof(TbCountdown)} needs an {nameof(ExtendText)} to name the button {nameof(OnExtend)} adds.");
        }

        if (!ShowStay && !OnExtend.HasDelegate)
        {
            throw new InvalidOperationException($"{nameof(TbCountdown)} needs a way to stop or extend it: keep "
                + $"{nameof(ShowStay)}, or add {nameof(OnExtend)}.");
        }

        if (!_started)
        {
            _left = Duration;
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);
        if (firstRender && RendererInfo.IsInteractive)
        {
            // It starts once the page is interactive, so a static page shows the full time and never acts.
            _started = true;
            Announcer.Polite(LeftText);
            Resume();
        }
    }

    private void Resume()
    {
        if (_state != CountdownState.Running || _focusInside || _pointerInside || _disposed)
        {
            return;
        }

        _resumedAt = Time.GetUtcNow();
        Arm();
    }

    private void Pause()
    {
        if (_resumedAt is null)
        {
            return;
        }

        _left = Left;
        _resumedAt = null;
        _tick?.Dispose();
    }

    // Wakes when the shown time next changes, or at the end: each second under a minute, and from a minute on, as
    // the rounded-up minutes change.
    private void Arm()
    {
        var left = Left;
        var seconds = Math.Ceiling(left.TotalSeconds);
        var next = seconds > 60 ? (Math.Ceiling(seconds / 60) - 1) * 60 : seconds - 1;
        var wait = left - TimeSpan.FromSeconds(next);
        _tick?.Dispose();
        _tick = Time.CreateTimer(_ => _ = InvokeAsync(TickAsync), null, wait > TimeSpan.Zero ? wait : TimeSpan.Zero,
            Timeout.InfiniteTimeSpan);
    }

    private async Task TickAsync()
    {
        // A tick queued before a pause, a stop or the countdown going away can run after it.
        if (_disposed || _resumedAt is null || _state != CountdownState.Running)
        {
            return;
        }

        if (Left <= TimeSpan.Zero)
        {
            // Runs outside any event handler, so it hands a failing OnElapsed to the renderer.
            try
            {
                await ActAsync("elapsed", CountdownEnd.TimeRanOut);
            }
            catch (Exception exception)
            {
                await DispatchExceptionAsync(exception);
            }

            return;
        }

        if (!_warned && Duration > Warning * 2 && Left <= Warning)
        {
            _warned = true;
            Announcer.Polite(LeftText);
        }

        Arm();
        StateHasChanged();
    }

    private async Task ActAsync(string action, CountdownEnd end)
    {
        Pause();
        _state = CountdownState.Done;
        Telemetry.Used(TelemetryName, action);
        StateHasChanged();
        if (Href is not null)
        {
            Navigation.NavigateTo(Href);
        }

        await OnElapsed.InvokeAsync(end);
    }

    private Task GoNowAsync() => ActAsync("go_now", CountdownEnd.GoNow);

    private async Task StayAsync()
    {
        Pause();
        _state = CountdownState.Stayed;
        Telemetry.Used(TelemetryName, "stay");
        // The buttons go, so focus goes to the countdown, which now says it stayed.
        StateHasChanged();
        await _root.FocusAsync();
    }

    private async Task ExtendAsync()
    {
        Telemetry.Used(TelemetryName, "extend");
        await OnExtend.InvokeAsync();
        Pause();
        (_left, _warned) = (Duration, false);
        Resume();
        Announcer.Polite(LeftText);
    }

    private void FocusIn(FocusEventArgs _)
    {
        _focusInside = true;
        Pause();
    }

    private void FocusOut(FocusEventArgs _)
    {
        _focusInside = false;
        Resume();
    }

    private void PointerEnter(PointerEventArgs _)
    {
        _pointerInside = true;
        Pause();
    }

    private void PointerLeave(PointerEventArgs _)
    {
        _pointerInside = false;
        Resume();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _tick?.Dispose();
    }

    private enum CountdownState
    {
        Running,
        Stayed,
        Done,
    }
}
