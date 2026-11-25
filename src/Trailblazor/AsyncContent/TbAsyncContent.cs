using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Trailblazor;

/// <summary>A region that loads its content. It runs <see cref="Load"/> and shows a spinner while a first load is slow,
/// a message with Retry when it fails, <see cref="EmptyContent"/> or <see cref="NoMatchesContent"/> when there's
/// nothing to show, and otherwise its <see cref="ChildContent"/>. A reload keeps the content in view.</summary>
public partial class TbAsyncContent : IDisposable
{
    private readonly CancellationTokenSource _gone = new();
    private CancellationTokenSource? _loading;
    private ElementReference _root;
    private bool _started;
    private object? _loadedKey;
    private bool _loaded;
    private bool _running;
    private bool _busyShown;
    private DateTimeOffset _busyShownAt;
    private ITimer? _busyTimer;
    private Exception? _failure;

    /// <summary>Loads the content into the app's own fields, such as a list of groups. It's given a token that is
    /// canceled when the region loads again or goes away, and a load canceled this way is ignored; anything else it
    /// throws, including its own timeout, is a failure. Once it finishes, the region renders again. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<CancellationToken, Task>? Load { get; set; }

    /// <summary>What it loads, such as <c>"groups"</c> or <c>"the profile"</c>, which names the spinner, "Loading
    /// groups", and the failure, "Unable to load groups.". Required.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>Whether the load found nothing, so <see cref="EmptyContent"/> shows in place of the content, such as
    /// <c>() => _groups.Count == 0</c>. The region asks each time it renders, so it reads what <see cref="Load"/>
    /// filled.</summary>
    [Parameter]
    public Func<bool>? IsEmpty { get; set; }

    /// <summary>Whether the app's search or filter matches nothing that was loaded, so
    /// <see cref="NoMatchesContent"/> shows in place of the content, asked each time the region renders.</summary>
    [Parameter]
    public Func<bool>? HasNoMatches { get; set; }

    /// <summary>A value the content depends on, such as an id from the route. When it changes, the region loads
    /// again.</summary>
    [Parameter]
    public object? LoadKey { get; set; }

    /// <summary>The content, shown once it has loaded.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Shown in place of the spinner while a first load is slow, such as a <see cref="TbSkeleton"/> in the
    /// content's shape. Screen readers still hear the loading words.</summary>
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    /// <summary>Shown when <see cref="IsEmpty"/> is true, such as a <see cref="TbEmptyState"/>. Without it, the
    /// content shows.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Shown when <see cref="HasNoMatches"/> is true, such as a message with a way to clear the search.
    /// Without it, the content shows.</summary>
    [Parameter]
    public RenderFragment? NoMatchesContent { get; set; }

    /// <summary>Shown in place of the message "Unable to load …" and its Retry button when a load fails, given the
    /// exception and a way to retry. The failure is still announced with that message, so set
    /// <see cref="AsyncContentLabels.LoadFailed"/> to match.</summary>
    [Parameter]
    public RenderFragment<AsyncContentFailure>? FailedContent { get; set; }

    /// <summary>Called with the exception when a load fails, such as to log it.</summary>
    [Parameter]
    public EventCallback<Exception> OnLoadFailed { get; set; }

    /// <summary>The words it shows and says, in place of the app's text for the <see cref="TbStrings"/> keys.
    /// </summary>
    [Parameter]
    public AsyncContentLabels Labels { get; set; } = new();

    /// <summary>A label this region is counted under, such as <c>"trip-groups"</c>, through the <c>Trailblazor</c>
    /// meter: in <c>trailblazor.control.shown</c> when it first renders, <c>trailblazor.load.duration</c> with how
    /// long each load took, <c>trailblazor.control.failed</c> for each failure and <c>trailblazor.control.used</c> for
    /// each retry. Nothing it loads is recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    private TimeProvider Time => Services.GetService<TimeProvider>() ?? TimeProvider.System;

    private BusySettings Busy => Services.GetRequiredService<IOptions<TrailblazorOptions>>().Value.Busy;

    // While the page prerenders, the load waits for the interactive render, so it runs once.
    private bool Prerendering => !RendererInfo.IsInteractive && AssignedRenderMode is not null;

    // The first load says it's loading through the status, so only a later load marks the region busy; a busy
    // region could hold back what its status says.
    private bool Reloading => _running && (_loaded || _failure is not null);

