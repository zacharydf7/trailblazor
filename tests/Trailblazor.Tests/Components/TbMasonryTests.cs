using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbMasonryTests : BunitContext
{
    private readonly BunitJSModuleInterop _handle;

    public TbMasonryTests()
    {
        this.AddTrailblazor();
        _handle = JSInterop.SetupModule("./_content/Trailblazor/masonry.js").SetupModule("attachMasonry", _ => true);
        _handle.SetupVoid("dispose").SetVoidResult();
    }

    [Fact]
    public void Render_ShouldKeepTheCardsInOrder_WhenCalled()
    {
        var cut = Render<TbMasonry>(parameters => parameters
            .AddChildContent("<article>Pine</article><article>Cedar</article><article>Eagle</article>"));

        var root = cut.Find(".tb-masonry");
        Assert.Equal(("div", null), (root.LocalName, root.GetAttribute("role")));
        Assert.Equal("--_tb-masonry-gap: var(--tb-space-4); --_tb-masonry-column: 16rem", root.GetAttribute("style"));
        Assert.Equal(["Pine", "Cedar", "Eagle"], cut.FindAll("article").Select(card => card.TextContent));
    }

    [Fact]
    public void Render_ShouldRenderAListOfCards_WhenAsAList()
    {
        var cut = Render<TbMasonry>(parameters => parameters.Add(masonry => masonry.As, LayoutElement.Ul)
            .Add(masonry => masonry.MinColumnWidth, "12rem").Add(masonry => masonry.Gap, 2)
            .AddChildContent("<li>Pine</li><li>Cedar</li>"));

        var root = cut.Find(".tb-masonry");
        Assert.Equal(("ul", "list"), (root.LocalName, root.GetAttribute("role")));
        Assert.Equal("--_tb-masonry-gap: var(--tb-space-2); --_tb-masonry-column: 12rem", root.GetAttribute("style"));
    }

    [Fact]
    public async Task Render_ShouldPlaceTheCardsWithItsScriptAndLetItGo_WhenInteractiveThenDisposed()
    {
        var cut = Render<TbMasonry>(parameters => parameters.AddChildContent("<article>Pine</article>"));

        Assert.Equal(cut.Find(".tb-masonry").GetAttribute("blazor:elementReference"),
            ((ElementReference)JSInterop.Invocations["attachMasonry"].Single().Arguments[0]!).Id);
        await DisposeComponentsAsync();
        Assert.Single(_handle.Invocations["dispose"]);
    }
}
