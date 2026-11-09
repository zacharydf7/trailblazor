using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Looks once, as the page becomes interactive, for another stylesheet whose unlayered rules override Trailblazor's,
// and tells the developer in the browser's console. TbRoot renders it. C#, so it can stay internal.
internal sealed class StylesheetCheck : ComponentBase
{
    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    [Inject]
    private IOptions<TrailblazorOptions> Options { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !RendererInfo.IsInteractive || !Options.Value.WarnAboutUnlayeredStyles)
        {
            return;
        }

        try
        {
            var module = await Modules.ImportAsync("./_content/Trailblazor/stylesheet-check.js");
            await module.InvokeVoidAsync("checkStylesheets");
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the page with it.
        }
    }
}
