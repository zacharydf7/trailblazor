using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A set of slides shown one at a time, such as trail photos, with previous and next buttons, a button for
/// each slide and, with <see cref="AutoPlayInterval"/>, slides that turn by themselves.</summary>
public partial class TbCarousel
{
    private readonly string _slidesId = $"tb-carousel-{Guid.NewGuid():N}-slides";
    private readonly List<TbCarouselSlide> _slides = [];
    private ElementReference _section;
    private int _current;
    private int _lastIndex;
    private int _commits;
    // Set when a removed slide moved the shown one, which is reported once the render is done.
    private bool _indexMoved;
    private bool _stopped;
    private bool _hovered;
    private bool _focused;
    private bool _attaching;
    // Set when the script reports that motion isn't reduced; from then on, a turn moves the slides rather than
    // swapping them.
    private bool _moves;
    // While a move runs, the slide it moves out of view, which stays drawn until the script says the move has ended.
    private int? _leaving;
    private bool _back;
    private int _move;
    // Set once the script has checked for reduced motion; the slides turn only after that.
    private IJSObjectReference? _handle;
    private DotNetObjectReference<CarouselEvents>? _events;
    private ITimer? _timer;
    private TimeSpan _timerInterval;
    private bool _disposed;
    private readonly ItemHandlers<int, Func<Task>> _goTo;

    /// <summary>Creates the carousel.</summary>
    public TbCarousel() => _goTo = new(index => () => PickAsync(index));

    /// <summary>Names the carousel for screen readers, such as "Trail photos". Required.</summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = "";

    /// <summary>The carousel's <see cref="TbCarouselSlide"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The shown slide's position, from 0. Bind it with <c>@bind-ActiveIndex</c>.</summary>
    [Parameter]
    public int ActiveIndex { get; set; }

    /// <summary>Raised when another slide is shown.</summary>
    [Parameter]
    public EventCallback<int> ActiveIndexChanged { get; set; }

    /// <summary>Moves from the last slide to the first, and back, instead of stopping at the ends. On by default.
    /// </summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>How often the carousel turns to the next slide by itself. Setting it adds a button that stops and
    /// starts the turning. It pauses while the pointer is over the carousel or focus is in it, stops when keyboard focus
    /// lands anywhere in it, and starts stopped for someone who asks for reduced motion.</summary>
    [Parameter]
    public TimeSpan? AutoPlayInterval { get; set; }

    /// <summary>Shows a button for each slide under the slides. On by default.</summary>
    [Parameter]
    public bool ShowPicker { get; set; } = true;

    /// <summary>How the slides move when the carousel turns. <see cref="CarouselTransition.Slide"/> by default; set
    /// <see cref="CarouselTransition.None"/> for an instant turn. The <c>--tb-carousel-duration</c> token sets how long
    /// a move takes.</summary>
    [Parameter]
    public CarouselTransition Transition { get; set; }

    /// <summary>Replaces the words the carousel says to screen readers.</summary>
    [Parameter]
    public CarouselLabels Labels { get; set; } = new();

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    private int Current => _slides.Count == 0 ? 0 : Math.Clamp(_current, 0, _slides.Count - 1);

    private bool AutoPlaying => AutoPlayInterval is not null && _handle is not null && !_stopped && !_hovered
        && !_focused && _slides.Count > 1;

    // Auto-play waits out a move, so each interval starts once the slide has arrived.
    private bool Rotating => AutoPlaying && _leaving is null;

    // The script pauses slides that turn by themselves and ends each move between slides.
    private bool NeedsScript => AutoPlayInterval is not null || Transition != CarouselTransition.None;

    private string Text(string? given, string key) => given ?? Localizer[key];

    private string RotationLabel => _stopped ? Text(Labels.Start, TbStrings.CarouselStart)
        : Text(Labels.Stop, TbStrings.CarouselStop);

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    internal void Add(TbCarouselSlide slide)
    {
        _slides.Add(slide);
        StateHasChanged();
    }

    // The shown slide stays shown when another goes; when it goes itself, the one after it, or else the last, shows.
    internal void Remove(TbCarouselSlide slide)
    {
        var at = _slides.IndexOf(slide);
        if (at < 0 || _disposed)
        {
            return;
        }

        var shown = Current;
        _slides.RemoveAt(at);
        _leaving = null;
        _current = at < shown ? shown - 1 : Current;
        _indexMoved = _current != shown;
        StateHasChanged();
    }

    internal bool IsShown(TbCarouselSlide slide) => _slides.IndexOf(slide) == Current;

    internal bool IsLeaving(TbCarouselSlide slide) => _leaving is { } leaving && _slides.IndexOf(slide) == leaving;

