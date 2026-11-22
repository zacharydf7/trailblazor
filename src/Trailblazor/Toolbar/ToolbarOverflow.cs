using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears which toolbar buttons the browser moved into the overflow menu, so the public toolbar needn't expose a
// [JSInvokable] method.
internal sealed class ToolbarOverflow(Func<Exception, Task> onException, Action<IReadOnlyList<string>> onOverflow)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnOverflow(string[] ids) => Run(() => onOverflow(ids));
}
