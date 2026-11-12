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

    // A row with NoValue gives the tree no Value, and its Value here only names it in the test.
    private sealed record Row(string Value, Row[] Children, string? Text = null, bool Disabled = false,
        string? Class = null, bool NoValue = false)
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
        // Each item draws its row in place, so the page already shows the new order; the tree learns it from the
        // browser, and the first row in it takes the tab stop.
        Assert.Equal("docs", cut.Find("[data-part=item][tabindex='0']").GetAttribute("data-key"));
        await cut.InvokeAsync(() => Events().OnItemOrder(order));

        Assert.Equal(["src", "docs"], cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key")));
        Assert.Equal("src", cut.Find("[data-part=item][tabindex='0']").GetAttribute("data-key"));
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
    public void TreeItem_ShouldOpenButNotBeSelectedCheckedActivatedOrReported_WhenItHasNoValue()
    {
        List<IReadOnlyCollection<string>> expanded = [];
        Row[] rows = [new("guides", [new("guide"), new("api")], "Guides", NoValue: true)];
        var cut = RenderFiles(rows, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.CascadeChecks, false)
            .Add(tree => tree.ExpandedKeysChanged, (IReadOnlyCollection<string> keys) => expanded.Add(keys)));

        var heading = cut.Find("[data-text=Guides]");
        Assert.Equal((null, null), (heading.GetAttribute("aria-selected"), heading.GetAttribute("aria-checked")));
        Assert.Empty(heading.QuerySelectorAll("[data-part=check]"));
        heading.Click();
        heading.DoubleClick();
        cut.Find("[data-key=guide] [data-part=check]").Click();
        cut.Find("[data-key=api] [data-part=check]").Click();

        Assert.Equal(3, cut.FindAll("[data-part=item]").Count);
        Assert.Equal([[]], expanded);
        Assert.Empty(_selectedChanges);
        Assert.Empty(_activated);
        Assert.Equal(["api", "guide"], _checkedChanges[^1].Order());
    }

    [Fact]
    public void TreeItem_ShouldCheckTheRowsInsideItWithoutReportingItself_WhenItHasNoValueAndChecksCascade()
    {
        Row[] rows = [new("trips", [new("trips-new"), new("trips-changed")], "Trips", NoValue: true),
            new("empty", [], "Empty", NoValue: true)];
        var cut = RenderFiles(rows, parameters => parameters.Add(tree => tree.Checkable, true)
            .Add(tree => tree.InitialExpandDepth, 1));

        cut.Find("[data-text=Trips] [data-part=check]").Click();

        Assert.Equal(["trips-changed", "trips-new"], _checkedChanges.Single().Order());
        Assert.Equal("true", cut.Find("[data-text=Trips]").GetAttribute("aria-checked"));
        Assert.Null(cut.Find("[data-text=Empty]").GetAttribute("aria-checked"));
    }

    [Fact]
    public async Task TreeItem_ShouldNotBeDraggedOrDroppedBeside_WhenItHasNoValue()
    {
        List<TreeMove<string>> drops = [];
        Row[] rows = [new("guides", [new("guide")], "Guides", NoValue: true), new("src")];
        var cut = RenderFiles(rows, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1)
            .Add(tree => tree.OnMove, (TreeMove<string> drop) => drops.Add(drop)));
        var heading = cut.Find("[data-text=Guides]");
        Assert.False(heading.HasAttribute("data-draggable"));
        Assert.True(cut.Find("[data-key=guide]").HasAttribute("data-draggable"));

        var check = await cut.InvokeAsync(() =>
            Events().OnDragOver(["src"], heading.GetAttribute("data-key")!, TreeMovePosition.Before));
        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs
            {
                Rows = ["src"], Target = heading.GetAttribute("data-key")!, Position = TreeMovePosition.Inside,
            });

        Assert.False(check.Allowed);
        Assert.Empty(drops);
    }

    [Fact]
    public async Task TreeItem_ShouldStayOpen_WhenItHasNoValueAndTheTreesExpandedKeysAreBound()
    {
        IReadOnlyCollection<string> expanded = [];
        Row[] rows = [new("guides", [new("guide", new Row("intro"))], "Guides", NoValue: true)];
        IRenderedComponent<TbTreeView<string, string>>? cut = null;
        cut = RenderFiles(rows, parameters => parameters.Add(tree => tree.ExpandedKeys, expanded)
            .Add(tree => tree.ExpandedKeysChanged, (IReadOnlyCollection<string> keys) =>
            {
                expanded = keys;
                cut!.Render(again => again.Add(tree => tree.ExpandedKeys, keys));
            }));

        cut.Find("[data-text=Guides] [data-part=toggle]").Click();
        await cut.InvokeAsync(() => cut.Instance.ExpandAsync("guide"));

        Assert.Equal(["Guides", "guide", "intro"],
            cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-text")));
        Assert.Equal(["guide"], expanded);
    }

    [Fact]
    public void TreeItem_ShouldHaveNoInsertSlotInsideIt_WhenItHasNoValue()
    {
        List<TreeInsertContext<string>> slots = [];
        Row[] rows = [new("guides", [new("guide")], "Guides", NoValue: true), new("src")];
        var cut = RenderFiles(rows, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1)
            .Add(tree => tree.InsertTemplate, slot => builder =>
            {
                slots.Add(slot);
                builder.AddContent(0, "+");
            }));

        Assert.Equal(2, cut.FindAll("[data-part=insert]").Count);
        Assert.Equal([new(null, 0), new(null, 2)], slots.Distinct());
    }

    [Fact]
    public void TreeItem_ShouldHaveNoText_WhenItHasNoValueOrTextAndItsTypeIsAValueType()
    {
        var cut = Render<TbTreeView<int, int>>(parameters => parameters.Add(tree => tree.AriaLabel, "Numbers")
            .Add(tree => tree.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTreeItem<int>>(0);
                builder.CloseComponent();
            })));

        var row = cut.Find("[data-part=item]");
        Assert.Equal(("", ""), (row.GetAttribute("data-text"), row.QuerySelector(".tb-tree__text")!.TextContent));
    }

    [Fact]
    public void TreeItem_ShouldShowEachRow_WhenRowsWithNoValueRepeatATextThatCouldNotBeAKey()
    {
        Row[] rows = [new("first", [], "P", NoValue: true), new("second", [], "P", NoValue: true), new("P")];
        var cut = RenderFiles(rows);

        var shown = cut.FindAll("[data-part=item]");
        Assert.Equal(["P", "P", "P"], shown.Select(row => row.GetAttribute("data-text")));
        Assert.Equal(3, shown.Select(row => row.GetAttribute("data-key")).Distinct().Count());
    }

    [Fact]
    public void TreeItem_ShouldNotBePassedToTheTreesItemFunctionsOrTemplates_WhenItHasNoValue()
    {
        Row[] rows = [new("guides", [new("guide"), new("api")], "Guides", NoValue: true)];
        var cut = RenderFiles(rows, parameters => parameters.Add(tree => tree.InitialExpandDepth, 1)
            .Add(tree => tree.ItemDisabled, value => value.Length == 0)
            .Add(tree => tree.IsSelectable, value => value.Length > 0)
            .Add(tree => tree.ItemText, value => value.ToUpperInvariant())
            .Add(tree => tree.Filter, value => value.StartsWith('g'))
            .Add(tree => tree.ItemEnd, context => builder => builder.AddContent(0, context.Item.Length)));

        Assert.Equal(["Guides", "GUIDE"],
            cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-text")));
        Assert.Empty(cut.FindAll("[data-text=Guides] [data-part=end]"));
        Assert.Equal("5", cut.Find("[data-key=guide] [data-part=end]").TextContent);
    }

    [Fact]
    public void TreeItem_ShouldStartATreeInsideItsRowAtTheFirstLevel_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderFragment inner = builder =>
        {
            builder.OpenComponent<TbTreeView<string, string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeView<string, string>.AriaLabel), "Inner");
            builder.AddComponentParameter(2, nameof(TbTreeView<string, string>.ChildContent),
                Fragment([new Row("x")], null));
            builder.CloseComponent();
        };

        var cut = RenderFiles([new Row("docs", new Row("guide"))], parameters => parameters
            .Add(tree => tree.InitialExpandDepth, 1), value => value == "guide" ? inner : text => text.AddContent(0, value));

        Assert.Equal("1", cut.Find("[aria-label=Inner] [role=treeitem]").GetAttribute("aria-level"));
    }

    [Fact]
    public void TreeItem_ShouldSelectOnlyTheInnerRow_WhenARowOfATreeInsideItIsClicked()
    {
        List<IReadOnlyCollection<string>> inner = [];
        RenderFragment sections = builder =>
        {
            builder.OpenComponent<TbTreeView<string, string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeView<string, string>.AriaLabel), "Sections");
            builder.AddComponentParameter(2, nameof(TbTreeView<string, string>.SelectedKeysChanged),
                EventCallback.Factory.Create<IReadOnlyCollection<string>>(this, keys => inner.Add(keys)));
            builder.AddComponentParameter(3, nameof(TbTreeView<string, string>.ChildContent),
                Fragment([new Row("intro")], null));
            builder.CloseComponent();
        };
        var cut = RenderFiles([new Row("chapter")], content: value => value == "chapter" ? sections : text => text.AddContent(0, value));

        cut.Find("[aria-label=Sections] [data-key=intro]").Click();

        Assert.Equal([["intro"]], inner);
        Assert.Empty(_selectedChanges);
    }

    [Fact]
    public void TreeItem_ShouldDrawItsRowAndWhatIsWrittenInsideItInPlace_WhenTheTreeIsInteractive()
    {
        var showNotes = false;
        RenderFragment rows = builder =>
        {
            AddRow(builder, new Row("docs", new Row("guide")), null);
            builder.AddMarkupContent(1, "<img alt=\"Divider\" data-testid=\"between\" />");
            if (showNotes)
            {
                AddRow(builder, new Row("notes"), null);
            }

            AddRow(builder, new Row("src"), null);
        };
        var cut = RenderFiles([], parameters => parameters.Add(tree => tree.ChildContent, rows));
        var group = cut.Find("[data-key=docs] + [data-part=group]");
        Assert.True(group.HasAttribute("hidden"));
        Assert.Equal("group", group.GetAttribute("role"));

        showNotes = true;
        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, rows));
        cut.Find("[data-key=docs] [data-part=toggle]").Click();

        Assert.Equal(["docs", "guide", "notes", "src"],
            cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key")));
        Assert.Equal("between", cut.Find("[data-key=docs] + [data-part=group] + img").GetAttribute("data-testid"));
        Assert.False(cut.Find("[data-key=docs] + [data-part=group]").HasAttribute("hidden"));
        Assert.Null(cut.Find("[data-key=guide]").GetAttribute("aria-setsize"));
    }

    [Fact]
    public void TreeItem_ShouldOpenFromItsBindingAndReportTheUser_WhenExpandedIsBoundOnARowWithNoValue()
    {
        var open = true;
        List<bool> reported = [];
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Text), "Guides");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.Expanded), open);
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ExpandedChanged),
                EventCallback.Factory.Create<bool>(this, value => reported.Add(open = value)));
            builder.AddComponentParameter(4, nameof(TbTreeItem<string>.ChildContent), Fragment([new Row("guide")], null));
            builder.CloseComponent();
        };
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, rows));
        Assert.Equal(["Guides", "guide"], cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-text")));

        cut.Find("[data-text=Guides] [data-part=toggle]").Click();
        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, rows));

        Assert.Equal([false], reported);
        Assert.Single(cut.FindAll("[data-part=item]"));
        open = true;
        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, rows));
        Assert.Equal(2, cut.FindAll("[data-part=item]").Count);
        Assert.Equal([false], reported);
    }

    [Fact]
    public void TreeItem_ShouldStayClosed_WhenItsExpandedIsFalseAndTheTreeOpensToADepth()
    {
        RenderFragment rows = builder =>
        {
            // Keyed, as the rows AddRow writes beside it are.
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.SetKey("docs");
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.Expanded), false);
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ChildContent), Fragment([new Row("guide")], null));
            builder.CloseComponent();
            AddRow(builder, new Row("src", new Row("main")), null);
        };
        // Rendered directly, as RenderFiles would give the tree two ChildContents.
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, rows).Add(tree => tree.InitialExpandDepth, 1));

        Assert.Equal(["docs", "src", "main"], cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key")));
        // A value given one way only opens or closes the row when it changes, so the user can still open it.
        cut.Find("[data-key=docs] [data-part=toggle]").Click();
        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, rows));
        Assert.Equal(["docs", "guide", "src", "main"],
            cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key")));
    }

    [Fact]
    public void TreeItem_ShouldClose_WhenItsExpandedTurnsFalseWhileTheFilterHoldsItOpen()
    {
        var open = true;
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.Expanded), open);
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ChildContent), Fragment([new Row("guide")], null));
            builder.CloseComponent();
        };
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, rows).Add(tree => tree.Filter, value => value == "guide"));
        Assert.Equal(2, cut.FindAll("[data-part=item]").Count);

        open = false;
        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, rows));

        Assert.Single(cut.FindAll("[data-part=item]"));
    }

    [Fact]
    public void TreeItem_ShouldOpen_WhenItsExpandedTurnsTrueAfterTheUserClosedItWhileFiltering()
    {
        var open = true;
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.Expanded), open);
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ExpandedChanged),
                EventCallback.Factory.Create<bool>(this, value => open = value));
            builder.AddComponentParameter(4, nameof(TbTreeItem<string>.ChildContent), Fragment([new Row("guide")], null));
            builder.CloseComponent();
        };
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, rows).Add(tree => tree.Filter, value => value == "guide"));
        cut.Find("[data-key=docs] [data-part=toggle]").Click();
        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, rows));
        Assert.Single(cut.FindAll("[data-part=item]"));

        open = true;
        cut.Render(parameters => parameters.Add(tree => tree.ChildContent, rows));

        Assert.Equal(2, cut.FindAll("[data-part=item]").Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExpandAll_ShouldStopOpeningNewRows_WhenARowIsClosed(bool byTheUser)
    {
        RenderFragment Lazy(string value, RenderFragment? inside) => builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), value);
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.HasChildren), true);
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ChildContent), inside);
            builder.CloseComponent();
        };
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, Lazy("docs", Lazy("guide", Fragment([new Row("intro")], null)))));
        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-part=item]").Count));

        if (byTheUser)
        {
            cut.Find("[data-key=docs] [data-part=toggle]").Click();
        }
        else
        {
            cut.Render(parameters => parameters.Add(tree => tree.ExpandedKeys, (IReadOnlyCollection<string>)[]));
        }

        Assert.Equal(["docs"], cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key")));
    }

    [Fact]
    public void TreeItem_ShouldTellItsParent_WhenTheTreesExpandedKeysOpenIt()
    {
        List<bool> reported = [];
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.ExpandedChanged),
                EventCallback.Factory.Create<bool>(this, reported.Add));
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ChildContent), Fragment([new Row("guide")], null));
            builder.CloseComponent();
        };
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, rows).Add(tree => tree.ExpandedKeys, (IReadOnlyCollection<string>)[]));

        cut.Render(parameters => parameters.Add(tree => tree.ExpandedKeys, (IReadOnlyCollection<string>)["docs"]));

        Assert.Equal([true], reported);
    }

    [Fact]
    public void TreeItem_ShouldTellItsParent_WhenTheTreesInitialDepthOpensIt()
    {
        List<bool> reported = [];
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.ExpandedChanged),
                EventCallback.Factory.Create<bool>(this, reported.Add));
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ChildContent), Fragment([new Row("guide")], null));
            builder.CloseComponent();
        };

        RenderFiles([], parameters => parameters.Add(tree => tree.ChildContent, rows).Add(tree => tree.InitialExpandDepth, 1));

        Assert.Equal([true], reported);
    }

    [Fact]
    public void TreeItem_ShouldAddTheTreesPartClasses_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.EndContent),
                (RenderFragment)(end => end.AddContent(0, "3 new")));
            builder.CloseComponent();
        };

        var cut = RenderFiles([], parameters => parameters.Add(tree => tree.ChildContent, rows)
            .Add(tree => tree.Classes, new TreeViewClasses
            {
                Item = "my-item", Toggle = "my-toggle", Content = "my-content", End = "my-end",
            }));

        Assert.Contains("my-item", cut.Find("[role=treeitem]").ClassList);
        Assert.Contains("my-toggle", cut.Find(".tb-tree__toggle").ClassList);
        Assert.Contains("my-content", cut.Find("[data-part=content]").ClassList);
        Assert.Contains("my-end", cut.Find("[data-part=end]").ClassList);
    }

    [Fact]
    public void TreeItem_ShouldFollowItsExpandedAndShowItsEndContent_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.Expanded), false);
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.EndContent),
                (RenderFragment)(end => end.AddContent(0, "3 new")));
            builder.AddComponentParameter(4, nameof(TbTreeItem<string>.ChildContent), Fragment([new Row("guide")], null));
            builder.CloseComponent();
        };

        var cut = RenderFiles([], parameters => parameters.Add(tree => tree.ChildContent, rows)
            .Add(tree => tree.InitialExpandDepth, 1));

        Assert.Equal("false", cut.Find("[role=treeitem]").GetAttribute("aria-expanded"));
        Assert.Equal("3 new", cut.Find("[data-part=end]").TextContent);
        Assert.Single(cut.FindAll("[role=treeitem]"));
    }

    [Fact]
    public async Task TreeItem_ShouldRenderWhatIsInsideItOnlyWhileOpen_WhenItHasChildren()
    {
        RenderFragment Lazy(string value, RenderFragment? inside) => builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), value);
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.HasChildren), true);
            builder.AddComponentParameter(3, nameof(TbTreeItem<string>.ChildContent), (RenderFragment)(content =>
            {
                content.AddMarkupContent(0, $"<span data-testid=\"inside-{value}\"></span>");
                content.AddContent(1, inside);
            }));
            builder.CloseComponent();
        };
        var cut = RenderFiles([], parameters => parameters.Add(tree => tree.ChildContent,
            Lazy("docs", Lazy("guide", Fragment([new Row("intro")], null)))));
        Assert.Equal("false", cut.Find("[data-key=docs]").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[data-testid=inside-docs]"));

        await cut.InvokeAsync(() => cut.Instance.ExpandAllAsync());

        cut.WaitForAssertion(() => Assert.Equal(["docs", "guide", "intro"],
            cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-key"))));
        Assert.Single(cut.FindAll("[data-testid=inside-guide]"));
    }

    [Fact]
    public void TreeItem_ShouldKeepClicksInItsEndContentFromTheRow_WhenGivenEndContent()
    {
        var clicks = 0;
        RenderFragment rows = builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), "docs");
            builder.AddComponentParameter(2, nameof(TbTreeItem<string>.EndContent), (RenderFragment)(end =>
            {
                end.OpenElement(0, "button");
                end.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => clicks++));
                end.AddContent(2, "Hide");
                end.CloseElement();
            }));
            builder.CloseComponent();
        };
        var cut = RenderFiles([], parameters => parameters.Add(tree => tree.ChildContent, rows));

        cut.Find("[data-key=docs] [data-part=end] button").Click();

        Assert.Equal(1, clicks);
        Assert.Empty(_selectedChanges);
    }

    [Fact]
    public void Tree_ShouldThrow_WhenTreeItemsAreVirtualized()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RenderFiles(Docs,
            parameters => parameters.Add(tree => tree.Virtualize, true)));

        Assert.Contains("virtualizes only Items", exception.Message);
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
        if (!row.NoValue)
        {
            builder.AddComponentParameter(1, nameof(TbTreeItem<string>.Value), row.Value);
        }

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
