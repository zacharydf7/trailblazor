using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the button first show, so the public component needn't expose [JSInvokable] methods.
internal sealed class BackToTopEvents(Func<Exception, Task> onException, Action onShown)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnShown() => Run(() => onShown());
}
