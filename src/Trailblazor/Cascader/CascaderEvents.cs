using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser open and close the lists, and the keys open a choice's list, so the public field needn't expose
// [JSInvokable] methods.
internal sealed class CascaderEvents(Func<bool, Task> onToggle, Func<int, int, Task> onOpen)
{
    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);

    [JSInvokable]
    public Task OnOpen(int level, int index) => onOpen(level, index);
}