    internal string SlideName(TbCarouselSlide slide) => Format(Text(Labels.SlideOf, TbStrings.CarouselSlideOf),
        _slides.IndexOf(slide) + 1, _slides.Count);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbCarousel)} needs an {nameof(AriaLabel)}.");
        }

        // A new ActiveIndex from the parent shows that slide at once and ends any move. An index the carousel
        // reported itself, which a bound parent passes back, changes nothing.
        if (ActiveIndex != _lastIndex)
        {
            if (ActiveIndex != Current)
            {
                _leaving = null;
            }

            _current = _lastIndex = ActiveIndex;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_indexMoved)
        {
            _indexMoved = false;
            _lastIndex = _current;
            await ActiveIndexChanged.InvokeAsync(_current);
        }

        if (NeedsScript && !_attaching && RendererInfo.IsInteractive)
        {
            await AttachAsync();
        }

        UpdateTimer();
    }

    // Runs the first time the carousel needs its script, for AutoPlayInterval or a transition, which needn't be on
    // the first render.
    private async Task AttachAsync()
    {
        _attaching = true;
        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/carousel.js");
            if (_disposed)
            {
                return;
            }

            _events = DotNetObjectReference.Create(new CarouselEvents(OnFocusEnter, OnFocusLeave, OnMoveEnd));
            var handle = await module.InvokeAsync<IJSObjectReference?>("createCarousel", _section, _events);
            if (handle is null)
            {
                return;
            }

            // Focus or the stop button may have stopped the slides while this was awaited.
            var reduced = await handle.InvokeAsync<bool>("prefersReducedMotion");
            _stopped |= reduced;
            _moves = !reduced;
            if (_disposed)
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            return;
        }

        StateHasChanged();
    }

    private async Task GoToAsync(int index)
    {
        if (_slides.Count == 0)
        {
            return;
        }

        var next = Loop ? (index % _slides.Count + _slides.Count) % _slides.Count
            : Math.Clamp(index, 0, _slides.Count - 1);
        if (next == Current)
        {
            return;
        }

        // A turn during a move replaces it: the slide that was arriving moves out, and the one leaving goes at once.
        // Wrapping keeps the direction of the button pressed, so Next from the last slide moves forward.
        if (_moves && Transition != CarouselTransition.None)
        {
            _leaving = Current;
            _back = index < Current;
            _move++;
        }

        _current = next;
        await ActiveIndexChanged.InvokeAsync(next);
        StateHasChanged();
    }

    // Counts a pick that moves the slides once ActiveIndexChanged has run, so a test can wait for the app's answer.
    private async Task PickAsync(int index)
    {
        if (index == Current)
        {
            return;
        }

        try
        {
            await GoToAsync(index);
        }
        finally
        {
            _commits++;
        }
    }

    // Starting the slides again also lifts the pause for focus already in the carousel, since the user asked for them.
    private void ToggleRotation()
    {
        _stopped = !_stopped;
        _focused = false;
    }

    // Keyboard focus anywhere in it stops the slides until the user starts them again; focus from a pointer only
    // pauses them.
    private void OnFocusEnter(bool fromKeyboard)
    {
        _focused = true;
        _stopped |= fromKeyboard;
        StateHasChanged();
    }

    private void OnFocusLeave()
    {
        _focused = false;
        StateHasChanged();
    }

    // Only the latest move's end hides the slide it moved out; a move a later turn replaced has nothing to finish.
    private void OnMoveEnd(int move)
    {
        if (move == _move && _leaving is not null)
        {
            _leaving = null;
            StateHasChanged();
        }
    }

    // The timer runs only while the slides should turn, and starts over after each pause or a new interval.
    private void UpdateTimer()
    {
        if (!Rotating || _timerInterval != AutoPlayInterval)
        {
            _timer?.Dispose();
            _timer = null;
        }

        if (Rotating && _timer is null)
        {
            _timerInterval = AutoPlayInterval!.Value;
            ITimer? timer = null;
            timer = _timer = Time.CreateTimer(_ => _ = InvokeAsync(() => TurnFromTimerAsync(timer)), null,
                _timerInterval, _timerInterval);
        }
    }

    // Runs outside any event handler, so it hands a failing ActiveIndexChanged to the renderer.
    private async Task TurnFromTimerAsync(ITimer? timer)
    {
        // A tick that fired before a pause, a new interval or disposal can run after it, so it checks it still counts.
        if (timer != _timer || _timerInterval != AutoPlayInterval || !Rotating)
        {
            return;
        }

        try
        {
            await GoToAsync(Current + 1);
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
