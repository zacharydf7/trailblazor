using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A tree grid whose top rows come from a DataProvider and whose rows load their children as they open.
public class TbDataGridProviderTreeTests : BunitContext
{
    public TbDataGridProviderTreeTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Folder(int Id, string Name);

    private static readonly Folder[] Top = [new(1, "Docs"), new(5, "Photos"), new(7, "Music")];

    private static readonly Dictionary<int, Folder[]> Children = new()
    {
        [1] = [new(2, "Notes"), new(3, "Plans")],
        [5] = [],
        [7] = [],
    };

    private readonly List<GridRequest> _requests = [];

    // The provider pages the top rows in the order it keeps them, whatever the grid's sort.
    private Task<GridResult<Folder>> ProvideAsync(GridRequest request, CancellationToken _)
    {
        _requests.Add(request);
        var page = Top.Skip(request.StartIndex).Take(request.Count ?? Top.Length);
        return Task.FromResult(new GridResult<Folder>([.. page], Top.Length));
    }

    private IRenderedComponent<TbDataGrid<Folder>> RenderFolders(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Folder>>>? configure = null) =>
        Render<TbDataGrid<Folder>>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, ProvideAsync).Add(grid => grid.AriaLabel, "Folders")
                .Add(grid => grid.KeySelector, folder => folder.Id)
                .Add(grid => grid.LoadChildren, (folder, _) => Task.FromResult<IEnumerable<Folder>>(Children[folder.Id]))
                .AddChildContent<TbColumn<Folder>>(column => column.Add(name => name.Field, folder => folder.Name)
                    .Add(name => name.Sortable, true).Add(name => name.Filterable, true));
            configure?.Invoke(parameters);
        });

    private static List<string> Lines(IRenderedComponent<TbDataGrid<Folder>> cut) =>
        [.. cut.FindAll("tbody tr[data-part=row]").Select(row =>
            $"{row.GetAttribute("aria-level")} {row.QuerySelector("td[data-column=Name]")!.TextContent.Trim()}")];

    [Fact]
    public async Task ExpandAsync_ShouldLoadTheChildrenUnderAProvidersRow_WhenTheRowOpens()
    {
        var cut = RenderFolders();
        cut.WaitForAssertion(() => Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut)));

        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
        Assert.Equal("treegrid", cut.Find("table").GetAttribute("role"));
    }

    [Fact]
    public async Task PageSize_ShouldPageTheTopRows_WhenTheyComeFromAProvider()
    {
        var cut = RenderFolders(parameters => parameters.Add(grid => grid.PageSize, 2));
        cut.WaitForAssertion(() => Assert.Equal(["1 Docs", "1 Photos"], Lines(cut)));

        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos"], Lines(cut));
        Assert.All(_requests, request => Assert.Equal(2, request.Count));
    }

    [Fact]
    public async Task Sort_ShouldOrderTheChildrenAndLeaveTheTopRowsToTheProvider_WhenAColumnIsSorted()
    {
        var cut = RenderFolders();
        var grid = cut.Tb().Grid("Folders");
        await grid.ExpandAsync("Docs");

        await grid.SortAsync("Name", descending: true);

        Assert.Equal(["1 Docs", "2 Plans", "2 Notes", "1 Photos", "1 Music"], Lines(cut));
        Assert.Equal(SortDirection.Descending, Assert.Single(_requests[^1].Sorts).Direction);
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldListEachTopRowWithTheChildrenLoadedUnderIt_WhenCalled()
    {
        var cut = RenderFolders();
        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        var csv = await cut.Instance.ExportCsvAsync();

        Assert.Equal(["Name", "Docs", "Notes", "Plans", "Photos", "Music"],
            csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public async Task GoToPageAsync_ShouldAskForTheNextTopRows_WhenTheSecondPageShows()
    {
        var cut = RenderFolders(parameters => parameters.Add(grid => grid.PageSize, 2));
        var grid = cut.Tb().Grid("Folders");
        await grid.ExpandAsync("Docs");

        await grid.GoToPageAsync(2);

        Assert.Equal(["1 Music"], Lines(cut));
        Assert.Equal(2, _requests[^1].StartIndex);
    }

    [Fact]
    public void State_ShouldPageTheTopRows_WhenItHasAPageSizeForAProvidersTree()
    {
        var cut = RenderFolders(parameters => parameters.Add(grid => grid.State, new GridState { PageSize = 2 }));

        cut.WaitForAssertion(() => Assert.Equal(["1 Docs", "1 Photos"], Lines(cut)));
        Assert.Equal(2, _requests[^1].Count);
    }

    [Fact]
    public async Task Filter_ShouldGoToTheProviderAndKeepTheLoadedChildren_WhenAFilterApplies()
    {
        var cut = RenderFolders();
        var grid = cut.Tb().Grid("Folders");
        await grid.ExpandAsync("Docs");

        await grid.FilterAsync("Name", "o");

        Assert.Equal("Name", Assert.Single(_requests[^1].Filters).Column);
        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
        await grid.CollapseAsync("Docs");
        Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut));
    }

    [Fact]
    public void ExpandedParents_ShouldShowTheLoadedChildren_WhenTheAppOpensARowAgain()
    {
        var cut = RenderFolders(parameters => parameters.Add(grid => grid.ExpandedParents, [Top[0]]));
        cut.WaitForAssertion(() => Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut)));
        cut.Render(parameters => parameters.Add(grid => grid.ExpandedParents, []));
        Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut));

        cut.Render(parameters => parameters.Add(grid => grid.ExpandedParents, [Top[0]]));

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAProvidersTreeVirtualizes()
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderFolders(parameters => parameters
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "20rem")));

        Assert.Contains("can't virtualize", error.Message);
    }
}
