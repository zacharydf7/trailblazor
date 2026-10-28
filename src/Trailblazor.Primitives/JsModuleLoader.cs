using Microsoft.JSInterop;

namespace Trailblazor.Primitives;

// Modules live as long as the circuit or app, so they are never disposed one by one.
internal sealed class JsModuleLoader(IJSRuntime jsRuntime)
{
    private readonly Dictionary<string, Task<IJSObjectReference>> _modules = [];

    public async Task<IJSObjectReference> ImportAsync(string path)
    {
        if (!_modules.TryGetValue(path, out var module))
        {
            module = jsRuntime.InvokeAsync<IJSObjectReference>("import", path).AsTask();
            _modules[path] = module;
        }

        try
        {
            return await module;
        }
        catch
        {
            // Forget the failed import so the next caller retries it, unless a retry has already replaced it.
            if (_modules.TryGetValue(path, out var current) && current == module)
            {
                _modules.Remove(path);
            }

            throw;
        }
    }
}
