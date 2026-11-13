using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser zoom a viewport, so the public viewport needn't expose a [JSInvokable] method.
internal sealed class ViewportReport(Func<double, string, string, Task> onZoom)
{
    [JSInvokable]
    public Task OnZoom(double zoom, string fit, string input) => onZoom(zoom, fit, input);
}
