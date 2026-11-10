using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A set of slides shown one at a time, such as trail photos, with previous and next buttons, a button for
/// each slide and, with <see cref="AutoPlay"/>, slides that turn by themselves.</summary>
public partial class TbCarousel
{
    private readonly string _slidesId = $"tb-carousel-{Guid.NewGuid():N}-slides";
    private readonly List<TbCarouselSlide> _slides = [];
    private ElementReference _section;
    private int _current;
    private int _lastIndex;
    // Set when a removed slide moved the shown one, which is reported once the render is done.
    private bool _indexMoved;
    private bool _stopped;
    private bool _hovered;
    private bool _focused;
    private bool _attaching;
    // Set once the script has checked for reduced motion; the slides turn only after that.
    private IJSObjectReference? _handle;
    private DotNetObjectReference<CarouselEvents>? _events;
    private ITimer? _timer;
    private TimeSpan _timerInterval;
    private bool _disposed;
    private readonly ItemHandlers<int, Func<Task>> _goTo;

    /// <summary>Creates the carousel.</summary>
    public TbCarousel() => _goTo = new(index => () => GoToAsync(index));

    /// <summary>Names the carousel for screen readers, such as "Trail photos". Required.</summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = "";

    /// <summary>The carousel's <see cref="TbCarouselSlide"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The shown slide's position, from 0. Bind it with <c>@bind-Index</c>.</summary>
    [Parameter]
    public int Index { get; set; }

    /// <summary>Raised when another slide is shown.</summary>
    [Parameter]
    public EventCallback<int> IndexChanged { get; set; }

    /// <summary>Moves from the last slide to the first, and back, instead of stopping at the ends. On by default.
    /// </summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>How often the carousel turns to the next slide by itself. Setting it adds a button that stops and
    /// starts the turning. It pauses while the pointer is over the carousel or focus is in it, stops when keyboard focus
    /// lands anywhere in it, and starts stopped for someone who asks for reduced motion.</summary>
    [Parameter]
    public TimeSpan? AutoPlay { get; set; }

    /// <summary>Shows a button for each slide under the slides. On by default.</summary>
    [Parameter]
    public bool ShowPicker { get; set; } = true;

    /// <summary>Replaces the words the carousel says to screen readers.</summary>
    [Parameter]
    public CarouselLabels? Labels { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    private int Current => _slides.Count == 0 ? 0 : Math.Clamp(_current, 0, _slides.Count - 1);

    private bool Rotating => AutoPlay is not null && _handle is not null && !_stopped && !_hovered && !_focused
        && _slides.Count > 1;

    private string Text(string? given, string key) => given ?? Localizer[key];

    private string RotationLabel => _stopped ? Text(Labels?.Start, TbStrings.CarouselStart)
        : Text(Labels?.Stop, TbStrings.CarouselStop);

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
        _current = at < shown ? shown - 1 : Current;
        _indexMoved = _current != shown;
        StateHasChanged();
    }

    internal bool IsShown(TbCarouselSlide slide) => _slides.IndexOf(slide) == Current;

    internal string SlideName(TbCarouselSlide slide) => Format(Text(Labels?.SlideOf, TbStrings.CarouselSlideOf),
        _slides.IndexOf(slide) + 1, _slides.Count);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbCarousel)} needs an {nameof(AriaLabel)}.");
        }

        // A new Index from the parent shows that slide; one the carousel reported itself changes nothing.
        if (Index != _lastIndex)
        {
            _current = _lastIndex = Index;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_indexMoved)
        {
            _indexMoved = false;
            _lastIndex = _current;
            await IndexChanged.InvokeAsync(_current);
        }

        if (AutoPlay is not null && !_attaching && RendererInfo.IsInteractive)
        {
            await AttachAsync();
        }

        UpdateTimer();
    }

    // Runs once AutoPlay is first set, which needn't be on the first render.
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

            _events = DotNetObjectReference.Create(new CarouselEvents(OnFocusEnter, OnFocusLeave));
            var handle = await module.InvokeAsync<IJSObjectReference?>("createCarousel", _section, _events);
            if (handle is null)
            {
                return;
            }

            // Focus or the stop button may have stopped the slides while this was awaited.
            _stopped |= await handle.InvokeAsync<bool>("prefersReducedMotion");
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

        _current = next;
        await IndexChanged.InvokeAsync(next);
        StateHasChanged();
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

    // The timer runs only while the slides should turn, and starts over after each pause or a new interval.
    private void UpdateTimer()
    {
        if (!Rotating || _timerInterval != AutoPlay)
        {
            _timer?.Dispose();
            _timer = null;
        }

        if (Rotating && _timer is null)
        {
            _timerInterval = AutoPlay!.Value;
            _timer = Time.CreateTimer(_ => _ = InvokeAsync(TurnFromTimerAsync), null, _timerInterval, _timerInterval);
        }
    }

    // Runs outside any event handler, so it hands a failing IndexChanged to the renderer.
    private async Task TurnFromTimerAsync()
    {
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
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
