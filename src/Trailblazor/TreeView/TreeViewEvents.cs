using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser move focus, press the keys that change the tree and report the tree items' order, so the
// public tree needn't expose [JSInvokable] methods.
internal sealed class TreeViewEvents(Func<string, Task> onFocus, Func<string, string, bool, bool, Task> onKey,
    Func<string[], Task> onItemOrder)
{
    [JSInvokable]
    public Task OnFocus(string key) => onFocus(key);

    [JSInvokable]
    public Task OnKey(string key, string pressed, bool shift, bool ctrl) => onKey(key, pressed, shift, ctrl);

    [JSInvokable]
    public Task OnItemOrder(string[] ids) => onItemOrder(ids);
}
