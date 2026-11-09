using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbAppShellTests : BunitContext
{
    public TbAppShellTests()
    {
        this.AddTrailblazor();
    }

    private static readonly RenderFragment Bar = builder =>
    {
        builder.OpenComponent<TbAppBar>(0);
        builder.AddComponentParameter(1, nameof(TbAppBar.ChildContent), (RenderFragment)(title => title.AddContent(0,
            "Trail log")));
        builder.CloseComponent();
    };

    private static readonly RenderFragment Menu = builder =>
    {
        builder.OpenComponent<TbNavMenu>(0);
        builder.AddComponentParameter(1, nameof(TbNavMenu.ChildContent), (RenderFragment)(links =>
        {
            links.OpenComponent<TbNavLink>(0);
            links.AddComponentParameter(1, nameof(TbNavLink.Href), "trails");
            links.AddComponentParameter(2, nameof(TbNavLink.ChildContent), (RenderFragment)(text => text.AddContent(0,
                "Trails")));
            links.CloseComponent();
        }));
        builder.CloseComponent();
    };

    private IRenderedComponent<TbAppShell> RenderShell(Action<ComponentParameterCollectionBuilder<TbAppShell>>?
        configure = null) => Render<TbAppShell>(parameters =>
    {
        parameters.Add(shell => shell.Header, Bar).Add(shell => shell.Navigation, Menu)
            .Add(shell => shell.Footer, "Made on the trail").AddChildContent("Today's hikes");
        configure?.Invoke(parameters);
    });

    [Fact]
    public void Render_ShouldLayOutTheLandmarksAndASkipLink_WhenGivenEachPart()
    {
        var cut = RenderShell();

        var main = cut.Find("main");
        var nav = cut.Find("nav[data-part=navigation]");

        Assert.Equal("Main", nav.GetAttribute("aria-label"));
        Assert.Equal(("-1", "Today's hikes"), (main.GetAttribute("tabindex"), main.TextContent));
        Assert.EndsWith($"#{main.Id}", cut.Find("[data-part=skip-link]").GetAttribute("href"));
        Assert.Equal("Skip to content", cut.Find("[data-part=skip-link]").TextContent);
        Assert.Equal("Trail log", cut.Find("header[data-part=app-bar] [data-part=title]").TextContent);
        Assert.Equal("Made on the trail", cut.Find("footer").TextContent);
    }

    [Fact]
    public void MenuButton_ShouldOpenTheNavigationInADrawer_WhenPressed()
    {
        var changes = new List<bool>();
        var cut = RenderShell(parameters => parameters.Add(shell => shell.NavigationOpenChanged,
            (bool open) => changes.Add(open)));
        var button = cut.Find("[data-part=menu-button]");
        Assert.Equal(("Navigation", "false", "dialog"), (button.GetAttribute("aria-label"),
            button.GetAttribute("aria-expanded"), button.GetAttribute("aria-haspopup")));

        button.Click();

        var drawer = cut.Find("dialog");
        Assert.Equal("Navigation", drawer.QuerySelector("h2")!.TextContent);
        Assert.Equal("Main", drawer.QuerySelector("nav")!.GetAttribute("aria-label"));
        Assert.Equal("true", cut.Find("[data-part=menu-button]").GetAttribute("aria-expanded"));
        Assert.Equal([true], changes);
    }

    [Fact]
    public void Navigation_ShouldCloseTheDrawer_WhenTheAddressChanges()
    {
        var cut = RenderShell(parameters => parameters.Add(shell => shell.NavigationOpen, true));
        Assert.Single(cut.FindAll("dialog"));

        Services.GetRequiredService<NavigationManager>().NavigateTo("trails");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("dialog")));
    }

    [Fact]
    public void CollapseButton_ShouldNarrowTheNavigationToARail_WhenPressed()
    {
        var changes = new List<bool>();
        var cut = RenderShell(parameters => parameters.Add(shell => shell.NavigationCollapsedChanged,
            (bool collapsed) => changes.Add(collapsed)));
        var button = cut.Find("[data-part=collapse-button]");
        Assert.Equal(("true", cut.Find("nav[data-part=navigation]").Id), (button.GetAttribute("aria-expanded"),
            button.GetAttribute("aria-controls")));

        button.Click();

        Assert.Contains("tb-app-shell--collapsed", cut.Find("[data-part=app-shell]").ClassList);
        Assert.Equal("false", cut.Find("[data-part=collapse-button]").GetAttribute("aria-expanded"));
        Assert.Equal([true], changes);
    }

    [Fact]
    public void Render_ShouldLeaveOutTheMenuButtons_WhenThereIsNoNavigation()
    {
        var cut = Render<TbAppShell>(parameters => parameters.Add(shell => shell.Header, Bar));

        Assert.Empty(cut.FindAll("[data-part=menu-button], [data-part=collapse-button], nav, dialog"));
        Assert.Contains("tb-app-shell--no-navigation", cut.Find("[data-part=app-shell]").ClassList);
    }

    [Fact]
    public void Render_ShouldUseTheGivenLabels_WhenTheyAreSet()
    {
        var cut = RenderShell(parameters => parameters.Add(shell => shell.NavigationLabel, "Trail log")
            .Add(shell => shell.MenuLabel, "Menu").Add(shell => shell.SkipLinkText, "Skip to the hikes"));

        Assert.Equal("Trail log", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Menu", cut.Find("[data-part=menu-button]").GetAttribute("aria-label"));
        Assert.Equal("Skip to the hikes", cut.Find("[data-part=skip-link]").TextContent);
    }

    [Fact]
    public void AppBar_ShouldShowNoMenuButton_WhenItIsOutsideAShell()
    {
        var cut = Render<TbAppBar>(parameters => parameters.Add(bar => bar.Start, "logo")
            .Add(bar => bar.End, "account").AddChildContent("Trail log"));

        Assert.Empty(cut.FindAll("button"));
        Assert.Equal(["logo", "Trail log", "account"], new[] { "start", "title", "end" }
            .Select(part => cut.Find($"[data-part={part}]").TextContent));
    }
}
