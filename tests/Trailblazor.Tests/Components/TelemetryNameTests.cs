using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// What each control with a TelemetryName counts, besides the button's, which TelemetryTests covers.
public class TelemetryNameTests : BunitContext
{
    private readonly MetricCollector<long> _shown;
    private readonly MetricCollector<long> _used;

    public TelemetryNameTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var meters = Services.GetRequiredService<IMeterFactory>();
        _shown = new(meters, "Trailblazor", "trailblazor.control.shown");
        _used = new(meters, "Trailblazor", "trailblazor.control.used");
    }

    private sealed record Trail(int Id, string Name);

    private sealed record Place(int Id, string Name, IReadOnlyList<Place>? Children = null);

    [Fact]
    public void Link_ShouldCountItsClicks_WhenItHasATelemetryName()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.TelemetryName, "trail-guide")
            .Add(link => link.OnClick, () => { }).AddChildContent("Trail guide"));

        cut.Find("button").Click();

        Assert.Equal(["trail-guide"], Names(_shown));
        Assert.Equal([("trail-guide", "TbLink", null)], Uses());
    }

    [Fact]
    public async Task Menu_ShouldCountItsOpensAndItsItemsChosen_WhenTheyHaveTelemetryNames()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.TelemetryName, "row-actions")
            .AddChildContent<TbMenuItem>(item => item.Add(menuItem => menuItem.TelemetryName, "export-csv")
                .AddChildContent("Export")));
        var events = ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createMenu"].Single()
            .Arguments[3]!).Value;

        cut.Find("[data-part=trigger]").Click();
        await cut.InvokeAsync(events.OnOpened);
        cut.Find("[role=menuitem]").Click();

        Assert.Equal(["row-actions"], Names(_shown));
        Assert.Equal([("row-actions", "TbMenu", null), ("export-csv", "TbMenuItem", null)], Uses());
    }

    [Fact]
    public void Menu_ShouldMakeNoCallsBack_WhenItHasNoTelemetryName()
    {
        Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Export")));

        Assert.Null(JSInterop.Invocations["createMenu"].Single().Arguments[3]);
    }

    [Fact]
    public void Tab_ShouldCountItsHeaderShownAndEachSwitchToIt_WhenItHasATelemetryName()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTab>(0);
            builder.AddAttribute(1, nameof(TbTab.Text), "General");
            builder.CloseComponent();
            builder.OpenComponent<TbTab>(2);
            builder.AddAttribute(3, nameof(TbTab.Text), "Billing");
            builder.AddAttribute(4, nameof(TbTab.TelemetryName), "billing-tab");
            builder.CloseComponent();
        })));

        cut.FindAll("[role=tab]")[1].Click();
        cut.FindAll("[role=tab]")[1].Click();

        Assert.Equal(["billing-tab"], Names(_shown));
        Assert.Equal([("billing-tab", "TbTab", null)], Uses());
    }

    [Fact]
    public void Switch_ShouldCountEachChangeButNotTheValue_WhenItHasATelemetryName()
    {
        var cut = Render<TbSwitch>(parameters => parameters.Add(toggle => toggle.Label, "Email alerts")
            .Add(toggle => toggle.TelemetryName, "email-alerts"));

        cut.Find("input").Change(true);

        Assert.Equal(["email-alerts"], Names(_shown));
        var use = Assert.Single(_used.GetMeasurementSnapshot());
        Assert.Equal(["trailblazor.component", "trailblazor.telemetry_name"], use.Tags.Keys.Order());
    }

    [Fact]
    public void ToggleItem_ShouldCountItsPresses_WhenItHasATelemetryName()
    {
        var cut = Render<TbToggleGroup<string>>(parameters => parameters.Add(group => group.AriaLabel, "View")
            .AddChildContent<TbToggleItem<string>>(item => item.Add(toggle => toggle.Value, "list")
                .Add(toggle => toggle.TelemetryName, "view-as-list").AddChildContent("List")));

        cut.Find("button").Click();

        Assert.Equal(["view-as-list"], Names(_shown));
        Assert.Equal([("view-as-list", "TbToggleItem", null)], Uses());
    }

    [Fact]
    public void Chip_ShouldCountAClickAndARemovalApart_WhenItHasATelemetryName()
    {
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Open trails")
            .Add(chip => chip.TelemetryName, "filter-open").Add(chip => chip.OnClick, () => { })
            .Add(chip => chip.OnClose, () => { }));

        cut.Find("[data-part=action]").Click();
        cut.Find("[data-part=remove]").Click();

        Assert.Equal([("filter-open", "TbChip", null), ("filter-open", "TbChip", "remove")], Uses());
    }

    [Fact]
    public void Select_ShouldCountItselfShownAndUsedAndAnOptionChosen_WhenTheyHaveTelemetryNames()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Size")
            .Add(select => select.TelemetryName, "trail-size")
            .AddChildContent<TbOption<string>>(option => option.Add(choice => choice.Value, "L")
                .Add(choice => choice.TelemetryName, "size-large").AddChildContent("Large")));

        cut.Find("[role=option]").Click();

        Assert.Equal(["trail-size"], Names(_shown));
        Assert.Equal([("trail-size", "TbSelect", null), ("size-large", "TbOption", null)], Uses());
    }

    [Fact]
    public void Option_ShouldCountNothing_WhenItsListBoxRefusesTheChoice()
    {
        var cut = Render<TbListBox<string>>(parameters => parameters.Add(listbox => listbox.Label, "Size")
            .Add(listbox => listbox.ReadOnly, true)
            .AddChildContent<TbOption<string>>(option => option.Add(choice => choice.Value, "L")
                .Add(choice => choice.TelemetryName, "size-large").AddChildContent("Large")));

        cut.Find("[role=option]").Click();

        Assert.Empty(Uses());
    }

    [Fact]
    public async Task DataGrid_ShouldCountWhatTheUserDoesButNotWhatCodeDoes_WhenItHasATelemetryName()
    {
        Trail[] trails = [new(1, "Pine Hollow"), new(2, "Eagle Ridge"), new(3, "Cedar Flats")];
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.TelemetryName, "trails").Add(grid => grid.PageSize, 1)
            .Add(grid => grid.PageSizes, [1, 2]).Add(grid => grid.SelectionMode, SelectionMode.Multiple)
            .AddChildContent<TbColumn<Trail>>(column => column.Add(name => name.Field, trail => trail.Name)
                .Add(name => name.Sortable, true)));

        cut.Find("th button").Click();
        cut.Find("nav [data-part=next]").Click();
        cut.Find("[role=option]:last-child").Click();
        cut.Find("tr[data-part=row] input").Change(true);
        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(trails[1]));

        Assert.Equal(["trails"], Names(_shown));
        Assert.Equal(["sort", "page", "page_size", "select"], Uses().Where(use => Equals(use.Name, "trails"))
            .Select(use => use.Action));
    }

    [Fact]
    public async Task TreeView_ShouldCountWhatTheUserDoesButNotWhatCodeDoes_WhenItHasATelemetryName()
    {
        Place[] places = [new(1, "North", [new(2, "Ridge")]), new(4, "South", [new(5, "Lake")])];
        var cut = Render<TbTreeView<Place, int>>(parameters => parameters.Add(tree => tree.Items, places)
            .Add(tree => tree.ChildrenSelector, place => place.Children).Add(tree => tree.KeySelector, place => place.Id)
            .Add(tree => tree.AriaLabel, "Places").Add(tree => tree.TelemetryName, "places")
            .Add(tree => tree.Checkable, true).Add(tree => tree.OnActivate, (Place _) => { })
            .Add(tree => tree.OnDrop, (TreeDrop<Place> _) => { }));
        var events = ((DotNetObjectReference<TreeViewEvents>)JSInterop.Invocations["createTreeView"].Single()
            .Arguments[1]!).Value;

        TreeRow(cut, "1").QuerySelector("[data-part=toggle]")!.Click();
        await cut.InvokeAsync(() => events.OnKey("1", "ArrowLeft", false, false));
        await cut.InvokeAsync(() => events.OnKey("1", "ArrowLeft", false, false));
        TreeRow(cut, "4").Click();
        TreeRow(cut, "4").Click();
        await cut.InvokeAsync(() => events.OnKey("4", "*", false, false));
        await cut.InvokeAsync(() => events.OnKey("4", "*", false, false));
        TreeRow(cut, "4").QuerySelector("[data-part=check]")!.Click();
        TreeRow(cut, "4").DoubleClick();
        await cut.InvokeAsync(() => events.OnDrop(["4"], "1", TreeDropPosition.Before));
        await cut.InvokeAsync(() => cut.Instance.ExpandAsync(4));

        Assert.Equal(["places"], Names(_shown));
        Assert.Equal(["expand", "collapse", "select", "expand", "check", "activate", "move"],
            Uses().Select(use => use.Action));
    }

    private static List<object?> Names(MetricCollector<long> collector) =>
        [.. collector.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"])];

    private List<(object? Name, object? Component, object? Action)> Uses() =>
    [
        .. _used.GetMeasurementSnapshot().Select(measurement => (measurement.Tags["trailblazor.telemetry_name"],
            measurement.Tags["trailblazor.component"], measurement.Tags.GetValueOrDefault("trailblazor.action"))),
    ];

    private static AngleSharp.Dom.IElement TreeRow(IRenderedComponent<TbTreeView<Place, int>> cut, string key) =>
        cut.Find($"[role=treeitem][data-key='{key}']");

    protected override void Dispose(bool disposing)
    {
        _shown.Dispose();
        _used.Dispose();
        base.Dispose(disposing);
    }
}
