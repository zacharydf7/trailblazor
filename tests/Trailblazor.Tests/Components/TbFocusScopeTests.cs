using Bunit;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbFocusScopeTests : BunitContext
{
    private const string ModulePath = "./_content/Trailblazor.Primitives/focus-scope.js";
    private const string BlockingPath = "./_content/Trailblazor/blocking-region.js";

    [Fact]
    public void Render_ShouldStartAScopeOnItsElementWithItsOptions_WhenFirstRendered()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupModule("createFocusScope", _ => true);
        this.AddTrailblazor();

        var cut = Render<TbFocusScope>(parameters => parameters.Add(scope => scope.Trapped, true)
            .Add(scope => scope.RestoreFocus, false).AddChildContent("<button>Save</button>"));

        var invocation = module.VerifyInvoke("createFocusScope");
        invocation.Arguments[0].ShouldBeElementReferenceTo(cut.Find("[data-part=focus-scope]"));
        Assert.Equal(new FocusScopeOptions { Trapped = true, AutoFocus = true, RestoreFocus = false, MarksReady = true },
            invocation.Arguments[1]);
        Assert.Equal("Save", cut.Find("[data-part=focus-scope] button").TextContent);
    }

    [Fact]
    public async Task Blocking_ShouldHoldWhatIsOutsideOnceTheScopeHasStartedAndFreeItFirst_WhenSet()
    {
        var ended = new List<string>();
        var scope = JSInterop.SetupModule(ModulePath).SetupModule("createFocusScope", _ => true);
        scope.SetupVoid("dispose", _ => Note(ended, "scope")).SetVoidResult();
        var block = JSInterop.SetupModule(BlockingPath).SetupModule("blockOutside", _ => true);
        block.SetupVoid("dispose", _ => Note(ended, "block")).SetVoidResult();
        this.AddTrailblazor(options => options.Dialogs.OutsidePopups.Add(".chat-widget"));

        var cut = Render<TbFocusScope>(parameters => parameters.Add(each => each.Blocking, true));

        // The scope notes what has the focus before the block makes it inert, and leaves the ready mark to the
        // block, which comes last.
        var started = JSInterop.Invocations.Where(call => call.Identifier is "blockOutside" or "createFocusScope")
            .ToList();
        Assert.Equal(["createFocusScope", "blockOutside"], started.Select(call => call.Identifier));
        Assert.Equal(new FocusScopeOptions { AutoFocus = true, RestoreFocus = true }, started[0].Arguments[1]);
        started[1].Arguments[0].ShouldBeElementReferenceTo(cut.Find("[data-part=focus-scope]"));
        Assert.Equal([".chat-widget"], (IEnumerable<string>)started[1].Arguments[1]!);
        Assert.Equal(true, started[1].Arguments[2]);

        await DisposeComponentsAsync();

        // What had the focus is usable again before the scope gives the focus back to it.
        Assert.Equal(["block", "scope"], ended);
    }

    private static bool Note(List<string> ended, string name)
    {
        ended.Add(name);
        return true;
    }

    [Fact]
    public void Blocking_ShouldLeaveThePageAsItIs_WhenNotSet()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("createFocusScope", _ => true);
        this.AddTrailblazor();

        Render<TbFocusScope>();

        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier == "blockOutside");
    }

    [Fact]
    public async Task Dispose_ShouldEndTheScope_WhenTheComponentGoesAway()
    {
        var handle = JSInterop.SetupModule(ModulePath).SetupModule("createFocusScope", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        this.AddTrailblazor();
        Render<TbFocusScope>();

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }
}
