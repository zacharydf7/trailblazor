using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report the tabs' markup order, the tab the arrow keys select and the tab a key closes, so the
// public TbTabs needn't expose a [JSInvokable] method.
internal sealed class TabOrder(Func<string[], Task> onReorder, Func<string, Task> onSelect, Func<string, Task> onClose)
{
    [JSInvokable]
    public Task OnReorder(string[] tabIds) => onReorder(tabIds);

    [JSInvokable]
    public Task OnSelect(string tabId) => onSelect(tabId);

    [JSInvokable]
    public Task OnClose(string tabId) => onClose(tabId);
}
