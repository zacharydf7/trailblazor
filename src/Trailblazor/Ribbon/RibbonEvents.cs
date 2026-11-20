using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser ask to collapse the ribbon or show it again, so the public TbRibbon needn't expose a
// [JSInvokable] method.
internal sealed class RibbonEvents(Func<Task> onToggle)
{
    [JSInvokable]
    public Task OnToggle() => onToggle();
}
