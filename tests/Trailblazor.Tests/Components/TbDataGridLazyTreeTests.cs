using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A tree grid whose rows load their children the first time they open.
public class TbDataGridLazyTreeTests : BunitContext
{
    public TbDataGridLazyTreeTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Folder(int Id, string Name);

    private static readonly Folder Docs = new(1, "Docs");
    private static readonly Folder Photos = new(5, "Photos");
    private static readonly Folder Music = new(7, "Music");

    private static readonly Dictionary<int, Folder[]> Children = new()
    {
        [1] = [new(2, "Notes"), new(3, "Plans")],
        [5] = [],
    };

    private readonly List<Folder> _asked = [];

    private IRenderedComponent<TbDataGrid<Folder>> RenderFolders(
        Func<Folder, CancellationToken, Task<IEnumerable<Folder>>>? load = null,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Folder>>>? configure = null) =>
        Render<TbDataGrid<Folder>>(parameters =>
        {
            parameters.Add(grid => grid.Items, [Docs, Photos, Music]).Add(grid => grid.AriaLabel, "Folders")
                .Add(grid => grid.KeySelector, folder => folder.Id)
                .Add(grid => grid.LoadChildren, load ?? ((folder, _) =>
                {
                    _asked.Add(folder);
                    return Task.FromResult<IEnumerable<Folder>>(Children[folder.Id]);
                }))
                .Add(grid => grid.HasChildren, folder => folder != Music)
                .AddChildContent<TbColumn<Folder>>(column => column.Add(name => name.Field, folder => folder.Name)
                    .Add(name => name.Filterable, true));
            configure?.Invoke(parameters);
        });

    private static List<string> Lines(IRenderedComponent<TbDataGrid<Folder>> cut) =>
        [.. cut.FindAll("tbody tr[data-part=row]").Select(row =>
            $"{row.GetAttribute("aria-level")} {row.QuerySelector("td[data-column=Name]")!.TextContent.Trim()}")];

    private static AngleSharp.Dom.IElement Row(IRenderedComponent<TbDataGrid<Folder>> cut, int id) =>
        cut.Find($"tbody tr[data-key='{id}']");

    private static Task ToggleAsync(IRenderedComponent<TbDataGrid<Folder>> cut, int id) =>
        Row(cut, id).QuerySelector("[data-part=tree-toggle]")!.ClickAsync(new());

