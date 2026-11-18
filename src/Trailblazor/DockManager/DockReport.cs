using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser select a tab with the arrow keys, close one with a key or a middle click, and move a separator,
// so the public dock needn't expose [JSInvokable] methods.
internal sealed class DockReport(Func<string, Task> onSelect, Func<string, bool, Task> onClose,
    Func<int, double, int, Task> onResize, Func<string, bool> holds)
{
    // Whether the dock holds the tab with this element id, by which the bUnit driver finds a dock's reports. Panel
    // ids won't do, since two docks can use the same ones.
    public bool Holds(string tab) => holds(tab);

    [JSInvokable]
    public Task OnSelect(string panel) => onSelect(panel);

    // Focused says whether the tab or its panel held the focus, which a middle click leaves where it was.
    [JSInvokable]
    public Task OnClose(string panel, bool focused) => onClose(panel, focused);

    [JSInvokable]
    public Task OnResize(int separator, double fraction, int report) => onResize(separator, fraction, report);
}
