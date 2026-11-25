using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Trailblazor;

/// <summary>What <see cref="TbButton"/> and <see cref="TbIconButton"/> share.</summary>
public abstract partial class TbButtonBase : TbComponentBase, IDisposable
{
    private ElementReference _element;
    // How many clicks the button has finished handling; the Playwright driver waits for it to change.
    private int _clicks;
    private ControlTelemetry? _telemetry;
    private bool _hearsForms;
    private bool _givenVariant;
    private bool _givenTone;
    private bool _givenSize;
    // Whether a click's OnClick task is still running, whether its spinner shows, and since when.
    private bool _running;
    private bool _busyShown;
    private DateTimeOffset _busyShownAt;
    private ITimer? _busyTimer;
    // The running cooldown's start and length, and the timers that count it down and end it.
    private long? _coolingSince;
    private TimeSpan _coolingFor;
    private ITimer? _cooledTimer;
    private ITimer? _cooldownTimer;
    private readonly string _cooldownId = $"tb-button-cooldown-{Guid.NewGuid():N}";
    private readonly string _reasonId = $"tb-button-reason-{Guid.NewGuid():N}";
    private readonly string _reasonTipId = $"tb-button-reason-tip-{Guid.NewGuid():N}";

    /// <summary>How strongly the button is drawn.</summary>
    [Parameter]
    public Variant Variant { get; set; }

    /// <summary>The button's meaning, which sets its color.</summary>
    [Parameter]
    public Tone Tone { get; set; }

    /// <summary>The button's size. Defaults to <see cref="Trailblazor.Size.Medium"/>.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>Turns the button off: it can't be clicked, and unless <see cref="FocusableWhenDisabled"/> is set it
    /// can't be focused either.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Keeps a disabled button in the tab order, marked <c>aria-disabled</c>, so it can still show a tooltip,
    /// and a keyboard user can still find it and learn why it is off.</summary>
    [Parameter]
    public bool FocusableWhenDisabled { get; set; }

    /// <summary>Why the button is off, shown in a tooltip on hover, keyboard focus and a press, and read as its
    /// description. It applies only while the button is off, so it can stay set when the button turns on. While it
    /// applies, the button stays a tab stop, as with <see cref="FocusableWhenDisabled"/>, and a press only shows the
    /// reason.</summary>
    [Parameter]
    public string? DisabledReason { get; set; }

    /// <summary>Shows a busy spinner and ignores clicks, but keeps the button focusable so focus isn't lost while
    /// the work runs. A submit button shows the same while the <see cref="TbForm"/> it submits handles a submit: the
    /// one its <c>form</c> attribute names, or else the one around it, unless the form's <c>ShowSubmitting</c> is
    /// off.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>The address the button goes to, which makes it a link drawn as a button.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Whether a toggle button is on. Bind it with <c>@bind-Pressed</c> to make the button a toggle, which
    /// screen readers announce as pressed or not. A button with an <see cref="Href"/> can't be a toggle.</summary>
    [Parameter]
    public bool Pressed { get; set; }

    /// <summary>Raised when the user turns a toggle button on or off.</summary>
    [Parameter]
    public EventCallback<bool> PressedChanged { get; set; }

    /// <summary>Shows a toggle button as partly on, like Bold over text that is only partly bold. Screen readers
    /// announce it as mixed. A press turns it on and raises <see cref="PressedChanged"/> with true; the app then sets
    /// this again from what the button now acts on.</summary>
    [Parameter]
    public bool Mixed { get; set; }

    /// <summary>How a toggle button shows that it's pressed. <see cref="Trailblazor.PressedLook.Quiet"/> suits a
    /// <see cref="Trailblazor.Variant.Text"/> toggle among other content, such as on a tree row.</summary>
    [Parameter]
    public PressedLook PressedLook { get; set; }

    /// <summary>What the button does inside a form. Defaults to <see cref="ButtonType.Button"/>. A submit or reset
    /// button turns off with the <see cref="TbForm"/> its <c>form</c> attribute names, wherever the button is, or else
    /// with the form around it, and a submit button shows busy while that form submits.</summary>
    [Parameter]
    public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>Raised when the button is clicked, unless it is disabled or loading, its last click is still running,
    /// or it is a submit button whose form is handling a submit. Await the work in it, so the button stays busy until
    /// the work is done.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Whether the button counts as busy while the task its <see cref="OnClick"/> returned is still running:
    /// it ignores clicks, stays focusable, and after a moment shows its spinner, as with <see cref="Loading"/>. Toggle
    /// buttons and links never do. Defaults to the app's <see cref="ButtonSettings.BusyWhileRunning"/>, which
    /// defaults to true.
    /// </summary>
    [Parameter]
    public bool? BusyWhileRunning { get; set; }

