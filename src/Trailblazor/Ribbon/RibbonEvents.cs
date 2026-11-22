using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser ask to collapse the ribbon or show it again, so the public TbRibbon needn't expose a
// [JSInvokable] method.
internal sealed class RibbonEvents(Func<Exception, Task> onException, Func<Task> onToggle)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnToggle() => Run(() => onToggle());
}
