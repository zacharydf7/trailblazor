using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report the tabs' markup order and the tab the arrow keys select, so the public TbTabs needn't
// expose a [JSInvokable] method.
internal sealed class TabOrder(Func<string[], Task> onReorder, Func<string, Task> onSelect)
{
    [JSInvokable]
    public Task OnReorder(string[] tabIds) => onReorder(tabIds);

    [JSInvokable]
    public Task OnSelect(string tabId) => onSelect(tabId);
}
