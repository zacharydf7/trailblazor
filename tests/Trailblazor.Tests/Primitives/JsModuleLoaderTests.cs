using Bunit;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Primitives;

public class JsModuleLoaderTests : BunitContext
{
    [Fact]
    public async Task ImportAsync_ShouldImportModuleOnce_WhenSamePathIsRequestedTwice()
    {
        JSInterop.SetupModule("./module.js");
        var loader = new JsModuleLoader(JSInterop.JSRuntime);

        var first = await loader.ImportAsync("./module.js");
        var second = await loader.ImportAsync("./module.js");

        Assert.Same(first, second);
        JSInterop.VerifyInvoke("import", calledTimes: 1);
    }

    [Fact]
    public async Task ImportAsync_ShouldRetryImport_WhenEarlierImportFailed()
    {
        JSInterop.SetupModule("./module.js");
        var loader = new JsModuleLoader(new FirstCallFailsJSRuntime(JSInterop.JSRuntime));
        await Assert.ThrowsAsync<JSException>(() => loader.ImportAsync("./module.js"));

        var module = await loader.ImportAsync("./module.js");

        Assert.NotNull(module);
    }

    [Fact]
    public async Task ImportAsync_ShouldImportEachModule_WhenPathsDiffer()
    {
        JSInterop.SetupModule("./first.js");
        JSInterop.SetupModule("./second.js");
        var loader = new JsModuleLoader(JSInterop.JSRuntime);

        await loader.ImportAsync("./first.js");
        await loader.ImportAsync("./second.js");

        JSInterop.VerifyInvoke("import", calledTimes: 2);
    }

    // bUnit can't make a module import fail, so this stands in for a transient network failure.
    private sealed class FirstCallFailsJSRuntime(IJSRuntime inner) : IJSRuntime
    {
        private bool _failed;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args)
        {
            if (!_failed)
            {
                _failed = true;
                return ValueTask.FromException<TValue>(new JSException("Failed to fetch the module."));
            }

            return inner.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }
    }
}
