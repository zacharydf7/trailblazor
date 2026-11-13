using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Internal.Events;

namespace Trailblazor.Tests.Components;

public class TbContextMenuTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;

    public TbContextMenuTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/menu.js");
        _module.SetupModule("createContextMenu", _ => true);
    }

    // One item that names the key it was drawn for.
    private static readonly RenderFragment<ContextMenuContext> DeleteItem = context => builder =>
    {
        builder.OpenComponent<TbMenuItem>(0);
        builder.AddAttribute(1, nameof(TbMenuItem.ChildContent),
            (RenderFragment)(text => text.AddContent(0, $"Delete {context.Key ?? "all"}")));
        builder.CloseComponent();
    };

    private IRenderedComponent<TbContextMenu> RenderMenu(Action<ComponentParameterCollectionBuilder<TbContextMenu>>?
        configure = null) => Render<TbContextMenu>(parameters =>
    {
        parameters.Add(menu => menu.Items, DeleteItem).AddChildContent("<p data-context-key=\"tent\">Tent</p>");
        configure?.Invoke(parameters);
    });

    [Fact]
    public void Render_ShouldDrawTheItemsForNoKeyInAHiddenMenu_WhenFirstRendered()
    {
        var cut = RenderMenu(parameters => parameters.Add(menu => menu.AriaLabel, "Trail actions"));

        var menu = cut.Find("[data-part=target] > [role=menu]");
        Assert.Equal("manual", menu.GetAttribute("popover"));
        Assert.Equal("Trail actions", menu.GetAttribute("aria-label"));
        Assert.Equal("", menu.GetAttribute("data-key"));
        Assert.False(menu.HasAttribute("data-open"));
        Assert.Equal("Delete all", menu.QuerySelector("[role=menuitem]")!.TextContent.Trim());
        Assert.Equal("Tent", cut.Find("[data-context-key=tent]").TextContent);
    }

    [Fact]
    public async Task Opening_ShouldDrawTheItemsAndMarkTheMenuForTheKey_WhenTheScriptAsksForAKey()
    {
        var cut = RenderMenu();

        await cut.Find("[data-part=target]").TriggerEventAsync("ontbcontextmenuopen",
            new ContextMenuOpenEventArgs { Key = "tent", Request = 3 });

        Assert.Equal("tent", cut.Find("[role=menu]").GetAttribute("data-key"));
        Assert.Equal("3", cut.Find("[role=menu]").GetAttribute("data-request"));
        Assert.Equal("Delete tent", cut.Find("[role=menuitem]").TextContent.Trim());
    }

    [Fact]
    public async Task Opening_ShouldDrawTheItemsForNoKey_WhenTheScriptAsksForAnEmptyKey()
    {
        var cut = RenderMenu();
        var target = cut.Find("[data-part=target]");
        await target.TriggerEventAsync("ontbcontextmenuopen", new ContextMenuOpenEventArgs { Key = "tent" });

        await cut.Find("[data-part=target]").TriggerEventAsync("ontbcontextmenuopen", new ContextMenuOpenEventArgs());

        Assert.Equal("", cut.Find("[role=menu]").GetAttribute("data-key"));
        Assert.Equal("Delete all", cut.Find("[role=menuitem]").TextContent.Trim());
    }

    [Fact]
    public async Task Opening_ShouldLeaveAnOuterMenusItemsAlone_WhenAnInnerMenuAsksForAKey()
    {
        var cut = Render<TbContextMenu>(parameters => parameters.Add(menu => menu.Items, DeleteItem)
            .AddChildContent<TbContextMenu>(inner => inner.Add(menu => menu.Items, DeleteItem)
                .AddChildContent("<p data-context-key=\"tent\">Tent</p>")));

        await cut.FindAll("[data-part=target]")[1].TriggerEventAsync("ontbcontextmenuopen",
            new ContextMenuOpenEventArgs { Key = "tent" });

        // The inner menu comes first, inside the outer one's target.
        Assert.Equal(["tent", ""], cut.FindAll("[role=menu]").Select(menu => menu.GetAttribute("data-key")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMarkTheTargetDisabled_WhenDisabledIsSet(bool disabled)
    {
        var cut = RenderMenu(parameters => parameters.Add(menu => menu.Disabled, disabled));

        Assert.Equal(disabled, cut.Find("[data-part=target]").HasAttribute("data-disabled"));
    }

    [Fact]
    public void Render_ShouldStartTheScriptWithTheTargetAndTheMenu_WhenFirstRendered()
    {
        var cut = RenderMenu();
        cut.Render();

        var call = Assert.Single(JSInterop.Invocations["createContextMenu"]);
        Assert.IsType<ElementReference>(call.Arguments[0]);
        Assert.IsType<ElementReference>(call.Arguments[1]);
        Assert.IsType<DotNetObjectReference<MenuEvents>>(call.Arguments[2]);
    }

    [Fact]
    public async Task OpenChanged_ShouldBeRaisedOnceForEachChange_WhenTheScriptReportsOpeningAndClosing()
    {
        var changes = new List<bool>();
        var cut = RenderMenu(parameters => parameters.Add(menu => menu.OpenChanged, open => changes.Add(open)));
        var events = Events();

        await cut.InvokeAsync(() => events.OnToggle(true));
        Assert.True(cut.Find("[role=menu]").HasAttribute("data-open"));
        await cut.InvokeAsync(() => events.OnToggle(true));
        await cut.InvokeAsync(() => events.OnToggle(false));

        Assert.Equal([true, false], changes);
        Assert.False(cut.Find("[role=menu]").HasAttribute("data-open"));
    }

    [Fact]
    public void Render_ShouldAddPartClasses_WhenClassesAreSet()
    {
        var cut = RenderMenu(parameters => parameters.Add(menu => menu.Classes,
            new MenuClasses { Menu = "my-menu", Item = "my-item", Label = "my-label" }));

        Assert.Contains("my-menu", cut.Find("[role=menu]").ClassList);
        Assert.Contains("my-item", cut.Find("[role=menuitem]").ClassList);
        Assert.Contains("my-label", cut.Find("[role=menuitem] [data-part=label]").ClassList);
    }

    [Fact]
    public async Task Dispose_ShouldDisposeTheScript_WhenTheMenuIsRemoved()
    {
        var handle = _module.SetupModule("createContextMenu", _ => true);
        handle.SetupVoid("dispose", _ => true).SetVoidResult();
        var cut = RenderMenu();

        await cut.Instance.DisposeAsync();

        Assert.Single(handle.Invocations["dispose"]);
    }

    private MenuEvents Events() =>
        ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createContextMenu"].Single().Arguments[2]!).Value;
}
