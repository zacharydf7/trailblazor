using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the chart's script say that a point was activated, so the public chart needn't expose a [JSInvokable] method.
internal sealed class ChartEvents(
    Func<Exception, Task> onException, Func<int, int, string, Task> onActivate, string plotId)
    : ScriptListener(onException)
{
    // The id of the chart's plot, which the testing helper finds this chart's events by.
    internal string PlotId => plotId;

    // The series by its number among all the chart's series, the place along the chart, and "keyboard" or "pointer".
    [JSInvokable]
    public Task OnActivate(int series, int place, string input) => Run(() => onActivate(series, place, input));
}
