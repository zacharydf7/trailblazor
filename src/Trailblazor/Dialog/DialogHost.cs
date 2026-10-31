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
        foreach (var dialog in Dialogs.OpenDialogs.Where(dialog => dialog.Component is not null))
        {
            builder.OpenComponent<DialogFrame>(0);
            builder.SetKey(dialog);
            builder.AddComponentParameter(1, nameof(DialogFrame.Dialog), dialog);
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
