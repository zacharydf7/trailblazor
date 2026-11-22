using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser open and close the lists, and the keys open a choice's list, so the public field needn't expose
// [JSInvokable] methods.
internal sealed class CascaderEvents(Func<Exception, Task> onException, Func<bool, Task> onToggle,
    Func<int, int, Task> onOpen)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnToggle(bool open) => Run(() => onToggle(open));

    [JSInvokable]
    public Task OnOpen(int level, int index) => Run(() => onOpen(level, index));
}
