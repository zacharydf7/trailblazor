using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser resize panes, ask to collapse one and report their order, so the public splitter needn't expose
// [JSInvokable] methods. Panes are named by their ids, so a report can't land on the wrong pane.
internal sealed class SplitterReport(Func<string[], double[], int, Task> onResize, Func<string, Task> onToggle,
    Action<string[]> onOrder)
{
    [JSInvokable]
    public Task OnResize(string[] panes, double[] shares, int report) => onResize(panes, shares, report);

    [JSInvokable]
    public Task OnToggle(string pane) => onToggle(pane);

    [JSInvokable]
    public void OnOrder(string[] panes) => onOrder(panes);
}
