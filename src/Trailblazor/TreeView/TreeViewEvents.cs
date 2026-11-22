using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser move focus, press the keys that change the tree, report the tree items' order, drag rows over
// others and scroll a virtualized tree, so the public tree needn't expose [JSInvokable] methods.
internal sealed class TreeViewEvents(Func<Exception, Task> onException, Func<string, Task> onFocus,
    Func<string, string, bool, bool, Task> onKey, Func<string, string, Task> onExtend, Func<string[], Task> onItemOrder,
    Func<string[], string, TreeMovePosition, Task<MoveCheck>> onDragOver, Func<int, int, Task> onView,
    Func<string, string, Task> onFind)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnFocus(string key) => Run(() => onFocus(key));

    [JSInvokable]
    public Task OnKey(string key, string pressed, bool shift, bool ctrl) => Run(() => onKey(key, pressed, shift, ctrl));

    [JSInvokable]
    public Task OnExtend(string key, string from) => Run(() => onExtend(key, from));

    [JSInvokable]
    public Task OnItemOrder(string[] ids) => Run(() => onItemOrder(ids));

    [JSInvokable]
    public Task<MoveCheck> OnDragOver(string[] ids, string target, TreeMovePosition position) =>
        Run(() => onDragOver(ids, target, position), new MoveCheck(false));

    [JSInvokable]
    public Task OnView(int start, int count) => Run(() => onView(start, count));

    [JSInvokable]
    public Task OnFind(string key, string typed) => Run(() => onFind(key, typed));
}
