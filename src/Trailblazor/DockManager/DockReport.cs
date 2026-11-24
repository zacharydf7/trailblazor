using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser select a tab with the arrow keys, close one with a key or a middle click, move a separator,
// drop a dragged tab or strip button or pull a tab out into a window, double-click a tab, press or focus a window
// behind another, move or resize a window, resize or leave a slid-out panel, close a panel's browser window, and
// find the dock narrower than its compact width or wide enough again, so the public dock needn't expose [JSInvokable]
// methods.
internal sealed class DockReport(Func<Exception, Task> onException, Func<string, Task> onSelect,
    Func<string, bool, Task> onClose, Func<int, double, int, Task> onResize, Func<string, string?, string, Task> onMove,
    Func<string, Task> onMaximize, Func<string, Task> onRaise, Func<string, DockBox, bool, int, Task> onFloat,
    Func<string, DockBox, Task> onPullOut, Func<bool, Task> onConceal, Func<string, double, int, Task> onSlide,
    Func<string, string, Task> onAutoHide, Func<bool, string?, bool, Task> onNarrow, Func<string, Task> onReturn,
    Func<string, Task> onKeptWindowClosed, Func<string, bool> holds)
    : ScriptListener(onException)
{
    // Whether the dock holds the tab with this element id, by which the bUnit driver finds a dock's reports. Panel
    // ids won't do, since two docks can use the same ones.
    public bool Holds(string tab) => holds(tab);

    [JSInvokable]
    public Task OnSelect(string panel) => Run(() => onSelect(panel));

    // Focused says whether the tab or its panel held the focus, which a middle click leaves where it was.
    [JSInvokable]
    public Task OnClose(string panel, bool focused) => Run(() => onClose(panel, focused));

    [JSInvokable]
    public Task OnResize(int separator, double fraction, int report) =>
        Run(() => onResize(separator, fraction, report));

    // The position is a DockPosition by name. With no target, the panel goes along that edge of the dock.
    [JSInvokable]
    public Task OnMove(string panel, string? target, string position) => Run(() => onMove(panel, target, position));

    // The panel's group fills the dock, or every group shows again when it filled it already.
    [JSInvokable]
    public Task OnMaximize(string panel) => Run(() => onMaximize(panel));

    // The window that holds the panel comes to the front.
    [JSInvokable]
    public Task OnRaise(string panel) => Run(() => onRaise(panel));

    // The window that holds the panel has this box now, in percent of the dock, after a move or a resize. The number
    // counts the script's reports, as for a separator.
    [JSInvokable]
    public Task OnFloat(string panel, double x, double y, double width, double height, bool resized, int report) =>
        Run(() => onFloat(panel, new DockBox(x, y, width, height), resized, report));

    // A dragged tab was pulled out of its group: its panel floats in a window with this box, in percent of the dock.
    [JSInvokable]
    public Task OnPullOut(string panel, double x, double y, double width, double height) =>
        Run(() => onPullOut(panel, new DockBox(x, y, width, height)));

    // A dragged tab or strip button was dropped on the strip along this edge, a DockPosition name in any letter case.
    [JSInvokable]
    public Task OnAutoHide(string panel, string edge) => Run(() => onAutoHide(panel, edge));

    // The slid-out panel slides back: Escape was pressed in it, or a press or the focus went elsewhere.
    [JSInvokable]
    public Task OnConceal(bool escaped) => Run(() => onConceal(escaped));

    // The slid-out panel takes this much of the dock now, in percent, after its inner edge moved. The number counts
    // the script's reports, as for a separator.
    [JSInvokable]
    public Task OnSlide(string panel, double size, int report) => Run(() => onSlide(panel, size, report));

    // The dock is narrower than its compact width now, or wide enough again. Focused is the panel whose tab holds the
    // focus, when onTab is true, or that holds it inside, and null when the focus is elsewhere.
    [JSInvokable]
    public Task OnNarrow(bool narrow, string? focused, bool onTab) => Run(() => onNarrow(narrow, focused, onTab));

    // The browser window that showed the panel closed, and the panel is back in the dock.
    [JSInvokable]
    public Task OnReturn(string panel) => Run(() => onReturn(panel));

    // The browser window kept open for a panel the app took away has closed while the panel was still away.
    [JSInvokable]
    public Task OnKeptWindowClosed(string panel) => Run(() => onKeptWindowClosed(panel));
}
