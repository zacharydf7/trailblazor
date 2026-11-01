using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor;

// Tells when InputFile's init call has returned: a parent's re-render can reach the browser before the call is sent.
internal sealed class ListeningInputFile : InputFile
{
    [Parameter]
    public EventCallback OnListening { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && RendererInfo.IsInteractive)
        {
            await OnListening.InvokeAsync();
        }
    }
}
