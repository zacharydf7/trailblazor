using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbThemePickerTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;

    public TbThemePickerTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/theme.js");
        _module.SetupVoid("apply", _ => true).SetVoidResult();
        _module.Setup<string?[]?>("read").SetResult(["light", "graphite"]);
        JSInterop.SetupModule("./_content/Trailblazor/toggle-group.js").SetupModule("attachToggleGroup", _ => true);
    }

    [Fact]
    public void Render_ShouldPressNothing_WhenItHasNotReadThePageYet()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbThemePicker>();

        Assert.All(cut.FindAll("button"), item => Assert.Equal("false", item.GetAttribute("aria-pressed")));
    }

    [Fact]
    public void Click_ShouldPressTheLoadedModeThenSetTheOnePressed_WhenInteractive()
    {
        var cut = Render<TbThemePicker>();

        cut.WaitForAssertion(() => Assert.Equal("true", Item(cut, "Light").GetAttribute("aria-pressed")));
        Item(cut, "Dark").Click();

        Assert.Equal(["tbTheme", "dark", true], _module.VerifyInvoke("apply").Arguments);
        Assert.Equal("true", Item(cut, "Dark").GetAttribute("aria-pressed"));
        Assert.Equal("Theme", cut.Find("[role=group]").GetAttribute("aria-label"));
    }

    private static IElement Item(IRenderedComponent<TbThemePicker> cut, string name) =>
        cut.Find($"button[aria-label='{name}']");
}
