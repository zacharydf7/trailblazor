using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Trailblazor;

// Renders the dialogs opened through the service; TbRoot puts one in the layout. C#, so it can stay internal.
internal sealed class DialogHost : ComponentBase, IDisposable
{
    [Inject]
    private DialogService Dialogs { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Dialogs.Hosts.Attach();
        Dialogs.Changed += OnChanged;
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "hidden");
        builder.AddAttribute(2, "data-tb-open-dialogs", Dialogs.OpenDialogs.Count(dialog => !dialog.IsClosing));
        builder.AddAttribute(3, "data-tb-answered-dialogs", RendererInfo.IsInteractive ? Dialogs.Answered : null);
        builder.CloseElement();
        foreach (var dialog in Dialogs.OpenDialogs.Where(dialog => dialog.ContentType is not null))
        {
            builder.OpenComponent<DialogFrame>(4);
            builder.SetKey(dialog);
            builder.AddComponentParameter(5, nameof(DialogFrame.Dialog), dialog);
            builder.CloseComponent();
        }
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => InvokeAsync(async () =>
    {
        try
        {
            await Dialogs.CloseForNavigationAsync(e.Location);
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
        }
    });

    public void Dispose()
    {
        Dialogs.Hosts.Detach();
        Dialogs.Changed -= OnChanged;
        Navigation.LocationChanged -= OnLocationChanged;
    }
}
