using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridTreeTests : BunitContext
{
    public TbDataGridTreeTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Folder(int Id, string Name, int Size, IReadOnlyList<Folder>? Children = null);

    private static readonly Folder Draft = new(4, "Draft", 1);

    private static readonly Folder[] Folders =
    [
        new(1, "Docs", 5, [new(2, "Notes", 3), new(3, "Plans", 9, [Draft])]),
        new(5, "Photos", 20, [new(6, "Trips", 12)]),
        new(7, "Music", 4),
    ];

    private List<IReadOnlyCollection<Folder>> Reported { get; } = [];

    private IRenderedComponent<TbDataGrid<Folder>> RenderFolders(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Folder>>>? configure = null,
        IReadOnlyList<Folder>? items = null) =>
        Render<TbDataGrid<Folder>>(parameters =>
        {
            parameters.Add(grid => grid.Items, items ?? Folders).Add(grid => grid.AriaLabel, "Folders")
                .Add(grid => grid.KeySelector, folder => folder.Id)
                .Add(grid => grid.ChildrenSelector, folder => folder.Children)
                .Add(grid => grid.ExpandedParentsChanged, (IReadOnlyCollection<Folder> open) => Reported.Add(open))
                .AddChildContent<TbColumn<Folder>>(column => column.Add(name => name.Field, folder => folder.Name)
                    .Add(name => name.Sortable, true).Add(name => name.Filterable, true)
                    .Add(name => name.Groupable, true))
                .AddChildContent<TbColumn<Folder>>(column => column.Add(size => size.Field, folder => folder.Size)
                    .Add(size => size.Sortable, true).Add(size => size.Aggregate, ColumnAggregate.Sum));
            configure?.Invoke(parameters);
        });

    // Each row as its level and name, such as "2 Notes".
    private static List<string> Lines(IRenderedComponent<TbDataGrid<Folder>> cut) =>
        [.. cut.FindAll("tbody tr[data-part=row]").Select(row =>
            $"{row.GetAttribute("aria-level")} {row.QuerySelector("td[data-column=Name]")!.TextContent.Trim()}")];

    private static IReadOnlyList<int> Ids(IReadOnlyCollection<Folder> folders) =>
        [.. folders.Select(folder => folder.Id).Order()];

    [Fact]
    public void Render_ShouldShowTheTopRowsOfATreeGrid_WhenGivenAChildrenSelector()
    {
        var cut = RenderFolders();

        Assert.Equal("treegrid", cut.Find("table").GetAttribute("role"));
        Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut));
        var docs = cut.Tb().Grid("Folders").Row("Docs");
        Assert.Equal(("false", "1", "3"), (docs.GetAttribute("aria-expanded"), docs.GetAttribute("aria-posinset"),
            docs.GetAttribute("aria-setsize")));
        var toggle = docs.QuerySelector("td[data-column=Name][data-part=tree-cell] [data-part=tree-toggle]")!;
        Assert.Equal(("Rows under Docs", "false"), (toggle.GetAttribute("aria-label"), toggle.GetAttribute("aria-expanded")));
        var music = cut.Tb().Grid("Folders").Row("Music");
        Assert.Null(music.GetAttribute("aria-expanded"));
        Assert.Null(music.QuerySelector("[data-part=tree-toggle]"));
        Assert.Null(docs.QuerySelector("td[data-column=Size]")!.GetAttribute("data-part"));
    }

    [Fact]
    public async Task ExpandAsync_ShouldShowTheRowsChildrenUnderIt_WhenItsButtonIsClicked()
    {
        var cut = RenderFolders();

        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
        var plans = cut.Tb().Grid("Folders").Row("Plans");
        Assert.Equal(("false", "2", "2"), (plans.GetAttribute("aria-expanded"), plans.GetAttribute("aria-posinset"),
            plans.GetAttribute("aria-setsize")));
        Assert.Equal("true", cut.Tb().Grid("Folders").Row("Docs").GetAttribute("aria-expanded"));
        Assert.Equal([1], Ids(Reported.Single()));
        Assert.Equal("--tb-data-grid-tree-level: 1", plans.QuerySelector(".tb-data-grid__tree")!.GetAttribute("style"));
    }

    [Fact]
    public async Task CollapseAsync_ShouldHideEveryRowUnderIt_WhenItsButtonIsClickedAgain()
    {
        var cut = RenderFolders();
        var grid = cut.Tb().Grid("Folders");
        await grid.ExpandAsync("Docs");
        await grid.ExpandAsync("Plans");
        Assert.Contains("3 Draft", Lines(cut));

        await grid.CollapseAsync("Docs");

        Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut));
        Assert.Equal([3], Ids(Reported[^1]));
        await grid.ExpandAsync("Docs");
        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "3 Draft", "1 Photos", "1 Music"], Lines(cut));
    }

    [Fact]
    public async Task ExpandAsync_ShouldLeaveTheRowOpen_WhenItIsOpenAlready()
    {
        var cut = RenderFolders();
        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        Assert.Equal("true", cut.Tb().Grid("Folders").Row("Docs").GetAttribute("aria-expanded"));
        Assert.Single(Reported);
    }

    [Fact]
    public async Task ExpandAsync_ShouldThrow_WhenTheRowHasNoChildren()
    {
        var cut = RenderFolders();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Grid("Folders").ExpandAsync("Music"));

        Assert.Contains("no child rows", error.Message);
    }

    [Fact]
    public void ExpandedParents_ShouldOpenAndCloseTheRows_WhenTheParentSetsThem()
    {
        var cut = RenderFolders(parameters => parameters.Add(grid => grid.ExpandedParents, [Folders[1]]));
        Assert.Equal(["1 Docs", "1 Photos", "2 Trips", "1 Music"], Lines(cut));

        cut.Render(parameters => parameters.Add(grid => grid.ExpandedParents, []));

        Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut));
        Assert.Empty(Reported);
    }

    [Fact]
    public async Task Sort_ShouldOrderRowsAmongTheirSiblings_WhenAColumnIsSorted()
    {
        var cut = RenderFolders(parameters => parameters.Add(grid => grid.ExpandedParents, [Folders[0], Folders[1]]));

        await cut.Tb().Grid("Folders").SortAsync("Size", descending: true);

        Assert.Equal(["1 Photos", "2 Trips", "1 Docs", "2 Plans", "2 Notes", "1 Music"], Lines(cut));
    }

    [Fact]
    public async Task Filter_ShouldShowMatchesWithTheRowsAboveThemOpened_WhenFiltered()
    {
        var cut = RenderFolders();

        await cut.Tb().Grid("Folders").FilterAsync("Name", "dr");

        Assert.Equal(["1 Docs", "2 Plans", "3 Draft"], Lines(cut));
        Assert.Equal("1", cut.Tb().Grid("Folders").Row("Plans").GetAttribute("aria-setsize"));
        Assert.Null(cut.Tb().Grid("Folders").Row("Draft").GetAttribute("aria-expanded"));
        Assert.Equal("Sum 1", cut.Tb().Grid("Folders").Total("Size"));
        Assert.Empty(Reported);
    }

    [Fact]
    public async Task Toggle_ShouldCloseARowOnlyUntilTheFiltersChange_WhenFiltersApply()
    {
        var cut = RenderFolders();
        var grid = cut.Tb().Grid("Folders");
        await grid.FilterAsync("Name", "dr");

        await grid.CollapseAsync("Docs");
        Assert.Equal(["1 Docs"], Lines(cut));
        await grid.FilterAsync("Name", "");
        Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut));
        await grid.FilterAsync("Name", "dr");

        Assert.Equal(["1 Docs", "2 Plans", "3 Draft"], Lines(cut));
        Assert.Empty(Reported);
    }

    [Fact]
    public void Totals_ShouldCountEveryRowAtEveryLevel_WhenRowsAreClosed()
    {
        var cut = RenderFolders();

        Assert.Equal("Sum 54", cut.Tb().Grid("Folders").Total("Size"));
    }

    [Fact]
    public void ScrollToAsync_ShouldOpenTheRowsAboveTheRow_WhenTheyAreClosed()
    {
        var cut = RenderFolders();

        _ = cut.InvokeAsync(() => cut.Instance.ScrollToAsync(Draft));

        cut.WaitForAssertion(() => Assert.Contains("3 Draft", Lines(cut)));
        Assert.Equal([1, 3], Ids(Reported.Single()));
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldWriteEveryRowInTreeOrder_WhenRowsAreClosed()
    {
        var cut = RenderFolders();

        var csv = await cut.InvokeAsync(() => cut.Instance.ExportCsvAsync());

        Assert.Equal(["Name,Size", "Docs,5", "Notes,3", "Plans,9", "Draft,1", "Photos,20", "Trips,12", "Music,4"],
            csv.TrimStart('﻿').TrimEnd().Split("\r\n"));
    }

    [Fact]
    public void Render_ShouldNotOfferGroupBy_WhenTheGridIsATree()
    {
        var cut = RenderFolders();

        Assert.Empty(cut.FindAll("[data-part=group-by]"));
    }

    [Theory]
    [InlineData("groups")]
    [InlineData("move")]
    [InlineData("pages")]
    public void Render_ShouldThrow_WhenATreeGridIsGroupedPagedOrItsRowsMove(string option)
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderFolders(parameters =>
        {
            if (option == "groups")
            {
                parameters.Add(grid => grid.Groups, ["Name"]);
            }
            else if (option == "move")
            {
                parameters.Add(grid => grid.OnRowMove, (SortableMove<Folder> _) => { });
            }
            else
            {
                parameters.Add(grid => grid.PageSize, 2);
            }
        }));

        Assert.Contains("ChildrenSelector", error.Message);
    }

    [Fact]
    public void State_ShouldLeaveTheRowsUnpaged_WhenItHasAPageSizeForATreeGrid()
    {
        var cut = RenderFolders(parameters => parameters.Add(grid => grid.State, new GridState { PageSize = 1 })
            .Add(grid => grid.ExpandedParents, [Folders[0]]));

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
        Assert.Empty(cut.FindAll("[data-part=pager]"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenATreeGridHasADataProvider()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbDataGrid<Folder>>(parameters => parameters
            .Add(grid => grid.ChildrenSelector, folder => folder.Children)
            .Add(grid => grid.DataProvider, (_, _) => Task.FromResult(new GridResult<Folder>([], 0)))));

        Assert.Contains("ChildrenSelector", error.Message);
    }

    [Fact]
    public void Render_ShouldNameTheKeyAndBothRows_WhenTwoChildRowsShareAKey()
    {
        Folder[] items = [new(1, "Docs", 5, [new(2, "Notes", 3), new(2, "Plans", 9)])];

        var error = Assert.Throws<InvalidOperationException>(() => RenderFolders(grid => grid
            .Add(folder => folder.RowLabel, folder => folder.Name), items));

        Assert.Contains("the same key (2): \"Notes\" and \"Plans\"", error.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenARowIsUnderItself()
    {
        List<Folder> children = [];
        var loop = new Folder(1, "Loop", 1, children);
        children.Add(loop);

        var error = Assert.Throws<InvalidOperationException>(() => RenderFolders(items: [loop]));

        Assert.Contains("under itself", error.Message);
    }
}
