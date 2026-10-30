using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

/// <summary>What <see cref="TbButton"/> and <see cref="TbIconButton"/> share.</summary>
public abstract partial class TbButtonBase : TbComponentBase
{
    private ElementReference _element;

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

    private string TypeAttribute => Type.ToString().ToLowerInvariant();

    private bool Toggles => PressedChanged.HasDelegate;

    // Still focusable, but nothing happens when it is pressed.
    private bool Inert => Loading || (Disabled && FocusableWhenDisabled);

    private string RootClass(string? extra) => ClassNames.Join("tb-button",
        ClassNames.Modifier("tb-button", Variant), ClassNames.Modifier("tb-button", Tone),
        ClassNames.Modifier("tb-button", Size), Loading ? "tb-button--loading" : null, extra, Class);

    private object? Theirs(string attribute) => AdditionalAttributes?.GetValueOrDefault(attribute);

    /// <summary>Moves focus to the button.</summary>
    /// <param name="preventScroll">Whether to keep the page from scrolling the button into view.</param>
    public ValueTask FocusAsync(bool preventScroll = false) => _element.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Href is not null && Toggles)
        {
            throw new InvalidOperationException("A button with an Href is a link, which can't be a toggle.");
        }
    }

    private async Task OnClickAsync(MouseEventArgs args)
    {
        if (Disabled || Loading)
        {
            return;
        }

        if (Toggles)
        {
            Pressed = !Pressed;
            await PressedChanged.InvokeAsync(Pressed);
        }

        await OnClick.InvokeAsync(args);
    }
}
