using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor;

/// <summary>A small confirmation that opens beside the button that asked, such as "Delete this trail?" with Cancel and
/// Delete, for an action that's quick to ask about but worth a second thought.</summary>
public partial class TbPopconfirm
{
    private bool _open;
    private bool _lastOpen;
    private bool _confirming;
    private bool _givenVariant;
    private bool _givenSize;
    private ControlTelemetry? _telemetry;
    private readonly PopoverDescription _description = new($"tb-popconfirm-{Guid.NewGuid():N}-description");

    /// <summary>The text on the trigger button. Required unless an <see cref="Icon"/> or a <see cref="Trigger"/>
    /// gives the trigger.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The icon trigger's accessible name, which screen readers read and which is never shown. Required
    /// with an <see cref="Icon"/>.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Shows an icon button as the trigger instead of a text button.</summary>
    [Parameter]
    public Icon? Icon { get; set; }

    /// <summary>A trigger of your own, such as a row's delete button. Put the context's attributes on the button:
    /// <c>@attributes="context.Attributes"</c>.</summary>
    [Parameter]
    public RenderFragment<PopoverTrigger>? Trigger { get; set; }

    /// <summary>How the built-in trigger is drawn. Defaults to <see cref="Trailblazor.Variant.Outlined"/>.</summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>The built-in trigger's size. Defaults to <see cref="Trailblazor.Size.Medium"/>.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>The question, such as "Delete Pine Hollow loop?". It names the confirmation for screen readers.
    /// Required.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>A sentence under the title, such as what can't be undone.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Content shown under the title instead of <see cref="Description"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The confirm button's text, such as "Delete". Defaults to the app's text for
    /// <see cref="TbStrings.PopconfirmConfirm"/>.</summary>
    [Parameter]
    public string? ConfirmText { get; set; }

    /// <summary>The cancel button's text. Defaults to the app's text for <see cref="TbStrings.PopconfirmCancel"/>.
    /// </summary>
    [Parameter]
    public string? CancelText { get; set; }

    /// <summary>The confirm button's tone, such as <see cref="Trailblazor.Tone.Danger"/> for a delete. Defaults to
    /// <see cref="Trailblazor.Tone.Primary"/>.</summary>
    [Parameter]
    public Tone Tone { get; set; } = Tone.Primary;

    /// <summary>The side of the trigger the confirmation opens on. Defaults to <see cref="Placement.Bottom"/>.
    /// </summary>
    [Parameter]
    public Placement Placement { get; set; } = Placement.Bottom;

    /// <summary>How the confirmation lines up with the trigger. Defaults to <see cref="PlacementAlign.Start"/>.
    /// </summary>
    [Parameter]
    public PlacementAlign Align { get; set; } = PlacementAlign.Start;

    /// <summary>Turns the trigger off, so the confirmation can't open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the confirmation is open. Bind it with <c>@bind-Open</c> to open it from code.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the confirmation opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Runs when the user confirms. The confirm button shows it's busy until this finishes, and then the
    /// confirmation closes.</summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>Runs when the user cancels: with the cancel button, Escape, or by clicking or tabbing away.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>A label this confirmation's use is counted under, such as <c>"delete-trail"</c>, so the app can see
    /// how often people confirm and cancel through the <c>Trailblazor</c> meter. It names what the confirmation asks,
    /// never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    // The description, when there is one, describes the panel for screen readers.
    private PopoverDescription? Described => ChildContent is null && Description is null ? null : _description;

    [CascadingParameter]
    private TbButtonGroup? Group { get; set; }

    // A trigger in a button group takes the group's look unless the popconfirm sets its own.
    private Variant TriggerVariant => TbButtonGroup.Choose(_givenVariant, Variant, Group?.Variant);

    private Size TriggerSize => TbButtonGroup.Choose(_givenSize, Size, Group?.Size);

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _givenVariant = parameters.TryGetValue<Variant>(nameof(Variant), out _);
        _givenSize = parameters.TryGetValue<Size>(nameof(Size), out _);
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException($"{nameof(TbPopconfirm)} needs a {nameof(Title)}.");
        }

        if (Trigger is null && Icon is not null && string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"{nameof(TbPopconfirm)} with an {nameof(Icon)} needs an "
                + $"{nameof(AriaLabel)}, which names the icon button; {nameof(Label)} is for a text trigger.");
        }

        // A new Open from the parent opens or closes the confirmation; one it already reported changes nothing.
        if (Open != _lastOpen)
        {
            _open = _lastOpen = Open;
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    // Leaves _lastOpen at the parent's own value, so an unbound parent's re-render doesn't close what the user opened.
    private async Task SetOpenAsync(bool open)
    {
        _open = open;
        await OpenChanged.InvokeAsync(open);
    }

    // The popover reports the user opening it, and closing it with Escape, a click outside or Tab; closing is a cancel.
    private async Task OnOpenChangedAsync(bool open)
    {
        var dismissed = _open && !open && !_confirming;
        await SetOpenAsync(open);
        if (dismissed)
        {
            Telemetry.Used(TelemetryName, "cancel");
            await OnCancel.InvokeAsync();
        }
    }

    private async Task CancelAsync()
    {
        Telemetry.Used(TelemetryName, "cancel");
        await SetOpenAsync(false);
        await OnCancel.InvokeAsync();
    }

    private async Task ConfirmAsync()
    {
        Telemetry.Used(TelemetryName, "confirm");
        _confirming = true;
        try
        {
            await OnConfirm.InvokeAsync();
        }
        finally
        {
            _confirming = false;
        }

        await SetOpenAsync(false);
    }
}
