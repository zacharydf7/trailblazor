using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report the tabs' markup order, so the public TbTabs needn't expose a [JSInvokable] method.
internal sealed class TabOrder(Func<string[], Task> onReorder)
{
    [JSInvokable]
    public Task OnReorder(string[] tabIds) => onReorder(tabIds);
}
