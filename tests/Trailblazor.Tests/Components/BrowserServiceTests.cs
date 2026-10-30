using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class BrowserServiceTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;

    public BrowserServiceTests()
    {
        Services.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/browser.js");
    }

    private IBrowserService Browser => Services.GetRequiredService<IBrowserService>();

    [Fact]
    public async Task ObserveBreakpoint_ShouldPassTheBreakpointsAndCallBack_WhenTheBrowserReports()
    {
        _module.SetupModule("observeBreakpoint", _ => true);
        var reported = new List<Breakpoint>();

        await Browser.ObserveBreakpointAsync(breakpoint =>
        {
            reported.Add(breakpoint);
            return Task.CompletedTask;
        });
        var arguments = _module.Invocations["observeBreakpoint"].Single().Arguments;
        await ((DotNetObjectReference<BrowserListener<Breakpoint>>)arguments[0]!).Value.OnChange(Breakpoint.Md);

        Assert.Equal([640, 768, 1024, 1280], (int[])arguments[1]!);
        Assert.Equal([Breakpoint.Md], reported);
    }

    [Fact]
    public async Task Dispose_ShouldStopTheBrowserOnce_WhenCalledTwice()
    {
        var handle = _module.SetupModule("observeViewport", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        var observation = await Browser.ObserveViewportAsync(_ => Task.CompletedTask);

        await observation.DisposeAsync();
        await observation.DisposeAsync();

        Assert.Single(handle.Invocations["dispose"]);
    }
}
