using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

// Rows written as nested TbTreeItems rather than given as Items.
public class TreeItemTests : BunitContext
{
    private readonly List<IReadOnlyCollection<string>> _checkedChanges = [];
    private readonly List<IReadOnlyCollection<string>> _selectedChanges = [];
    private readonly List<string> _activated = [];

    public TreeItemTests() => this.AddTrailblazor();

    private sealed record Row(string Value, Row[] Children, string? Text = null, bool Disabled = false,
        string? Class = null)
    {
        public Row(string value, params Row[] children) : this(value, children, null)
        {
        }
    }

    private static readonly Row[] Docs = [new("docs", new Row("guide"), new Row("api")), new("src")];

    [Fact]
    public void TreeItem_ShouldBecomeRowsAtTheirLevels_WhenNested()
    {
        var cut = RenderFiles(Docs, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1));

        var rows = cut.FindAll("[data-part=item]");
        Assert.Equal(["docs", "guide", "api", "src"], rows.Select(row => row.GetAttribute("data-key")));
        Assert.Equal(["1", "2", "2", "1"], rows.Select(row => row.GetAttribute("aria-level")));
        Assert.Equal("true", rows[0].GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[data-part=empty]"));
    }

    [Fact]
    public void TreeItem_ShouldShowItsTextAndContent_WhenGiven()
    {
        var cut = RenderFiles([new Row("docs", [], "Documents")], content: value => builder => builder.AddMarkupContent(0, $"<b>{value}</b>"));

        var row = cut.Find("[data-part=item]");
        Assert.Equal("Documents", row.GetAttribute("data-text"));
        Assert.Equal("docs", row.QuerySelector("b")!.TextContent);
    }

    [Fact]
    public void TreeItem_ShouldCheckItsValuesButNotATurnedOffRow_WhenAParentIsChecked()
    {
        Row[] rows = [new("docs", [new("guide"), new("api", [], Disabled: true)])];
        var cut = RenderFiles(rows, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.InitialExpandDepth, 1));

        cut.Find("[data-key=docs] [data-part=check]").Click();

        Assert.Equal(["guide"], _checkedChanges.Single());
        Assert.Equal("true", cut.Find("[data-key=api]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void TreeItem_ShouldUpdateAndRemoveRows_WhenTheMarkupChanges()
    {
        var cut = RenderFiles(Docs);

        cut.Render(parameters => parameters.Add(tree => tree.ChildContent,
            Fragment([new Row("docs", [], "Documents")], null)));

        var row = cut.Find("[data-part=item]");
        Assert.Equal("Documents", row.GetAttribute("data-text"));
        Assert.Single(cut.FindAll("[data-part=item]"));
    }

    [Fact]
    public void TreeItem_ShouldRenderItsOwnRowsToTheInitialDepth_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderFiles(Docs, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1));

        var rows = cut.FindAll("[role=treeitem]");
        Assert.Equal(["docs", "guide", "api", "src"], rows.Select(row => row.TextContent.Trim()));
        Assert.Equal(["true", null, null, null], rows.Select(row => row.GetAttribute("aria-expanded")));
    }

