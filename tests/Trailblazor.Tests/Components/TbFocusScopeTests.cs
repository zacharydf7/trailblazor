using Bunit;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbFocusScopeTests : BunitContext
{
    private const string ModulePath = "./_content/Trailblazor.Primitives/focus-scope.js";

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
        Assert.Equal(new FocusScopeOptions { Trapped = true, AutoFocus = true, RestoreFocus = false },
            invocation.Arguments[1]);
        Assert.Equal("Save", cut.Find("[data-part=focus-scope] button").TextContent);
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
