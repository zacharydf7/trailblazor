using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the pointer move over the saturation and brightness pad, so the public picker needn't expose [JSInvokable]
// methods.
internal sealed class ColorPickerEvents(Func<Exception, Task> onException, Func<double, double, Task> onPad)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnPad(double saturation, double brightness) => Run(() => onPad(saturation, brightness));
}
