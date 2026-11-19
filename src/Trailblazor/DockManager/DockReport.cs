using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser select a tab with the arrow keys, close one with a key or a middle click, move a separator,
// drop a dragged tab, double-click a tab, and press or focus a window behind another, so the public dock needn't
// expose [JSInvokable] methods.
internal sealed class DockReport(Func<string, Task> onSelect, Func<string, bool, Task> onClose,
    Func<int, double, int, Task> onResize, Func<string, string?, string, Task> onMove, Func<string, Task> onMaximize,
    Func<string, Task> onRaise, Func<string, bool> holds)
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

    // The position is a DockPosition by name. With no target, the panel goes along that edge of the dock.
    [JSInvokable]
    public Task OnMove(string panel, string? target, string position) => onMove(panel, target, position);

    // The panel's group fills the dock, or every group shows again when it filled it already.
    [JSInvokable]
    public Task OnMaximize(string panel) => onMaximize(panel);

    // The window that holds the panel comes to the front.
    [JSInvokable]
    public Task OnRaise(string panel) => onRaise(panel);
}
