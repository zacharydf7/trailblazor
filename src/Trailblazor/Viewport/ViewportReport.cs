using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser zoom a viewport, so the public viewport needn't expose a [JSInvokable] method.
internal sealed class ViewportReport(Func<Exception, Task> onException, Func<double, string, string, Task> onZoom)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnZoom(double zoom, string fit, string input) => Run(() => onZoom(zoom, fit, input));
}
