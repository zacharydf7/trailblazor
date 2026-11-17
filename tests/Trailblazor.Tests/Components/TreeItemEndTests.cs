using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

// A row's end controls drawn by a TbTreeItemEnd in the row's own template.
public class TreeItemEndTests : BunitContext
{
    private readonly List<IReadOnlyCollection<int>> _selectedChanges = [];

    public TreeItemEndTests() => this.AddTrailblazor();

    private sealed record Trail(int Id, string Name, Trail[]? Branches = null);

    // Closes and opens a row from one click, so the tree hides and shows its child without drawing in between.
    private sealed class Reopener : ComponentBase
    {
        private TbTreeView<Trail, int>? _tree;

        [Parameter]
        public RenderFragment<TreeItemContext<Trail, int>>? ItemTemplate { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, async () =>
            {
                await _tree!.CollapseAsync(1);
                await _tree.ExpandAsync(1);
            }));
            builder.CloseElement();
            builder.OpenComponent<TbTreeView<Trail, int>>(2);
            builder.AddComponentParameter(3, nameof(TbTreeView<Trail, int>.Items),
                new[] { new Trail(1, "North", [new(2, "Ridge")]) });
            builder.AddComponentParameter(4, nameof(TbTreeView<Trail, int>.KeySelector),
                (Func<Trail, int>)(trail => trail.Id));
            builder.AddComponentParameter(5, nameof(TbTreeView<Trail, int>.ChildrenSelector),
                (Func<Trail, IEnumerable<Trail>?>)(trail => trail.Branches));
            builder.AddComponentParameter(6, nameof(TbTreeView<Trail, int>.AriaLabel), "Trails");
            builder.AddComponentParameter(7, nameof(TbTreeView<Trail, int>.InitialExpandDepth), 1);
            builder.AddComponentParameter(8, nameof(TbTreeView<Trail, int>.ItemTemplate), ItemTemplate);
            builder.AddComponentReferenceCapture(9, tree => _tree = (TbTreeView<Trail, int>)tree);
            builder.CloseComponent();
        }
    }

    // A row component of an app's own: one count drawn in the row's text and changed by a button at its end.
    private sealed class CountedRow : ComponentBase
    {
        private int _count;

        [Parameter]
        public string Name { get; set; } = "";

        [Parameter]
        public bool ShowEnd { get; set; } = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-part", "text");
            builder.AddContent(2, $"{Name} ({_count})");
            builder.CloseElement();
            if (ShowEnd)
            {
                builder.OpenComponent<TbTreeItemEnd>(3);
                builder.AddComponentParameter(4, nameof(TbTreeItemEnd.ChildContent), (RenderFragment)(end =>
                {
                    end.OpenElement(0, "button");
                    end.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => _count++));
                    end.AddContent(2, $"Add to {Name}");
                    end.CloseElement();
                }));
                builder.CloseComponent();
            }
        }
    }

    [Fact]
    public async Task TreeItemEnd_ShouldShowInTheRowsEndAndShareTheTemplatesState_WhenItsButtonIsClicked()
    {
        var cut = RenderTrails();

        await cut.Find("[data-part=end] button").ClickAsync(new());

        Assert.Equal(("North (1)", 0, 0), (cut.Find("[data-part=content]").TextContent, _selectedChanges.Count,
            cut.FindAll("[data-part=content] button").Count));
        Assert.Equal(["Add to North", "Add to South"], cut.FindAll("[data-part=end]").Select(end => end.TextContent));
    }

    [Fact]
    public void TreeItemEnd_ShouldShowInEachRowsEnd_WhenTheTreeIsVirtualized()
    {
        var cut = RenderTrails(parameters => parameters.Add(tree => tree.Virtualize, true));

        Assert.Equal(["Add to North", "Add to South"], cut.FindAll("[data-part=end]").Select(end => end.TextContent));
    }

    [Fact]
    public void TreeItemEnd_ShouldShowAfterTheItemEndTemplate_WhenTheRowHasBoth()
    {
        var cut = RenderTrails(parameters => parameters.Add(tree => tree.ItemEndTemplate,
            context => builder => builder.AddContent(0, "3 new")));

        Assert.Equal("3 newAdd to North", cut.Find("[data-part=end]").TextContent);
    }

    [Fact]
    public void TreeItemEnd_ShouldLeaveTheRowWithoutAnEnd_WhenTheTemplateNoLongerHasIt()
    {
        var cut = RenderTrails();

        cut.Render(parameters => parameters.Add(tree => tree.ItemTemplate, context => Row(context.Item, false)));

        Assert.Empty(cut.FindAll("[data-part=end]"));
    }

    [Fact]
    public async Task TreeItemEnd_ShouldStayInTheRowsEnd_WhenTheRowIsHiddenAndShownAgainWithoutADrawBetween()
    {
        var cut = Render<Reopener>(parameters => parameters.Add(reopener => reopener.ItemTemplate,
            context => Row(context.Item)));

        await cut.Find("button").ClickAsync(new());

        Assert.Equal(["Add to North", "Add to Ridge"], cut.FindAll("[data-part=end]").Select(end => end.TextContent));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TreeItemEnd_ShouldShowInTheRowsEnd_WhenInATreeItemsTextContent(bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, builder =>
            {
                builder.OpenComponent<TbTreeItem<string>>(0);
                builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
                builder.AddComponentParameter(2, nameof(TbTreeItem<string>.TextContent), (RenderFragment)(text =>
                {
                    text.AddContent(0, "Docs");
                    text.OpenComponent<TbTreeItemEnd>(1);
                    text.AddComponentParameter(2, nameof(TbTreeItemEnd.ChildContent),
                        (RenderFragment)(end => end.AddContent(0, "12 files")));
                    text.CloseComponent();
                }));
                builder.CloseComponent();
            }));

        Assert.Equal(("Docs", "12 files"),
            (cut.Find("[data-part=content]").TextContent.Trim(), cut.Find("[data-part=end]").TextContent));
    }

    [Fact]
    public void TreeItemEnd_ShouldThrow_WhenOutsideATreeRow()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbTreeItemEnd>());

        Assert.Equal("Put TbTreeItemEnd inside a tree's ItemTemplate or a TbTreeItem's TextContent.", thrown.Message);
    }

    private static RenderFragment Row(Trail trail, bool showEnd = true) => builder =>
    {
        builder.OpenComponent<CountedRow>(0);
        builder.AddComponentParameter(1, nameof(CountedRow.Name), trail.Name);
        builder.AddComponentParameter(2, nameof(CountedRow.ShowEnd), showEnd);
        builder.CloseComponent();
    };

    private IRenderedComponent<TbTreeView<Trail, int>> RenderTrails(
        Action<ComponentParameterCollectionBuilder<TbTreeView<Trail, int>>>? configure = null) =>
        Render<TbTreeView<Trail, int>>(parameters =>
        {
            parameters.Add(tree => tree.Items, [new Trail(1, "North"), new Trail(2, "South")])
                .Add(tree => tree.KeySelector, trail => trail.Id).Add(tree => tree.ItemText, trail => trail.Name)
                .Add(tree => tree.AriaLabel, "Trails").Add(tree => tree.ItemTemplate, context => Row(context.Item))
                .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<int> keys) => _selectedChanges.Add(keys));
            configure?.Invoke(parameters);
        });
}
