using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// Renders the dialogs opened through the service; TbRoot puts one in the layout. C#, so it can stay internal.
internal sealed class DialogHost : ComponentBase, IDisposable
{
    [Inject]
    private DialogService Dialogs { get; set; } = default!;

    protected override void OnInitialized()
    {
        Dialogs.Hosts.Attach();
        Dialogs.Changed += OnChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "hidden");
        builder.AddAttribute(2, "data-tb-open-dialogs", Dialogs.OpenDialogs.Count(dialog => !dialog.IsClosing));
        builder.CloseElement();
        foreach (var dialog in Dialogs.OpenDialogs.Where(dialog => dialog.ContentType is not null))
        {
            builder.OpenComponent<DialogFrame>(3);
            builder.SetKey(dialog);
            builder.AddComponentParameter(4, nameof(DialogFrame.Dialog), dialog);
            builder.CloseComponent();
        }
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Dialogs.Hosts.Detach();
        Dialogs.Changed -= OnChanged;
    }
}