    [Fact]
    public async Task Toggle_ShouldLoadAndShowTheChildren_WhenAnUnloadedRowOpens()
    {
        var cut = RenderFolders();

        await ToggleAsync(cut, 1);

        Assert.Equal([Docs], _asked);
        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
        Assert.Equal("true", Row(cut, 1).GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Render_ShouldShowAButtonOnlyOnRowsThatMayHaveChildren_WhenNoneHaveLoaded()
    {
        var cut = RenderFolders();

        Assert.Equal(("false", "false", null), (Row(cut, 1).GetAttribute("aria-expanded"),
            Row(cut, 5).GetAttribute("aria-expanded"), Row(cut, 7).GetAttribute("aria-expanded")));
        Assert.Empty(Row(cut, 7).QuerySelectorAll("[data-part=tree-toggle]"));
    }

    [Fact]
    public async Task Toggle_ShouldDropTheButton_WhenTheRowTurnsOutToHaveNoChildren()
    {
        var cut = RenderFolders();

        await ToggleAsync(cut, 5);

        Assert.Null(Row(cut, 5).GetAttribute("aria-expanded"));
        Assert.Empty(Row(cut, 5).QuerySelectorAll("[data-part=tree-toggle]"));
    }

    [Fact]
    public async Task Toggle_ShouldKeepTheLoadedChildren_WhenTheRowOpensAgain()
    {
        var cut = RenderFolders();
        await ToggleAsync(cut, 1);
        await ToggleAsync(cut, 1);

        await ToggleAsync(cut, 1);

        Assert.Equal([Docs], _asked);
        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
    }

    [Fact]
    public async Task Toggle_ShouldCancelTheLoadAndMarkTheRowBusyMeanwhile_WhenTheRowClosesFirst()
    {
        var waiting = new TaskCompletionSource<IEnumerable<Folder>>();
        CancellationToken token = default;
        var cut = RenderFolders((_, cancel) =>
        {
            token = cancel;
            return waiting.Task;
        });
        var opening = ToggleAsync(cut, 1);
        Assert.Equal("true", Row(cut, 1).GetAttribute("aria-busy"));
        Assert.NotNull(Row(cut, 1).QuerySelector("[data-part=tree-toggle] .tb-data-grid__tree-spinner"));

        await ToggleAsync(cut, 1);
        waiting.SetResult(Children[1]);
        await opening;

        Assert.True(token.IsCancellationRequested);
        Assert.Equal(["1 Docs", "1 Photos", "1 Music"], Lines(cut));
        Assert.Null(Row(cut, 1).GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task Toggle_ShouldCloseTheRowAndSaySo_WhenTheLoadFails()
    {
        var host = Render<AnnouncerHost>();
        var failures = new List<Exception>();
        var tries = 0;
        var cut = RenderFolders((folder, _) => ++tries == 1
                ? Task.FromException<IEnumerable<Folder>>(new InvalidOperationException("offline"))
                : Task.FromResult<IEnumerable<Folder>>(Children[folder.Id]),
            parameters => parameters.Add(grid => grid.OnLoadFailed, (Exception failure) => failures.Add(failure)));

        await ToggleAsync(cut, 1);

        Assert.Equal("offline", Assert.Single(failures).Message);
        Assert.Equal("false", Row(cut, 1).GetAttribute("aria-expanded"));
        host.WaitForAssertion(() => Assert.NotEmpty(host.Find("[role=status]").TextContent.Trim()));
        await ToggleAsync(cut, 1);
        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
    }

    [Fact]
    public void ExpandedParents_ShouldLoadTheChildren_WhenTheAppOpensAnUnloadedRow()
    {
        var cut = RenderFolders(configure: parameters => parameters.Add(grid => grid.ExpandedParents, [Docs]));

        cut.WaitForAssertion(() => Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut)));
        Assert.Equal([Docs], _asked);
    }

    [Fact]
    public async Task Toggle_ShouldStartANewLoad_WhenTheRowOpensAgainBeforeItsCanceledLoadEnds()
    {
        List<TaskCompletionSource<IEnumerable<Folder>>> loads = [];
        var cut = RenderFolders((_, _) =>
        {
            loads.Add(new());
            return loads[^1].Task;
        });
        var first = ToggleAsync(cut, 1);
        await ToggleAsync(cut, 1);
        var again = ToggleAsync(cut, 1);

        // A loader may finish a canceled load anyway; its children are dropped and the row loads again.
        loads[0].SetResult([new(9, "Stale")]);
        await first;
        cut.WaitForAssertion(() => Assert.Equal(2, loads.Count));
        loads[1].SetResult(Children[1]);
        await again;

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
    }

    [Fact]
    public async Task Filter_ShouldLeaveAnUnloadedRowClosedUntilItOpens_WhenFiltersApply()
    {
        var cut = RenderFolders();
        var grid = cut.Tb().Grid("Folders");
        await grid.FilterAsync("Name", "o");
        Assert.Equal("false", Row(cut, 1).GetAttribute("aria-expanded"));

        await ToggleAsync(cut, 1);

        Assert.Equal(["1 Docs", "2 Notes", "1 Photos"], Lines(cut));
    }

    [Fact]
    public async Task Toggle_ShouldCloseTheRow_WhenTheLoadFailsWhileFiltersApply()
    {
        var cut = RenderFolders((_, _) => Task.FromException<IEnumerable<Folder>>(new InvalidOperationException()));
        await cut.Tb().Grid("Folders").FilterAsync("Name", "o");

        await ToggleAsync(cut, 1);

        Assert.Equal("false", Row(cut, 1).GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task ExpandAsync_ShouldReturnOnceTheChildrenHaveLoaded_WhenTheyLoadSlowly()
    {
        var cut = RenderFolders(async (folder, cancel) =>
        {
            await Task.Delay(50, cancel);
            return Children[folder.Id];
        });

        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
    }

    [Fact]
    public void ExpandedParents_ShouldCancelTheLoad_WhenTheAppClosesALoadingRow()
    {
        CancellationToken token = default;
        var cut = RenderFolders((_, cancel) =>
        {
            token = cancel;
            return new TaskCompletionSource<IEnumerable<Folder>>().Task;
        }, parameters => parameters.Add(grid => grid.ExpandedParents, [Docs]));

        cut.Render(parameters => parameters.Add(grid => grid.ExpandedParents, []));

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public async Task ExpandAsync_ShouldWaitForTheChildren_WhenTheAppDrawsTheGridAgainFirst()
    {
        IRenderedComponent<TbDataGrid<Folder>>? cut = null;
        cut = RenderFolders(async (folder, cancel) =>
        {
            _asked.Add(folder);
            await Task.Delay(50, cancel);
            return Children[folder.Id];
        }, parameters => parameters.Add(grid => grid.ExpandedParentsChanged, async (IReadOnlyCollection<Folder> open) =>
        {
            // As @bind-ExpandedParents does, the new rows come back to the grid before the toggle goes on.
            await Task.Yield();
            cut!.Render(again => again.Add(grid => grid.ExpandedParents, open));
        }));

        await cut.Tb().Grid("Folders").ExpandAsync("Docs");

        Assert.Equal(["1 Docs", "2 Notes", "2 Plans", "1 Photos", "1 Music"], Lines(cut));
        Assert.Equal([Docs], _asked);
    }

    [Fact]
    public async Task Dispose_ShouldCancelTheLoad_WhenTheGridGoesWhileChildrenLoad()
    {
        CancellationToken token = default;
        var cut = RenderFolders((_, cancel) =>
        {
            token = cancel;
            return new TaskCompletionSource<IEnumerable<Folder>>().Task;
        });
        _ = ToggleAsync(cut, 1);

        await DisposeComponentsAsync();

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public async Task Render_ShouldShowTheChildrenOfRowsTheAppOpens_WhenRenderedStatically()
    {
        await using var renderer = new HtmlRenderer(Services, NullLoggerFactory.Instance);
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<TbColumn<Folder>>(0);
            builder.AddComponentParameter(1, nameof(TbColumn<Folder>.Field),
                (System.Linq.Expressions.Expression<Func<Folder, object?>>)(folder => folder.Name));
            builder.CloseComponent();
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(TbDataGrid<Folder>.Items)] = new[] { Docs, Photos },
            [nameof(TbDataGrid<Folder>.AriaLabel)] = "Folders",
            [nameof(TbDataGrid<Folder>.KeySelector)] = (Func<Folder, object>)(folder => folder.Id),
            [nameof(TbDataGrid<Folder>.ExpandedParents)] = new[] { Docs },
            [nameof(TbDataGrid<Folder>.LoadChildren)] = (Func<Folder, CancellationToken, Task<IEnumerable<Folder>>>)(
                async (folder, cancel) =>
                {
                    await Task.Delay(20, cancel);
                    return Children[folder.Id];
                }),
            [nameof(TbDataGrid<Folder>.ChildContent)] = columns,
        });

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<TbDataGrid<Folder>>(parameters)).ToHtmlString());

        Assert.Contains("Plans", html);
    }

    [Fact]
    public void Render_ShouldThrow_WhenBothChildrenSelectorAndLoadChildrenAreSet()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => RenderFolders(configure: parameters =>
            parameters.Add(grid => grid.ChildrenSelector, _ => null)));

        Assert.Contains("not both", failure.Message);
    }
}
