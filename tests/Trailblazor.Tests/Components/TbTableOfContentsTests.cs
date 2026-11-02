using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbTableOfContentsTests : BunitContext
{
    public TbTableOfContentsTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldListLinksToEachSectionInANamedLandmark_WhenGivenItems()
    {
        var cut = RenderGuide();

        Assert.Equal("On this page", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal(["http://localhost/#route", "http://localhost/#climb", "http://localhost/#switchbacks",
            "http://localhost/#water"], cut.FindAll("a").Select(link => link.GetAttribute("href")));
        Assert.Equal("The climb", cut.Find("nav > ul > li > ul > li > a").TextContent);
        Assert.Equal(["0", "1", "2", "0"], cut.FindAll("a")
            .Select(link => link.GetAttribute("style")!.Split(':')[1].Trim()));
        Assert.Empty(cut.FindAll("[aria-current]"));
    }

    [Fact]
    public async Task Active_ShouldMarkTheSectionInViewAndRaiseActiveChanged_WhenTheScriptReportsIt()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/table-of-contents.js");
        module.SetupModule("attachTableOfContents", _ => true);
        var raised = new List<string?>();
        var cut = RenderGuide(parameters => parameters.Add(contents => contents.ActiveChanged,
            (string? active) => raised.Add(active)));
        var events = ((DotNetObjectReference<TableOfContentsEvents>)module.Invocations["attachTableOfContents"]
            .Single().Arguments[1]!).Value;

        await cut.InvokeAsync(() => events.OnActive("climb"));
        Assert.Equal("location", cut.Find("[data-target=climb]").GetAttribute("aria-current"));
        Assert.Single(cut.FindAll("[aria-current]"));
        await cut.InvokeAsync(() => events.OnActive(null));

        Assert.Empty(cut.FindAll("[aria-current]"));
        Assert.Null(cut.Instance.Active);
        Assert.Equal(["climb", null], raised);
    }

    [Fact]
    public void Render_ShouldPointTheLinksAtTheNewAddress_WhenThePageNavigatesWithoutReplacingIt()
    {
        var cut = RenderGuide();

        Services.GetRequiredService<NavigationManager>().NavigateTo("trails/ridge-loop");

        Assert.Equal("http://localhost/trails/ridge-loop#route", cut.Find("[data-target=route]").GetAttribute("href"));
    }

    [Fact]
    public void Render_ShouldPutAnItemsClassStyleAndAttributesOnItsListItem_WhenTheyArePassed()
    {
        var cut = Render<TbTableOfContents>(parameters => parameters.AddChildContent<TbTableOfContentsItem>(item => item
            .Add(link => link.TargetId, "water").Add(link => link.Text, "Water").Add(link => link.Class, "stop")
            .Add(link => link.Style, "color: teal").AddUnmatched("data-kind", "supply")));

        var listItem = cut.Find("li");
        Assert.Contains("stop", listItem.ClassList);
        Assert.Equal("color: teal", listItem.GetAttribute("style"));
        Assert.Equal("supply", listItem.GetAttribute("data-kind"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnItemIsOutsideATableOfContents()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbTableOfContentsItem>(parameters => parameters
            .Add(item => item.TargetId, "route").Add(item => item.Text, "The route")));
    }

    private IRenderedComponent<TbTableOfContents> RenderGuide(
        Action<ComponentParameterCollectionBuilder<TbTableOfContents>>? configure = null) =>
        Render<TbTableOfContents>(parameters =>
        {
            parameters.AddChildContent<TbTableOfContentsItem>(route => route.Add(item => item.TargetId, "route")
                    .Add(item => item.Text, "The route")
                    .AddChildContent<TbTableOfContentsItem>(climb => climb.Add(item => item.TargetId, "climb")
                        .Add(item => item.Text, "The climb")
                        .AddChildContent<TbTableOfContentsItem>(switchbacks => switchbacks
                            .Add(item => item.TargetId, "switchbacks").Add(item => item.Text, "Switchbacks"))))
                .AddChildContent<TbTableOfContentsItem>(water => water.Add(item => item.TargetId, "water")
                    .Add(item => item.Text, "Water"));
            configure?.Invoke(parameters);
        });
}
