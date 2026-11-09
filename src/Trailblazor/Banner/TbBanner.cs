using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

/// <summary>A strip across the top of the page or app with a notice for everyone using it, such as planned
/// maintenance or a new feature. People can dismiss it when <see cref="OnClose"/> is set.</summary>
public partial class TbBanner
{
    private readonly string _titleId = $"tb-banner-{Guid.NewGuid():N}-title";

    /// <summary>The notice's meaning, which sets its color and icon. Defaults to <see cref="Trailblazor.Tone.Info"/>.
    /// </summary>
    [Parameter]
    public Tone Tone { get; set; } = Tone.Info;

    /// <summary>A short lead in bold before the message, such as "Maintenance tonight". It also names the banner for
    /// screen readers.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The message.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>An icon in place of the tone's own.</summary>
    [Parameter]
    public Icon? Icon { get; set; }

    /// <summary>Shows the icon. On by default.</summary>
    [Parameter]
    public bool ShowIcon { get; set; } = true;

    /// <summary>Buttons or links after the message, such as "Learn more".</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Shows a close button that raises this; hide the banner when it does.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClose { get; set; }

    /// <summary>The close button's name. Defaults to the app's text for <see cref="TbStrings.BannerClose"/>.
    /// </summary>
    [Parameter]
    public string? CloseLabel { get; set; }

    /// <summary>Names the banner for screen readers when it has no <see cref="Title"/>. Defaults to the app's text for
    /// <see cref="TbStrings.BannerLabel"/>.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }
}
