using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the chart's script say that a point was activated, so the public chart needn't expose a [JSInvokable] method.
// A pie has no zoom, so it gives no zoom or move handler.
internal sealed class ChartEvents(
    Func<Exception, Task> onException, Func<int, int, string, Task> onActivate, string plotId,
    Func<string, double?, Task>? onZoom = null, Func<double, Task>? onMove = null)
    : ScriptListener(onException)
{
    // The id of the chart's plot, which the testing helper finds this chart's events by.
    internal string PlotId => plotId;

    // The series by its number among all the chart's series, the place along the chart, and "keyboard" or "pointer".
    [JSInvokable]
    public Task OnActivate(int series, int place, string input) => Run(() => onActivate(series, place, input));

    // "in", "out", "back", "forward" or "all", and for a zoom with the wheel, the share of the plot under the pointer.
    [JSInvokable]
    public Task OnZoom(string action, double? at) => Run(() => onZoom?.Invoke(action, at) ?? Task.CompletedTask);

    // How far the view moves, as a share of what it shows: below zero back along the scale, above zero forward.
    [JSInvokable]
    public Task OnMove(double share) => Run(() => onMove?.Invoke(share) ?? Task.CompletedTask);
}
