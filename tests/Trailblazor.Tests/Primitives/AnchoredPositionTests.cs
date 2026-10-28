using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Primitives;

public class AnchoredPositionTests : BunitContext
{
    [Fact]
    public async Task CreateAsync_ShouldPassElementsAndOptionsToJs_WhenCalled()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor.Primitives/anchored-position.js");
        module.SetupModule("createAnchoredPosition", _ => true);
        var floating = new ElementReference("floating");
        var anchor = new ElementReference("anchor");
        var options = new AnchoredPositionOptions { Side = AnchorSide.Top, Offset = 4 };

        await AnchoredPosition.CreateAsync(new JsModuleLoader(JSInterop.JSRuntime), floating, anchor, options);

        var invocation = module.VerifyInvoke("createAnchoredPosition");
        Assert.Equal([floating, anchor, options], invocation.Arguments);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeJsPositionOnce_WhenCalledTwice()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor.Primitives/anchored-position.js")
            .SetupModule("createAnchoredPosition", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        var loader = new JsModuleLoader(JSInterop.JSRuntime);
        var position = await AnchoredPosition.CreateAsync(loader, default, default, new());

        await position.DisposeAsync();
        await position.DisposeAsync();

        handle.VerifyInvoke("dispose", calledTimes: 1);
    }

    [Fact]
    public void Serialize_ShouldWriteSideAndAlignAsNames_WhenOptionsAreSentToJs()
    {
        var options = new AnchoredPositionOptions { Side = AnchorSide.Start, Align = AnchorAlign.End };

        var json = JsonSerializer.Serialize(options, JsonSerializerOptions.Web);

        Assert.Contains("\"side\":\"Start\"", json);
        Assert.Contains("\"align\":\"End\"", json);
    }
}
