using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the toaster's script say when every toast should pause, so the host needn't expose [JSInvokable] methods.
internal sealed class ToasterEvents(Func<Exception, Task> onException, Func<bool, Task> onPause)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnPause(bool paused) => Run(() => onPause(paused));
}
