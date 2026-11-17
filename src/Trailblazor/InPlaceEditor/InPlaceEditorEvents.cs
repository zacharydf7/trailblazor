using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the script's save on Enter and cancel on Escape, so the public component needn't expose [JSInvokable] methods.
internal sealed class InPlaceEditorEvents(Func<Task> onSave, Func<Task> onCancel)
{
    [JSInvokable]
    public Task OnSave() => onSave();

    [JSInvokable]
    public Task OnCancel() => onCancel();
}
