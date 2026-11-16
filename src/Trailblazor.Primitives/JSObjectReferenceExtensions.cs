using Microsoft.JSInterop;

namespace Trailblazor.Primitives;

internal static class JSObjectReferenceExtensions
{
    // Calls the JS object's own dispose(), then releases the .NET reference to it.
    public static async ValueTask DisposeHandleAsync(this IJSObjectReference handle, params object?[] args)
    {
        try
        {
            await handle.InvokeVoidAsync("dispose", args);
            await handle.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the browser state went with it.
        }
    }
}
