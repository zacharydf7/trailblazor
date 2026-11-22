using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser resize panes, ask to collapse one and report their order, so the public splitter needn't expose
// [JSInvokable] methods. Panes are named by their ids, so a report can't land on the wrong pane.
internal sealed class SplitterReport(Func<Exception, Task> onException, Func<string[], double[], int, Task> onResize,
    Func<string, Task> onToggle, Action<string[]> onOrder, Func<string, bool> holds)
    : ScriptListener(onException)
{
    // Whether the splitter holds the pane with this id, by which the bUnit driver finds a splitter's reports.
    public bool Holds(string pane) => holds(pane);

    [JSInvokable]
    public Task OnResize(string[] panes, double[] shares, int report) => Run(() => onResize(panes, shares, report));

    [JSInvokable]
    public Task OnToggle(string pane) => Run(() => onToggle(pane));

    [JSInvokable]
    public Task OnOrder(string[] panes) => Run(() => onOrder(panes));
}
