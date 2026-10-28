using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Primitives;

public class FocusScopeTests : BunitContext
{
    [Fact]
    public async Task CreateAsync_ShouldPassContainerAndOptionsToJs_WhenCalled()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor.Primitives/focus-scope.js");
        module.SetupModule("createFocusScope", _ => true);
        var container = new ElementReference("container");
        var options = new FocusScopeOptions { Trapped = true };

        await FocusScope.CreateAsync(new JsModuleLoader(JSInterop.JSRuntime), container, options);

        var invocation = module.VerifyInvoke("createFocusScope");
        Assert.Equal([container, options], invocation.Arguments);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeJsScopeOnce_WhenCalledTwice()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor.Primitives/focus-scope.js")
            .SetupModule("createFocusScope", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        var scope = await FocusScope.CreateAsync(new JsModuleLoader(JSInterop.JSRuntime), default, new());

        await scope.DisposeAsync();
        await scope.DisposeAsync();

        handle.VerifyInvoke("dispose", calledTimes: 1);
    }
}
