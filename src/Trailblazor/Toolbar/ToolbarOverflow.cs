using Microsoft.JSInterop;

namespace Trailblazor;

// Hears which toolbar buttons the browser moved into the overflow menu, so the public toolbar needn't expose a
// [JSInvokable] method.
internal sealed class ToolbarOverflow(Action<IReadOnlyList<string>> onOverflow)
{
    [JSInvokable]
    public void OnOverflow(string[] ids) => onOverflow(ids);
}
