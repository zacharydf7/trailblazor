using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDockManagerCloseGuardTests : BunitContext
{
    private static readonly DockLayout Hike = new(new DockSplit(new DockGroup("trails", "map"), new DockGroup("notes")));

    private readonly BunitJSModuleInterop _module;
    private readonly List<DockLayout> _changes = [];
    private int _asked;
    private Func<Task<bool>> _answer = () => Task.FromResult(false);
    private bool _modified;
    private bool _closable = true;

    public TbDockManagerCloseGuardTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/dock-manager.js");
        _module.SetupModule("attachDock", _ => true).Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData("mark")]
    [InlineData("key")]
    [InlineData("group menu")]
    [InlineData("tab menu")]
    [InlineData("driver")]
    public async Task Close_ShouldKeepThePanelItsContentAndTheFocus_WhenCanCloseAnswersNo(string route)
    {
        var cut = RenderDock(Hike);

        await CloseAsync(cut, route);

        Assert.Equal(1, _asked);
        Assert.Empty(_changes);
        Assert.Equal(["Trails", "Map", "Notes"], cut.Tb().Dock().Panels);
        Assert.Equal("Trail list", cut.Find("[data-panel=trails][data-part=panel]").TextContent.Trim());
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier.EndsWith("focus"));
    }

    [Theory]
    [InlineData("mark")]
    [InlineData("key")]
    [InlineData("group menu")]
    [InlineData("tab menu")]
    [InlineData("driver")]
    public async Task Close_ShouldCloseThePanel_WhenCanCloseAnswersYes(string route)
    {
        _answer = () => Task.FromResult(true);
        var cut = RenderDock(Hike);

        await CloseAsync(cut, route);

        Assert.Equal(1, _asked);
        Assert.Equal(["trails"], Assert.Single(_changes).Closed);
        Assert.Equal(["Map", "Notes"], cut.Tb().Dock().Panels);
    }

    [Fact]
    public async Task Close_ShouldAskOnce_WhenAnotherRequestArrivesWhileTheFirstIsBeingAnswered()
    {
        var answer = new TaskCompletionSource<bool>();
        _answer = () => answer.Task;
        var cut = RenderDock(Hike);
        var first = CloseAsync(cut, "mark");

        var second = CloseAsync(cut, "key");

        Assert.Equal(1, _asked);
        answer.SetResult(true);
        await Task.WhenAll(first, second);
        Assert.Equal(["trails"], Assert.Single(_changes).Closed);
    }

    [Fact]
    public async Task Close_ShouldChangeNothing_WhenTheAppClosedThePanelWhileItWasBeingAsked()
    {
        var answer = new TaskCompletionSource<bool>();
        _answer = () => answer.Task;
        var cut = RenderDock(Hike);
        var asked = CloseAsync(cut, "mark");

        cut.Render(parameters => parameters.Add(dock => dock.Layout, Hike.Close("trails")));
        answer.SetResult(true);
        await asked;

        Assert.Empty(_changes);
        Assert.Equal(["Map", "Notes"], cut.Tb().Dock().Panels);
    }

    [Fact]
    public void Layout_ShouldCloseThePanelWithoutAsking_WhenTheAppClosesIt()
    {
        var cut = RenderDock(Hike);

        cut.Render(parameters => parameters.Add(dock => dock.Layout, Hike.Close("trails")));

        Assert.Equal(0, _asked);
        Assert.Equal(["Map", "Notes"], cut.Tb().Dock().Panels);
    }

    [Theory]
    [InlineData(true, "Unsaved changes|Press Delete to close")]
    [InlineData(false, "Unsaved changes")]
    public void Render_ShouldMarkTheTabAndTellScreenReaders_WhenItsPanelIsModified(bool closable, string hints)
    {
        (_modified, _closable) = (true, closable);
        var cut = RenderDock(Hike);

        var trails = Tab(cut, "Trails");
        Assert.True(trails.HasAttribute("data-modified"));
        Assert.Equal("true", trails.QuerySelector("[data-part=modified]")!.GetAttribute("aria-hidden"));
        Assert.Equal(hints, string.Join('|', trails.GetAttribute("aria-describedby")!.Split(' ')
            .Select(id => cut.Find($"[id='{id}']").TextContent)));
        // The other panels aren't marked.
        var map = Tab(cut, "Map");
        Assert.False(map.HasAttribute("data-modified"));
        Assert.Null(map.QuerySelector("[data-part=modified]"));
        Assert.Equal("Press Delete to close", cut.Find($"[id='{map.GetAttribute("aria-describedby")}']").TextContent);
    }

    [Fact]
    public void Render_ShouldMarkTheStripButtonAndTellScreenReaders_WhenAnAutoHiddenPanelIsModified()
    {
        _modified = true;
        var cut = RenderDock(Hike.AutoHide("trails", DockPosition.Start));

        var button = cut.Find("[data-part=strip-button][data-panel=trails]");
        Assert.True(button.HasAttribute("data-modified"));
        Assert.NotNull(button.QuerySelector("[data-part=modified]"));
        Assert.Equal("Unsaved changes", cut.Find($"[id='{button.GetAttribute("aria-describedby")}']").TextContent);
    }

    [Fact]
    public void Labels_ShouldWordTheUnsavedChangesHint_WhenGiven()
    {
        _modified = true;
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.Labels, new DockLabels { Modified = "Not saved yet" })
            .Add(dock => dock.ChildContent, Panels()));

        var hint = Tab(cut, "Trails").GetAttribute("aria-describedby")!.Split(' ')[0];
        Assert.Equal("Not saved yet", cut.Find($"[id='{hint}']").TextContent);
    }

    // Each way a user closes the Trails panel. Delete, Backspace and a middle click all reach the dock through the
    // script's one call.
    private async Task CloseAsync(IRenderedComponent<TbDockManager> cut, string route)
    {
        var dock = cut.Tb().Dock();
        switch (route)
        {
            case "mark":
                await Tab(cut, "Trails").QuerySelector("[data-part=close]")!.ClickAsync();
                break;
            case "key":
                var report = ((DotNetObjectReference<DockReport>)_module.Invocations["attachDock"].Single()
                    .Arguments[1]!).Value;
                await cut.InvokeAsync(() => report.OnClose("trails", false));
                break;
            case "group menu":
                await cut.Find("[data-part=group-menu] [data-move=close]").ClickAsync();
                break;
            case "tab menu":
                await dock.ChooseFromTabMenuAsync("Trails", "Close");
                break;
            default:
                await dock.CloseAsync("Trails");
                break;
        }
    }

    private IRenderedComponent<TbDockManager> RenderDock(DockLayout layout) =>
        Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, layout)
            .Add(dock => dock.LayoutChanged, _changes.Add).Add(dock => dock.ChildContent, Panels()));

    // Trails asks before it closes and takes the test's Modified and Closable; the others are plain.
    private RenderFragment Panels() => builder =>
    {
        foreach (var (id, title, text) in new[]
        {
            ("trails", "Trails", "Trail list"), ("map", "Map", "Map view"), ("notes", "Notes", "Packing notes"),
        })
        {
            builder.OpenComponent<TbDockPanel>(0);
            builder.SetKey(id);
            builder.AddComponentParameter(1, nameof(TbDockPanel.Id), id);
            builder.AddComponentParameter(2, nameof(TbDockPanel.Title), title);
            if (id == "trails")
            {
                builder.AddComponentParameter(3, nameof(TbDockPanel.CanClose), (Func<Task<bool>>)AskAsync);
                builder.AddComponentParameter(4, nameof(TbDockPanel.Modified), _modified);
                builder.AddComponentParameter(5, nameof(TbDockPanel.Closable), _closable);
            }

            builder.AddComponentParameter(6, nameof(TbDockPanel.ChildContent),
                (RenderFragment)(content => content.AddContent(0, text)));
            builder.CloseComponent();
        }
    };

    private Task<bool> AskAsync()
    {
        _asked++;
        return _answer();
    }

    private static AngleSharp.Dom.IElement Tab(IRenderedComponent<TbDockManager> cut, string title) =>
        cut.FindAll("[role=tab]").Single(tab => tab.TextContent.Trim() == title);
}
