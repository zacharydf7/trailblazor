using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser open and close a select's list, so the public select needn't expose a [JSInvokable] method.
internal sealed class SelectToggle(Func<bool, Task> onToggle)
{
    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);
}
