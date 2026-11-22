using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser report the tabs' markup order, the tab the arrow keys select and the tab a key closes, so the
// public TbTabs needn't expose a [JSInvokable] method.
internal sealed class TabOrder(Func<Exception, Task> onException, Func<string[], Task> onReorder,
    Func<string, Task> onSelect, Func<string, Task> onClose)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnReorder(string[] tabIds) => Run(() => onReorder(tabIds));

    [JSInvokable]
    public Task OnSelect(string tabId) => Run(() => onSelect(tabId));

    [JSInvokable]
    public Task OnClose(string tabId) => Run(() => onClose(tabId));
}
