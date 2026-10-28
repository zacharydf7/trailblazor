using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Primitives;

public class DismissableLayerTests : BunitContext
{
    private const string ModulePath = "./_content/Trailblazor.Primitives/dismissable-layer.js";

    [Fact]
    public async Task CreateAsync_ShouldPassLayerAndTriggerToJs_WhenCalled()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupModule("createDismissableLayer", _ => true);
        var layer = new ElementReference("layer");
        var trigger = new ElementReference("trigger");

        await DismissableLayer.CreateAsync(new JsModuleLoader(JSInterop.JSRuntime), layer, trigger, default);

        var invocation = module.VerifyInvoke("createDismissableLayer");
        Assert.Equal(layer, invocation.Arguments[0]);
        Assert.Equal(trigger, invocation.Arguments[1]);
    }

    [Fact]
    public async Task OnDismissRequested_ShouldInvokeCallbackWithReason_WhenJsRequestsDismiss()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("createDismissableLayer", _ => true);
        DismissReason? received = null;
        var onDismiss = EventCallback.Factory.Create<DismissReason>(this, reason => received = reason);
        var loader = new JsModuleLoader(JSInterop.JSRuntime);
        var layer = await DismissableLayer.CreateAsync(loader, default, null, onDismiss);

        await layer.OnDismissRequested(DismissReason.PointerDownOutside);

        Assert.Equal(DismissReason.PointerDownOutside, received);
    }

    [Fact]
    public async Task DisposeAsync_ShouldReleaseDotNetReference_WhenCalled()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupModule("createDismissableLayer", _ => true).SetupVoid("dispose").SetVoidResult();
        var layer = await DismissableLayer.CreateAsync(new JsModuleLoader(JSInterop.JSRuntime), default, null, default);
        var arguments = module.VerifyInvoke("createDismissableLayer").Arguments;
        var reference = (DotNetObjectReference<DismissableLayer>)arguments[2]!;

        await layer.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeJsLayerOnce_WhenCalledTwice()
    {
        var handle = JSInterop.SetupModule(ModulePath).SetupModule("createDismissableLayer", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        var layer = await DismissableLayer.CreateAsync(new JsModuleLoader(JSInterop.JSRuntime), default, null, default);

        await layer.DisposeAsync();
        await layer.DisposeAsync();

        handle.VerifyInvoke("dispose", calledTimes: 1);
    }

    [Fact]
    public void Deserialize_ShouldReadReasonName_WhenJsSendsIt()
    {
        var reason = JsonSerializer.Deserialize<DismissReason>("\"FocusOutside\"", JsonSerializerOptions.Web);

        Assert.Equal(DismissReason.FocusOutside, reason);
    }
}
