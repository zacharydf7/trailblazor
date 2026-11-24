using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
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
    public void Allow_ShouldNameTheRegionsTheHoldLeavesUsable_WhenGivenOrNot()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("createFocusScope", _ => true).Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(BlockingPath).SetupModule("blockOutside", _ => true).Mode = JSRuntimeMode.Loose;
        this.AddTrailblazor();

        Render<TbFocusScope>(parameters => parameters.Add(each => each.Blocking, true)
            .Add(each => each.Allow, ["#viewer", ".legend"]));
        Render<TbFocusScope>(parameters => parameters.Add(each => each.Blocking, true));

        Assert.Equal([["#viewer", ".legend"], []], JSInterop.Invocations
            .Where(call => call.Identifier == "blockOutside").Select(call => (IEnumerable<string>)call.Arguments[3]!));
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
    public void Blocking_ShouldStartAndEndTheHoldAndKeepTheScope_WhenItChangesAfterTheScopeStarted()
    {
        var scope = JSInterop.SetupModule(ModulePath).SetupModule("createFocusScope", _ => true);
        scope.Mode = JSRuntimeMode.Loose;
        var block = JSInterop.SetupModule(BlockingPath).SetupModule("blockOutside", _ => true);
        block.SetupVoid("dispose").SetVoidResult();
        this.AddTrailblazor(options => options.Dialogs.OutsidePopups.Add(".chat-widget"));
        var cut = Render<TbFocusScope>(parameters => parameters.Add(each => each.AutoFocus, false));
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier == "blockOutside");

        cut.Render(parameters => parameters.Add(each => each.Blocking, true));

        // The scope notes what has the focus before the hold makes it inert.
        Assert.Equal(["noteFocus", "blockOutside"], Since("createFocusScope"));
        var started = JSInterop.Invocations.Single(call => call.Identifier == "blockOutside");
        started.Arguments[0].ShouldBeElementReferenceTo(cut.Find("[data-part=focus-scope]"));
        Assert.Equal([".chat-widget"], (IEnumerable<string>)started.Arguments[1]!);
        Assert.Equal(false, started.Arguments[2]);

        cut.Render(parameters => parameters.Add(each => each.Blocking, false));

        // What had the focus is usable again before the scope, which stays, gives the focus back to it.
        Assert.Equal(["dispose", "holdEnded"], Since("blockOutside"));
        Assert.Single(block.Invocations["dispose"]);
        Assert.Empty(scope.Invocations["dispose"]);
    }

    // The calls made to the scripts after the last one with this name, in order.
    private string[] Since(string identifier) =>
        [.. JSInterop.Invocations.Select(call => call.Identifier).Where(each => each != "import").Reverse()
            .TakeWhile(each => each != identifier).Reverse()];

    [Fact]
    public async Task Dispose_ShouldEndALateHold_WhenTheComponentGoesAwayWhileThatHoldStarts()
    {
        var (script, hold) = (new HeldScript(), new HeldScript());
        this.AddTrailblazor();
        Services.AddScoped(_ => new JsModuleLoader(script));
        var cut = Render<TbFocusScope>();
        cut.Render(parameters => parameters.Add(each => each.Blocking, true));

        await DisposeComponentsAsync();
        script.Holding.SetResult(hold);
        await Renderer.Dispatcher.InvokeAsync(() => { });

        Assert.Contains("dispose", hold.Calls);
    }

    // The scope's scripts, whose blockOutside waits until the test lets it return the hold.
    private sealed class HeldScript : IJSRuntime, IJSObjectReference
    {
        public TaskCompletionSource<IJSObjectReference> Holding { get; } = new();

        public List<string> Calls { get; } = [];

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(identifier);
            return identifier switch
            {
                "import" or "createFocusScope" => (TValue)(object)this,
                "blockOutside" => (TValue)await Holding.Task,
                _ => default!,
            };
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public void Trapped_ShouldTellTheRunningScope_WhenItChangesAfterTheScopeStarted()
    {
        var scope = JSInterop.SetupModule(ModulePath).SetupModule("createFocusScope", _ => true);
        scope.SetupVoid("setTrapped", _ => true).SetVoidResult();
        this.AddTrailblazor();
        var cut = Render<TbFocusScope>();

        cut.Render(parameters => parameters.Add(each => each.Trapped, true));
        cut.Render(parameters => parameters.Add(each => each.Trapped, false));

        Assert.Equal([true, false], scope.Invocations["setTrapped"].Select(call => call.Arguments.Single()));
        Assert.Single(JSInterop.Invocations, call => call.Identifier == "createFocusScope");
    }

    [Fact]
    public void Blocking_ShouldStartAgainOnlyOnceTheEarlierHoldHasEnded_WhenItIsTurnedBackOnWhileThatOneEnds()
    {
        JSInterop.SetupModule(ModulePath).SetupModule("createFocusScope", _ => true).Mode = JSRuntimeMode.Loose;
        var block = JSInterop.SetupModule(BlockingPath).SetupModule("blockOutside", _ => true);
        var ending = block.SetupVoid("dispose");
        this.AddTrailblazor();
        var cut = Render<TbFocusScope>(parameters => parameters.Add(each => each.Blocking, true));
        cut.Render(parameters => parameters.Add(each => each.Blocking, false));

        cut.Render(parameters => parameters.Add(each => each.Blocking, true));

        // The script hasn't answered that the first hold ended, so the second waits its turn.
        Assert.Single(JSInterop.Invocations, call => call.Identifier == "blockOutside");
        ending.SetVoidResult();
        cut.WaitForAssertion(() =>
            Assert.Equal(2, JSInterop.Invocations.Count(call => call.Identifier == "blockOutside")));
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
