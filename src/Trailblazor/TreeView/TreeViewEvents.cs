using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser move focus, press the keys that change the tree, report the tree items' order, drag rows and
// scroll a virtualized tree, so the public tree needn't expose [JSInvokable] methods.
internal sealed class TreeViewEvents(Func<string, Task> onFocus, Func<string, string, bool, bool, Task> onKey,
    Func<string, string, Task> onExtend, Func<string[], Task> onItemOrder,
    Func<string[], string, TreeDropPosition, Task<MoveCheck>> onDragOver,
    Func<string[], string, TreeDropPosition, Task> onDrop, Func<int, int, Task> onView,
    Func<string, string, Task> onFind)
{
    [JSInvokable]
    public Task OnFocus(string key) => onFocus(key);

    [JSInvokable]
    public Task OnKey(string key, string pressed, bool shift, bool ctrl) => onKey(key, pressed, shift, ctrl);

    [JSInvokable]
    public Task OnExtend(string key, string from) => onExtend(key, from);

    [JSInvokable]
    public Task OnItemOrder(string[] ids) => onItemOrder(ids);

    [JSInvokable]
    public Task<MoveCheck> OnDragOver(string[] ids, string target, TreeDropPosition position) =>
        onDragOver(ids, target, position);

    [JSInvokable]
    public Task OnDrop(string[] ids, string target, TreeDropPosition position) => onDrop(ids, target, position);

    [JSInvokable]
    public Task OnView(int start, int count) => onView(start, count);

    [JSInvokable]
    public Task OnFind(string key, string typed) => onFind(key, typed);
}
