using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser move focus, press the keys that change the tree, report the tree items' order and drag rows, so
// the public tree needn't expose [JSInvokable] methods.
internal sealed class TreeViewEvents(Func<string, Task> onFocus, Func<string, string, bool, bool, Task> onKey,
    Func<string[], Task> onItemOrder, Func<string[], string, TreeDropPosition, Task<TreeDropCheck>> onDragOver,
    Func<string[], string, TreeDropPosition, Task> onDrop)
{
    [JSInvokable]
    public Task OnFocus(string key) => onFocus(key);

    [JSInvokable]
    public Task OnKey(string key, string pressed, bool shift, bool ctrl) => onKey(key, pressed, shift, ctrl);

    [JSInvokable]
    public Task OnItemOrder(string[] ids) => onItemOrder(ids);

    [JSInvokable]
    public Task<TreeDropCheck> OnDragOver(string[] ids, string target, TreeDropPosition position) =>
        onDragOver(ids, target, position);

    [JSInvokable]
    public Task OnDrop(string[] ids, string target, TreeDropPosition position) => onDrop(ids, target, position);
}
