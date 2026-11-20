using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser select a tab with the arrow keys, close one with a key or a middle click, move a separator,
// drop a dragged tab or strip button or pull a tab out into a window, double-click a tab, press or focus a window
// behind another, move or resize a window, resize or leave a slid-out panel, and close a panel's browser window, so
// the public dock needn't expose [JSInvokable] methods.
internal sealed class DockReport(Func<string, Task> onSelect, Func<string, bool, Task> onClose,
    Func<int, double, int, Task> onResize, Func<string, string?, string, Task> onMove, Func<string, Task> onMaximize,
    Func<string, Task> onRaise, Func<string, DockBox, bool, int, Task> onFloat, Func<string, DockBox, Task> onPullOut,
    Func<bool, Task> onConceal, Func<string, double, int, Task> onSlide, Func<string, string, Task> onAutoHide,
    Func<string, Task> onReturn, Func<string, bool> holds)
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

    // The window that holds the panel has this box now, in percent of the dock, after a move or a resize. The number
    // counts the script's reports, as for a separator.
    [JSInvokable]
    public Task OnFloat(string panel, double x, double y, double width, double height, bool resized, int report) =>
        onFloat(panel, new DockBox(x, y, width, height), resized, report);

    // A dragged tab was pulled out of its group: its panel floats in a window with this box, in percent of the dock.
    [JSInvokable]
    public Task OnPullOut(string panel, double x, double y, double width, double height) =>
        onPullOut(panel, new DockBox(x, y, width, height));

    // A dragged tab or strip button was dropped on the strip along this edge, a DockPosition name in any letter case.
    [JSInvokable]
    public Task OnAutoHide(string panel, string edge) => onAutoHide(panel, edge);

    // The slid-out panel slides back: Escape was pressed in it, or a press or the focus went elsewhere.
    [JSInvokable]
    public Task OnConceal(bool escaped) => onConceal(escaped);

    // The slid-out panel takes this much of the dock now, in percent, after its inner edge moved. The number counts
    // the script's reports, as for a separator.
    [JSInvokable]
    public Task OnSlide(string panel, double size, int report) => onSlide(panel, size, report);

    // The browser window that showed the panel closed, and the panel is back in the dock.
    [JSInvokable]
    public Task OnReturn(string panel) => onReturn(panel);
}
