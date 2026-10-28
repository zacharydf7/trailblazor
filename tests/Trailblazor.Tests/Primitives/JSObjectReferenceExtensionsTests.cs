using Bunit;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Primitives;

public class JSObjectReferenceExtensionsTests : BunitContext
{
    [Fact]
    public async Task DisposeHandleAsync_ShouldCallJsDispose_WhenCalled()
    {
        var handleInterop = JSInterop.SetupModule("./handle.js");
        handleInterop.SetupVoid("dispose").SetVoidResult();
        var handle = await JSInterop.JSRuntime.InvokeAsync<IJSObjectReference>("import", "./handle.js");

        await handle.DisposeHandleAsync();

        handleInterop.VerifyInvoke("dispose");
    }

    [Fact]
    public async Task DisposeHandleAsync_ShouldNotThrow_WhenCircuitIsDisconnected()
    {
        var handleInterop = JSInterop.SetupModule("./handle.js");
        handleInterop.SetupVoid("dispose").SetException(new JSDisconnectedException("The circuit is gone."));
        var handle = await JSInterop.JSRuntime.InvokeAsync<IJSObjectReference>("import", "./handle.js");

        var exception = await Record.ExceptionAsync(() => handle.DisposeHandleAsync().AsTask());

        Assert.Null(exception);
    }
}