    // A TbButton's Cooldown and its words; the other buttons have no text to show the seconds after.
    private protected virtual TimeSpan? CooldownLength => null;

    private protected virtual string CooldownWords(int seconds) => "";

    /// <summary>A label this button's use is counted under, such as <c>"export-csv"</c>, so the app can count how often
    /// people see it and use it through the <c>Trailblazor</c> meter. Controls that share a label count together; a
    /// control without one isn't counted. It names what the button does, never the user, and must never hold a
    /// credential.
    /// </summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [CascadingParameter]
    private TbButtonGroup? Group { get; set; }

    [CascadingParameter(Name = TbForm.DisabledCascade)]
    private bool FormDisabled { get; set; }

    [CascadingParameter(Name = TbForm.SubmittingCascade)]
    private bool FormSubmitting { get; set; }

    [Inject]
    private protected IServiceProvider Services { get; set; } = default!;

    private protected Variant ShownVariant => TbButtonGroup.Choose(_givenVariant, Variant, Group?.Variant);

    private protected Tone ShownTone => TbButtonGroup.Choose(_givenTone, Tone, Group?.Tone);

    private protected Size ShownSize => TbButtonGroup.Choose(_givenSize, Size, Group?.Size);

    private bool Off => Disabled || Group?.Disabled == true
        || (OwnForm(FormDisabled, form => form.Disabled) && Type is ButtonType.Submit or ButtonType.Reset);

    private protected bool Busy => Loading || _busyShown || (OwnForm(FormSubmitting, form => form.ShowsSubmitting)
        && Type is ButtonType.Submit);

    // Busy, or running a click that hasn't shown its spinner yet: clicks are ignored either way.
    private bool Ignores => Busy || _running;

    private bool Cooling => _coolingSince is not null;

    // The whole seconds left of the cooldown, counting a part second as one.
    // A timer can fire a few milliseconds early, which mustn't round a second up to the one before.
    private static readonly TimeSpan TickLeeway = TimeSpan.FromMilliseconds(50);

    private protected int? CooldownLeft => _coolingSince is { } since
        ? Math.Max(1, (int)Math.Ceiling((_coolingFor - Time.GetElapsedTime(since) - TickLeeway).TotalSeconds))
        : null;


    private string? DescribedBy => ClassNames.Joined([Theirs("aria-describedby") as string,
        Cooling ? _cooldownId : null, ShowsReason ? _reasonId : null], " ");

    // The reason shows while the button is off, and keeps the button a tab stop so the reason can be reached.
    private protected bool ShowsReason => Off && !string.IsNullOrWhiteSpace(DisabledReason);

    private bool KeepsFocus => FocusableWhenDisabled || ShowsReason;

    [CascadingParameter]
    private ReasonTooltip? OuterReasonTooltip { get; set; }

    // The button's own tooltip shows the reason, unless a tooltip around it already does.
    private protected bool TipsReason => ShowsReason && OuterReasonTooltip is null;

    // The name the reason's tooltip shows above the reason, for a button whose name shows only in a tooltip.
    private protected virtual string? TipName => null;

    // A button can render without AddTrailblazor, so its reason's tooltip then falls back to the default delays.
    private TooltipSettings Tooltips => Services.GetService<IOptions<TrailblazorOptions>>()?.Value.Tooltips ?? new();

    private static string Milliseconds(TimeSpan delay) =>
        delay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

    private TrailblazorOptions Options => Services.GetRequiredService<IOptions<TrailblazorOptions>>().Value;

    private TimeProvider Time => Services.GetService<TimeProvider>() ?? TimeProvider.System;

    private bool RunsBusy => !Toggles && Href is null && (BusyWhileRunning ?? Options.Buttons.BusyWhileRunning);

    private string? FormId => Theirs("form") as string;

    private FormRegistry? Registry => field ??= Services.GetService<FormRegistry>();

    private bool OwnForm(bool around, Func<TbForm, bool> named) =>
        FormId is { } id ? Registry?.Find(id) is { } form && named(form) : around;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private string TypeAttribute => Type.ToString().ToLowerInvariant();

    private bool Toggles => PressedChanged.HasDelegate;

    // Still focusable, but nothing happens when it is pressed.
    private bool Inert => Busy || Cooling || (Off && KeepsFocus);

    private int? Clicks => RendererInfo.IsInteractive ? _clicks : null;

    private string? StaticMark => AssignedRenderMode is null && !RendererInfo.IsInteractive ? "" : null;

