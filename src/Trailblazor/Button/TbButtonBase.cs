using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

/// <summary>What <see cref="TbButton"/> and <see cref="TbIconButton"/> share.</summary>
public abstract class TbButtonBase : TbComponentBase
{
    /// <summary>How strongly the button is drawn.</summary>
    [Parameter]
    public Variant Variant { get; set; }

    /// <summary>The button's meaning, which sets its color.</summary>
    [Parameter]
    public Tone Tone { get; set; }

    /// <summary>The button's size. Defaults to <see cref="Trailblazor.Size.Medium"/>.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>Turns the button off: it can't be focused or clicked.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows a busy spinner and ignores clicks, but keeps the button focusable so focus isn't lost while
    /// the work runs.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>What the button does inside a form. Defaults to <see cref="ButtonType.Button"/>.</summary>
    [Parameter]
    public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>Raised when the button is clicked, unless it is disabled or loading.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private protected string TypeAttribute => Type.ToString().ToLowerInvariant();

    private protected string RootClass(string? extra = null) => ClassNames.Join("tb-button",
        ClassNames.Modifier("tb-button", Variant), ClassNames.Modifier("tb-button", Tone),
        ClassNames.Modifier("tb-button", Size), Loading ? "tb-button--loading" : null, extra, Class);

    private protected Task OnClickAsync(MouseEventArgs args) =>
        Disabled || Loading ? Task.CompletedTask : OnClick.InvokeAsync(args);
}