    private AsyncContentState State => _failure is not null ? AsyncContentState.Failed
        : !_loaded ? AsyncContentState.Loading
        : IsEmpty?.Invoke() == true ? AsyncContentState.Empty
        : HasNoMatches?.Invoke() == true ? AsyncContentState.NoMatches
        : AsyncContentState.Content;

    // A reload that fails keeps what loaded before in view, under the failure.
    private RenderFragment? ShownContent => IsEmpty?.Invoke() == true ? EmptyContent ?? ChildContent
        : HasNoMatches?.Invoke() == true ? NoMatchesContent ?? ChildContent
        : ChildContent;

    private bool ShowsSpinner => _busyShown && !_loaded && _failure is null;

    private bool ShowsProgress => _busyShown && _loaded && _failure is null;

    private string LoadingText => Labels.Loading
        ?? string.Format(CultureInfo.CurrentCulture, Localizer[TbStrings.AsyncContentLoading], Label);

    private string FailedText => Labels.LoadFailed
        ?? string.Format(CultureInfo.CurrentCulture, Localizer[TbStrings.AsyncContentLoadFailed], Label);

    private string RetryText => Labels.Retry ?? Localizer[TbStrings.AsyncContentRetry];

    /// <summary>Loads the content again. What is shown stays while it loads.</summary>
    public Task ReloadAsync() => InvokeAsync(LoadAsync);

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        if (Load is null)
        {
            throw new InvalidOperationException($"{nameof(TbAsyncContent)} needs a {nameof(Load)} function.");
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException(
                $"{nameof(TbAsyncContent)} needs a {nameof(Label)} that says what it loads, such as \"groups\".");
        }

        if (Prerendering || (_started && Equals(_loadedKey, LoadKey)))
        {
            return Task.CompletedTask;
        }

        (_started, _loadedKey) = (true, LoadKey);
        return LoadAsync();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    private async Task LoadAsync()
    {
        if (_gone.IsCancellationRequested)
        {
            return;
        }

        _loading?.Cancel();
        _loading?.Dispose();
        _loading = CancellationTokenSource.CreateLinkedTokenSource(_gone.Token);
        var token = _loading.Token;
        var started = Time.GetTimestamp();
        _running = true;
        if (!_busyShown && RendererInfo.IsInteractive)
        {
            SetBusyTimer();
        }

        Exception? failure = null;
        try
        {
            await Load!(token);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        // A load that was replaced, or whose region went away, has its token canceled, so whatever it ended with is
        // ignored. Load is awaited directly, as an EventCallback would swallow a load's own cancellation.
        var took = Time.GetElapsedTime(started);
        if (token.IsCancellationRequested || !await KeptBusyLongEnoughAsync(token))
        {
            return;
        }

        (_running, _busyShown, _failure) = (false, false, failure);
        if (failure is null)
        {
            _loaded = true;
        }

        _busyTimer?.Dispose();
        Telemetry.Loaded(TelemetryName, failure is null ? "loaded" : "failed", took);
        if (failure is not null)
        {
            Telemetry.Failed(TelemetryName, Label, "load", failure);
            Announcer.Polite(FailedText);
            await OnLoadFailed.InvokeAsync(failure);
        }

        StateHasChanged();
    }

    // A spinner that showed stays for the minimum, so it doesn't flicker off just after it appears.
    private async Task<bool> KeptBusyLongEnoughAsync(CancellationToken token)
    {
        var left = _busyShown ? _busyShownAt + Busy.Minimum - Time.GetUtcNow() : TimeSpan.Zero;
        if (left > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(left, Time, token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return true;
    }

    private void SetBusyTimer()
    {
        _busyTimer?.Dispose();
        _busyTimer = Time.CreateTimer(_ => _ = InvokeAsync(() =>
        {
            if (_running)
            {
                (_busyShown, _busyShownAt) = (true, Time.GetUtcNow());
                StateHasChanged();
            }
        }), null, Busy.ShowDelay, Timeout.InfiniteTimeSpan);
    }

    // The failure view stays while a retry runs, so its Retry button keeps focus and shows that it's busy. Once the
    // content loads, focus goes to the region, since the button it was on is gone.
    private async Task RetryAsync()
    {
        Telemetry.Used(TelemetryName, "retried");
        await LoadAsync();
        if (_failure is null && _loaded)
        {
            await _root.FocusAsync();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gone.Cancel();
        _gone.Dispose();
        _loading?.Dispose();
        _busyTimer?.Dispose();
    }
}