    private string RootClass(string? extra) => ClassNames.Join("tb-button",
        ClassNames.Modifier("tb-button", ShownVariant), ClassNames.Modifier("tb-button", ShownTone),
        ClassNames.Modifier("tb-button", ShownSize), PressedLook == PressedLook.Quiet ? "tb-button--quiet" : null,
        TipsReason ? "tb-button--reason" : null, extra, Class);

    private object? Theirs(string attribute) => AdditionalAttributes?.GetValueOrDefault(attribute);

    /// <summary>Moves focus to the button.</summary>
    /// <param name="preventScroll">Whether to keep the page from scrolling the button into view.</param>
    public ValueTask FocusAsync(bool preventScroll = false) => _element.FocusAsync(preventScroll);

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // A group fills in only the settings the button wasn't given.
        _givenVariant = parameters.TryGetValue<Variant>(nameof(Variant), out _);
        _givenTone = parameters.TryGetValue<Tone>(nameof(Tone), out _);
        _givenSize = parameters.TryGetValue<Size>(nameof(Size), out _);
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Only a button that names a form hears the forms change, so a page of plain buttons costs nothing.
        if ((FormId is not null) != _hearsForms && Registry is { } registry)
        {
            _hearsForms = !_hearsForms;
            if (_hearsForms)
            {
                registry.Changed += OnFormChanged;
            }
            else
            {
                registry.Changed -= OnFormChanged;
            }
        }

        if (Href is not null && Toggles)
        {
            throw new InvalidOperationException("A button with an Href is a link, which can't be a toggle.");
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    /// <inheritdoc />
    public void Dispose()
    {
        _busyTimer?.Dispose();
        _cooldownTimer?.Dispose();
        _cooledTimer?.Dispose();
        if (_hearsForms)
        {
            Registry!.Changed -= OnFormChanged;
        }
    }

    private void OnFormChanged(string id)
    {
        if (id == FormId)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnClickAsync(MouseEventArgs args)
    {
        try
        {
            await HandleClickAsync(args);
        }
        finally
        {
            _clicks++;
            StateHasChanged();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (Off || Cooling)
        {
            if (ShowsReason)
            {
                // How often people press a button that's off is worth knowing.
                Telemetry.Used(TelemetryName, "pressed_off");
            }

            return;
        }

        if (Ignores)
        {
            // Many of these point to a slow handler.
            Telemetry.Used(TelemetryName, "ignored_busy");
            return;
        }

        Telemetry.Used(TelemetryName);

        if (Toggles)
        {
            Pressed = Mixed || !Pressed;
            await PressedChanged.InvokeAsync(Pressed);
        }

        if (!RunsBusy)
        {
            await OnClick.InvokeAsync(args);
            CoolDown();
            return;
        }

        _running = true;
        SetBusyTimer(ShowBusy, Options.Busy.ShowDelay);
        try
        {
            await OnClick.InvokeAsync(args);
            CoolDown();
        }
        finally
        {
            _running = false;
            var left = _busyShownAt + Options.Busy.Minimum - Time.GetUtcNow();
            if (_busyShown && left > TimeSpan.Zero && !Cooling)
            {
                SetBusyTimer(HideBusy, left);
            }
            else
            {
                _busyTimer?.Dispose();
                _busyShown = false;
            }
        }
    }

    private void SetBusyTimer(Action then, TimeSpan after)
    {
        _busyTimer?.Dispose();
        _busyTimer = Time.CreateTimer(_ => _ = InvokeAsync(() =>
        {
            then();
            StateHasChanged();
        }), null, after, Timeout.InfiniteTimeSpan);
    }

    private void ShowBusy()
    {
        if (_running)
        {
            (_busyShown, _busyShownAt) = (true, Time.GetUtcNow());
        }
    }

    private void HideBusy() => _busyShown = false;

    // Starts the cooldown, announces once how long it lasts, updates the seconds left each second and ends it on time.
    private void CoolDown()
    {
        if (CooldownLength is not { } length || length <= TimeSpan.Zero || Toggles || Href is not null)
        {
            return;
        }

        (_coolingSince, _coolingFor) = (Time.GetTimestamp(), length);
        Services.GetRequiredService<IAnnouncer>().Polite(CooldownWords(CooldownLeft!.Value));
        _cooldownTimer?.Dispose();
        _cooldownTimer = Time.CreateTimer(_ => _ = InvokeAsync(StateHasChanged), null, TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));
        _cooledTimer?.Dispose();
        _cooledTimer = Time.CreateTimer(_ => _ = InvokeAsync(() =>
        {
            _cooldownTimer?.Dispose();
            _coolingSince = null;
            StateHasChanged();
        }), null, length, Timeout.InfiniteTimeSpan);
    }
}
