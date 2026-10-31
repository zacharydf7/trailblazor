using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

/// <summary>What <see cref="TbButton"/> and <see cref="TbIconButton"/> share.</summary>
public abstract partial class TbButtonBase : TbComponentBase
{
    private ElementReference _element;
    private ControlTelemetry? _telemetry;
    private bool _givenVariant;
    private bool _givenTone;
    private bool _givenSize;

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

    /// <summary>Keeps a disabled button in the tab order, marked <c>aria-disabled</c>, so a tooltip or a keyboard
    /// user can still find it and learn why it is off.</summary>
    [Parameter]
    public bool FocusableWhenDisabled { get; set; }

    /// <summary>Shows a busy spinner and ignores clicks, but keeps the button focusable so focus isn't lost while
    /// the work runs.</summary>
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

    /// <summary>What the button does inside a form. Defaults to <see cref="ButtonType.Button"/>.</summary>
    [Parameter]
    public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>Raised when the button is clicked, unless it is disabled or loading.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>A label this button's use is counted under, such as <c>"export-csv"</c>, so the app can see how often
    /// people see it and use it through the <c>Trailblazor</c> meter. Controls that share a label count together; a
    /// control without one isn't counted. It names what the button does, never the user, and is no credential.
    /// </summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [CascadingParameter]
    private TbButtonGroup? Group { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private protected Variant ShownVariant => TbButtonGroup.Choose(_givenVariant, Variant, Group?.Variant);

    private protected Tone ShownTone => TbButtonGroup.Choose(_givenTone, Tone, Group?.Tone);

    private protected Size ShownSize => TbButtonGroup.Choose(_givenSize, Size, Group?.Size);

    private bool Off => Disabled || Group?.Disabled == true;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private string TypeAttribute => Type.ToString().ToLowerInvariant();

    private bool Toggles => PressedChanged.HasDelegate;

    // Still focusable, but nothing happens when it is pressed.
    private bool Inert => Loading || (Off && FocusableWhenDisabled);

    private string RootClass(string? extra) => ClassNames.Join("tb-button",
        ClassNames.Modifier("tb-button", ShownVariant), ClassNames.Modifier("tb-button", ShownTone),
        ClassNames.Modifier("tb-button", ShownSize), Loading ? "tb-button--loading" : null, extra, Class);

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
        if (Href is not null && Toggles)
        {
            throw new InvalidOperationException("A button with an Href is a link, which can't be a toggle.");
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    private async Task OnClickAsync(MouseEventArgs args)
    {
        if (Off || Loading)
        {
            return;
        }

        Telemetry.Used(TelemetryName);

        if (Toggles)
        {
            Pressed = !Pressed;
            await PressedChanged.InvokeAsync(Pressed);
        }

        await OnClick.InvokeAsync(args);
    }
}
