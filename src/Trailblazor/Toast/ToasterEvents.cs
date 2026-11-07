using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the toaster's script say when every toast should pause, so the host needn't expose [JSInvokable] methods.
internal sealed class ToasterEvents(Func<bool, Task> onPause)
{
    [JSInvokable]
    public Task OnPause(bool paused) => onPause(paused);
}
