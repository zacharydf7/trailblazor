using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbMenubarTests : BunitContext
{
    private readonly List<string> _chosen = [];

    public TbMenubarTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldBeAMenubarOfMenuItems_WhenGivenMenus()
    {
        var cut = RenderBar();

        var bar = cut.Find("[role=menubar]");
        var triggers = cut.FindAll("[data-part=trigger]");
        Assert.Equal("Trail editor", bar.GetAttribute("aria-label"));
        Assert.Equal([("menuitem", "menu"), ("menuitem", "menu"), ("menuitem", "menu")],
            triggers.Select(trigger => (trigger.GetAttribute("role"), trigger.GetAttribute("aria-haspopup"))));
        Assert.Equal(["File", "Edit", "View"], cut.Tb().Menubar("Trail editor").Menus);
    }

    [Fact]
    public void Render_ShouldShowTriggersAsTextWithoutAChevron_WhenInAMenubar()
    {
        var cut = RenderBar();
        var alone = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Alone")
            .AddChildContent(Item("Only")));

        var trigger = cut.Find("[data-part=trigger]");
        Assert.Contains("tb-button--text", trigger.ClassList);
        Assert.Empty(trigger.QuerySelectorAll("svg"));
        Assert.Null(alone.Find("[data-part=trigger]").GetAttribute("role"));
        Assert.NotEmpty(alone.Find("[data-part=trigger]").QuerySelectorAll("svg"));
    }

    [Fact]
    public async Task Menu_ShouldChooseAnItem_WhenTheHelperIsUsedThroughTheBar()
    {
        var cut = RenderBar();

        await cut.Tb().Menubar().Menu("Edit").ChooseAsync("Undo");

        Assert.Equal(["Undo"], _chosen);
        Assert.Throws<InvalidOperationException>(() => cut.Tb().Menubar().Menu("Help"));
    }

    [Fact]
    public async Task Menu_ShouldFindTheBarsOwnMenu_WhenAnotherMenuHasTheSameName()
    {
        var cut = Render(builder =>
        {
            AddMenu(builder, 0, "Edit", "Paste");
            builder.OpenComponent<TbMenubar>(10);
            builder.AddComponentParameter(11, nameof(TbMenubar.AriaLabel), "Trail editor");
            builder.AddComponentParameter(12, nameof(TbMenubar.ChildContent), Menus);
            builder.CloseComponent();
        });

        await cut.Tb().Menubar("Trail editor").Menu("Edit").ChooseAsync("Undo");

        Assert.Equal(["Undo"], _chosen);
    }

    // The bar's script gives the triggers one tab stop; until it runs, as on a static page, each one is reachable.
    [Fact]
    public void Render_ShouldLeaveEveryTriggerInTheTabOrder_WhenTheScriptHasNotRun()
    {
        var cut = RenderBar();

        Assert.All(cut.FindAll("[data-part=trigger]"), trigger => Assert.Null(trigger.GetAttribute("tabindex")));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoAriaLabel()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbMenubar>());

        Assert.Equal("TbMenubar needs an AriaLabel.", thrown.Message);
    }

    [Fact]
    public void Sizes_ShouldReachEachMenusTriggerAndList_WhenTheMenuSetsNoneOfItsOwn()
    {
        var cut = Render<TbMenubar>(parameters => parameters.Add(bar => bar.AriaLabel, "Trail editor")
            .Add(bar => bar.Size, Size.Small).Add(bar => bar.ItemSize, Size.Large)
            .AddChildContent<TbMenu>(menu => menu.Add(each => each.Label, "File").AddChildContent(Item("New trail")))
            .AddChildContent<TbMenu>(menu => menu.Add(each => each.Label, "View").Add(each => each.Size, Size.Large)
                .Add(each => each.ItemSize, Size.Small).AddChildContent(Item("Zoom in"))));

        var (triggers, lists) = (cut.FindAll("[data-part=trigger]"), cut.FindAll("[data-part=menu]"));
        Assert.Contains("tb-button--small", triggers[0].ClassList);
        Assert.Contains("tb-menu__list--large", lists[0].ClassList);
        // A menu that sets its own keeps them.
        Assert.Contains("tb-button--large", triggers[1].ClassList);
        Assert.Contains("tb-menu__list--small", lists[1].ClassList);
    }

    private IRenderedComponent<TbMenubar> RenderBar() => Render<TbMenubar>(parameters =>
        parameters.Add(bar => bar.AriaLabel, "Trail editor").AddChildContent(Menus));

    private RenderFragment Menus => builder =>
    {
        AddMenu(builder, 0, "File", "New trail");
        AddMenu(builder, 10, "Edit", "Undo");
        AddMenu(builder, 20, "View", "Zoom in");
    };

    private void AddMenu(RenderTreeBuilder builder, int sequence, string label, string item)
    {
        builder.OpenComponent<TbMenu>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbMenu.Label), label);
        builder.AddComponentParameter(sequence + 2, nameof(TbMenu.ChildContent), Item(item));
        builder.CloseComponent();
    }

    private RenderFragment Item(string text) => builder =>
    {
        builder.OpenComponent<TbMenuItem>(0);
        builder.AddComponentParameter(1, nameof(TbMenuItem.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, () => _chosen.Add(text)));
        builder.AddComponentParameter(2, nameof(TbMenuItem.ChildContent), (RenderFragment)(content =>
            content.AddContent(0, text)));
        builder.CloseComponent();
    };
}
