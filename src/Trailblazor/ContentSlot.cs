using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// Content a component hands to the component around it to show elsewhere, such as in a dialog's header or at a tree
// row's end. One swapped in sets its content before the one it replaces is disposed, so only the component that set
// the content last clears it.
internal sealed class ContentSlot
{
    private object? _owner;

    public RenderFragment? Content { get; private set; }

    public event Action? Changed;

    public void Set(object owner, RenderFragment? content)
    {
        _owner = owner;
        Content = content;
        Changed?.Invoke();
    }

    // False when another component has set the content since.
    public bool Clear(object owner)
    {
        if (_owner != owner)
        {
            return false;
        }

        _owner = null;
        Content = null;
        Changed?.Invoke();
        return true;
    }
}