    [Fact]
    public void TreeItem_ShouldThrow_WhenOutsideATree()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Render<TbTreeItem<string>>(parameters => parameters.Add(item => item.Value, "docs")));

        Assert.Contains("inside a TbTreeView", exception.Message);
    }

    [Fact]
    public void TreeItem_ShouldRenderEachRowOnce_WhenStaticAndRenderedAgain()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderFiles(Docs, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1));

        cut.Render(parameters => parameters.Add(tree => tree.AriaLabel, "Files again"));

        Assert.Equal(4, cut.FindAll("[role=treeitem]").Count);
    }

    [Fact]
    public async Task TreeItem_ShouldFollowTheMarkupOrder_WhenKeyedItemsMove()
    {
        var cut = RenderFiles(Docs);

        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, Fragment([.. Docs.Reverse()], null)));
        var order = cut.FindAll("[data-tree-item]").Select(item => item.GetAttribute("data-tree-item")!).ToArray();
        await cut.InvokeAsync(() => Events().OnItemOrder(order));

        Assert.Equal(["src", "docs"], cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key")));
    }

    [Fact]
    public void TreeItem_ShouldRenderOnce_WhenItsValueIsMadeAnewOnEachRender()
    {
        var renders = 0;

        var cut = Render<TbTreeView<Folder, int>>(parameters => parameters
            .Add(tree => tree.KeySelector, folder => folder.Id)
            .Add(tree => tree.ChildContent, (RenderFragment)(builder =>
            {
                renders++;
                builder.OpenComponent<TbTreeItem<Folder>>(0);
                builder.AddComponentParameter(1, nameof(TbTreeItem<Folder>.Value), new Folder(1));
                builder.CloseComponent();
            })));

        Assert.Single(cut.FindAll("[data-part=item]"));
        Assert.InRange(renders, 1, 3);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TreeItem_ShouldPutItsClassStyleAndAttributesOnItsRow_WhenGiven(bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

        var cut = RenderFiles([new Row("docs", [], Class: "row-class")]);

        var row = cut.Find("[role=treeitem]");
        Assert.Contains("row-class", row.ClassList);
        Assert.Equal("--tb-tree-level: 0; color: red", row.GetAttribute("style"));
        Assert.Equal("docs-row", row.GetAttribute("data-testid"));
    }

    [Fact]
    public void TreeItem_ShouldShowMatchesAndTheirParents_WhenTheTreeFilters()
    {
        var cut = RenderFiles(Docs, parameters => parameters.Add(tree => tree.Filter, value => value == "api"));

        Assert.Equal(["docs", "api"], cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key")));
    }

    [Fact]
    public async Task TreeItem_ShouldOpenAndCloseEveryRow_WhenTheTreeExpandsAndCollapsesAll()
    {
        var cut = RenderFiles(Docs);

        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());
        Assert.Equal(4, cut.FindAll("[data-part=item]").Count);
        await cut.InvokeAsync(() => cut.Instance.CollapseAllAsync());

        Assert.Equal(2, cut.FindAll("[data-part=item]").Count);
    }

    [Fact]
    public void TreeItem_ShouldNotBeSelectedOrActivated_WhenDisabled()
    {
        var cut = RenderFiles([new Row("docs", [], Disabled: true)]);

        cut.Find("[data-key=docs]").Click();
        cut.Find("[data-key=docs]").DoubleClick();

        Assert.Empty(_selectedChanges);
        Assert.Empty(_activated);
    }

    [Fact]
    public void Tree_ShouldThrow_WhenGivenItemsAndTreeItems()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RenderFiles(Docs,
            parameters => parameters.Add(tree => tree.Items, ["other"])));

        Assert.Contains("not both", exception.Message);
    }

    private IRenderedComponent<TbTreeView<string, string>> RenderFiles(Row[] rows,
        Action<ComponentParameterCollectionBuilder<TbTreeView<string, string>>>? configure = null,
        Func<string, RenderFragment>? content = null) =>
        Render<TbTreeView<string, string>>(parameters =>
        {
            parameters.Add(tree => tree.AriaLabel, "Files").Add(tree => tree.ChildContent, Fragment(rows, content))
                .Add(tree => tree.CheckedKeysChanged, (IReadOnlyCollection<string> keys) => _checkedChanges.Add(keys))
                .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<string> keys) => _selectedChanges.Add(keys))
                .Add(tree => tree.OnActivate, (string value) => _activated.Add(value));
            configure?.Invoke(parameters);
        });

    private TreeViewEvents Events() =>
        ((DotNetObjectReference<TreeViewEvents>)JSInterop.Invocations["createTreeView"].Single().Arguments[1]!).Value;

    private static RenderFragment Fragment(Row[] rows, Func<string, RenderFragment>? content) => builder =>
    {
        foreach (var row in rows)
        {
            AddRow(builder, row, content);
        }
    };

    private static void AddRow(RenderTreeBuilder builder, Row row, Func<string, RenderFragment>? content)
    {
        builder.OpenComponent<TbTreeItem<string>>(0);
        builder.SetKey(row.Value);
        builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), row.Value);
        builder.AddComponentParameter(2, nameof(TbTreeItem<string>.Text), row.Text);
        builder.AddComponentParameter(3, nameof(TbTreeItem<string>.Disabled), row.Disabled);
        builder.AddComponentParameter(4, nameof(TbTreeItem<string>.TextContent), content?.Invoke(row.Value));
        if (row.Children.Length > 0)
        {
            builder.AddComponentParameter(5, nameof(TbTreeItem<string>.ChildContent), Fragment(row.Children, content));
        }

        if (row.Class is not null)
        {
            builder.AddAttribute(6, "class", row.Class);
            builder.AddAttribute(7, "style", "color: red");
            builder.AddAttribute(8, "data-testid", $"{row.Value}-row");
        }

        builder.CloseComponent();
    }

    // Compared by reference, so each new instance is a different value with the same key.
    private sealed class Folder(int id)
    {
        public int Id { get; } = id;
    }
}
