using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trailblazor;

/// <summary>Shows richer content, such as an icon or a status beside the text, in place of the dialog's title in its
/// header. Put it anywhere in the dialog's content. The content becomes the dialog's accessible name, so keep the
/// title's text in it and put buttons in <see cref="TbDialogHeaderActions"/> instead. Without the header, the title's
/// text still names the dialog.</summary>
public sealed class TbDialogTitle : ComponentBase, IDisposable
{
    [CascadingParameter]
    private DialogReference? Dialog { get; set; }

    /// <summary>What the header shows as the title.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private bool _warned;

    private DialogReference Reference => Dialog
        ?? throw new InvalidOperationException($"Put {nameof(TbDialogTitle)} inside a dialog.");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Reference.SetTitleContent(ChildContent);
        if (!Reference.ShowHeader && !_warned)
        {
            _warned = true;
            Services.GetService<ILogger<TbDialogTitle>>()?.LogWarning("The dialog {Title} has a TbDialogTitle, but "
                + "ShowHeader is off, so the title content doesn't show. Show the header, or put the content in the "
                + "dialog's body.", Reference.Title);
        }
    }

    /// <inheritdoc />
    public void Dispose() => Dialog?.SetTitleContent(null);
}
