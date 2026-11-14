using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
    public void TableOfContents_ShouldCountTheLinksFollowed_WhenItHasATelemetryName()
    {
        var cut = Render<TbTableOfContents>(parameters => parameters.Add(contents => contents.TelemetryName,
                "guide-contents")
            .AddChildContent<TbTableOfContentsItem>(item => item.Add(link => link.TargetId, "water")
                .Add(link => link.Text, "Water")));

        cut.Find("a").Click();

        Assert.Equal(["guide-contents"], Names(_shown));
        Assert.Equal([("guide-contents", "TbTableOfContents", null)], Uses());
    }

    [Fact]
    public void NavMenu_ShouldCountTheLinksFollowed_WhenItHasATelemetryName()
    {
        var cut = Render<TbNavMenu>(parameters => parameters.Add(menu => menu.TelemetryName, "main-menu")
            .AddChildContent<TbNavLink>(link => link.Add(l => l.Href, "trails").AddChildContent("Trails")));

        cut.Find("a").Click();

        Assert.Equal(["main-menu"], Names(_shown));
        Assert.Equal([("main-menu", "TbNavMenu", null)], Uses());
    }

    [Fact]
    public void Card_ShouldCountItsLinkFollowed_WhenItHasATelemetryName()
    {
        var cut = Render<TbCard>(parameters => parameters.Add(card => card.TelemetryName, "trail-card")
            .Add(card => card.Title, "Ridge loop").Add(card => card.Href, "trails/ridge-loop"));

        cut.Find("a").Click();

        Assert.Equal(["trail-card"], Names(_shown));
        Assert.Equal([("trail-card", "TbCard", null)], Uses());
    }

    [Fact]
    public void Breadcrumbs_ShouldCountTheLinksFollowed_WhenTheTrailHasATelemetryName()
    {
        var cut = Render<TbBreadcrumbs>(parameters => parameters
            .Add(trail => trail.TelemetryName, "project-breadcrumbs")
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Href, "/").AddChildContent("Home"))
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Current, true)
                .AddChildContent("Ridge loop")));

        cut.Find("a").Click();

        Assert.Equal(["project-breadcrumbs"], Names(_shown));
        Assert.Equal([("project-breadcrumbs", "TbBreadcrumbs", null)], Uses());
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
        await cut.InvokeAsync(() => events.OnToggle(true));
        cut.Find("[role=menuitem]").Click();

        Assert.Equal(["row-actions"], Names(_shown));
        Assert.Equal([("row-actions", "TbMenu", null), ("export-csv", "TbMenuItem", null)], Uses());
    }

    [Fact]
    public async Task ContextMenu_ShouldCountItsOpensAndItsItemsChosen_WhenTheyHaveTelemetryNames()
    {
        var cut = Render<TbContextMenu>(parameters => parameters.Add(menu => menu.TelemetryName, "trail-actions")
            .Add(menu => menu.Items, _ => builder =>
            {
                builder.OpenComponent<TbMenuItem>(0);
                builder.AddAttribute(1, nameof(TbMenuItem.TelemetryName), "delete-trail");
                builder.AddAttribute(2, nameof(TbMenuItem.ChildContent), (RenderFragment)(text => text.AddContent(0,
                    "Delete")));
                builder.CloseComponent();
            }));
        var events = ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createContextMenu"].Single()
            .Arguments[2]!).Value;

        await cut.InvokeAsync(() => events.OnToggle(true));
        cut.Find("[role=menuitem]").Click();

        Assert.Equal(["trail-actions"], Names(_shown));
        Assert.Equal([("trail-actions", "TbContextMenu", null), ("delete-trail", "TbMenuItem", null)], Uses());
    }

    [Fact]
    public async Task Hotkey_ShouldCountEachPress_WhenItHasATelemetryName()
    {
        var cut = Render<TbHotkey>(parameters => parameters.Add(hotkey => hotkey.Keys, "Mod+S")
            .Add(hotkey => hotkey.TelemetryName, "save-shortcut"));

        await cut.Find("[data-part=hotkey]").TriggerEventAsync("ontbhotkey", EventArgs.Empty);

        Assert.Equal(["save-shortcut"], Names(_shown));
        Assert.Equal([("save-shortcut", "TbHotkey", null)], Uses());
    }

    [Fact]
    public async Task PropertyGrid_ShouldCountEachChange_WhenItHasATelemetryName()
    {
        var name = "Ridge loop";
        var cut = Render<TbPropertyGrid<object>>(parameters => parameters.Add(grid => grid.Label, "Trail")
            .Add(grid => grid.TelemetryName, "trail-settings")
            .Add(grid => grid.Fields, [new PropertyGridField
            {
                Name = "Name", Type = typeof(string), Get = () => name, Set = value => name = (string)value!,
            }]));

        await cut.Tb().TextField("Name").EnterAsync("Lake path");

        Assert.Equal(["trail-settings"], Names(_shown));
        Assert.Equal([("trail-settings", "TbPropertyGrid", "change")], Uses());
    }

    [Fact]
    public async Task Viewport_ShouldCountZoomsAndFitsButNotResizes_WhenItHasATelemetryName()
    {
        var cut = Render<TbViewport>(parameters => parameters.Add(viewport => viewport.Label, "Trail map")
            .Add(viewport => viewport.TelemetryName, "trail-map"));
        var report = ((DotNetObjectReference<ViewportReport>)JSInterop.Invocations["attachViewport"].Single()
            .Arguments[1]!).Value;

        await cut.Tb().Viewport().ZoomInAsync();
        await cut.InvokeAsync(() => report.OnZoom(2, "None", "keyboard"));
        await cut.InvokeAsync(() => report.OnZoom(1.5, "Page", "resize"));
        await cut.Tb().Viewport().FitAsync(ViewportFit.Width);
        await cut.Tb().Viewport().ActualSizeAsync();

        Assert.Equal(["trail-map"], Names(_shown));
        Assert.Equal([("trail-map", "TbViewport", "zoom"), ("trail-map", "TbViewport", "zoom"),
            ("trail-map", "TbViewport", "fit"), ("trail-map", "TbViewport", "zoom")], Uses());
        Assert.Equal(["pointer", "keyboard", null, "pointer"],
            _used.GetMeasurementSnapshot().Select(measurement => measurement.Tags.GetValueOrDefault("trailblazor.input")));
    }

    [Fact]
    public async Task QueryBuilder_ShouldCountEachKindOfChange_WhenItHasATelemetryName()
    {
        var cut = Render<TbQueryBuilder>(parameters => parameters.Add(builder => builder.Label, "Trail search")
            .Add(builder => builder.TelemetryName, "trail-search")
            .Add(builder => builder.Fields, [new QueryField("Name", "Name", QueryFieldKind.Text),
                new QueryField("Loop", "Loop", QueryFieldKind.YesNo)]));
        var search = cut.Tb().QueryBuilder("Trail search");

        await search.AddConditionAsync();
        await search.Condition(1).Select("Field").ChooseAsync("Loop");
        await search.AddGroupAsync();
        await search.RemoveConditionAsync(1);

        Assert.Equal(["trail-search"], Names(_shown));
        Assert.Equal([("trail-search", "TbQueryBuilder", "add_condition"), ("trail-search", "TbQueryBuilder", "change"),
            ("trail-search", "TbQueryBuilder", "add_group"), ("trail-search", "TbQueryBuilder", "remove")], Uses());
    }

    [Fact]
    public async Task SpeedDial_ShouldCountItsOpensAndItsActionsChosen_WhenTheyHaveTelemetryNames()
    {
        var cut = Render<TbSpeedDial>(parameters => parameters.Add(dial => dial.Label, "Add")
            .Add(dial => dial.TelemetryName, "new-item")
            .AddChildContent<TbMenuItem>(item => item.Add(action => action.TelemetryName, "new-trail")
                .AddChildContent("Trail")));
        var events = ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createMenu"].Single()
            .Arguments[3]!).Value;

        cut.Find("[data-part=trigger]").Click();
        await cut.InvokeAsync(() => events.OnToggle(true));
        cut.Find("[role=menuitem]").Click();

        Assert.Equal(["new-item"], Names(_shown));
        Assert.Equal([("new-item", "TbSpeedDial", null), ("new-trail", "TbMenuItem", null)], Uses());
    }

    [Fact]
    public async Task Popconfirm_ShouldCountEachAnswer_WhenItHasATelemetryName()
    {
        var cut = Render<TbPopconfirm>(parameters => parameters.Add(confirm => confirm.Label, "Delete")
            .Add(confirm => confirm.Title, "Delete Pine Hollow loop?")
            .Add(confirm => confirm.TelemetryName, "delete-trail").Add(confirm => confirm.OnConfirm, () => { }));
        var popover = cut.FindComponent<TbPopover>().Instance;

        await cut.Find("[data-part=confirm]").ClickAsync(new());
        await cut.InvokeAsync(() => popover.OnToggleAsync(true));
        await cut.InvokeAsync(() => popover.OnToggleAsync(false));
        await cut.Find("[data-part=cancel]").ClickAsync(new());

        Assert.Equal(["delete-trail"], Names(_shown));
        Assert.Equal([("delete-trail", "TbPopconfirm", "confirm"), ("delete-trail", "TbPopconfirm", "cancel"),
            ("delete-trail", "TbPopconfirm", "cancel")], Uses());
    }

    [Fact]
    public async Task Popover_ShouldCountItselfShownAndEachOpen_WhenItHasATelemetryName()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Details")
            .Add(popover => popover.TelemetryName, "trail-details").AddChildContent("Pine Hollow loop"));

        await cut.InvokeAsync(() => cut.Instance.OnToggleAsync(true));
        await cut.InvokeAsync(() => cut.Instance.OnToggleAsync(false));

        Assert.NotNull(JSInterop.Invocations["createPopover"].Single().Arguments[4]);
        Assert.Equal(["trail-details"], Names(_shown));
        Assert.Equal([("trail-details", "TbPopover", null)], Uses());
    }

    [Fact]
    public async Task Popover_ShouldLeaveAOneWayOpenToItsParent_WhenItHasATelemetryName()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Details")
            .Add(popover => popover.Open, false).Add(popover => popover.TelemetryName, "trail-details")
            .AddChildContent("Pine Hollow loop"));

        await cut.InvokeAsync(() => cut.Instance.OnToggleAsync(true));
        await cut.InvokeAsync(() => cut.Instance.OnToggleAsync(true));

        Assert.False(cut.Instance.Open);
        Assert.Equal([("trail-details", "TbPopover", null)], Uses());
    }

    [Fact]
    public void ToolbarButton_ShouldCountItsClicksInlineAndInTheOverflowMenu_WhenItHasATelemetryName()
    {
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Trail actions")
            .AddChildContent<TbToolbarButton>(button => button.Add(b => b.Label, "Export")
                .Add(b => b.TelemetryName, "export-csv").Add(b => b.OnClick, () => { })));
        var button = cut.FindComponent<TbToolbarButton>().Instance;
        var overflow = Render<OverflowMenuItems>(parameters => parameters.Add(items => items.Items,
            () => [new OverflowItem(button.Id, button)]));

        cut.Find("button").Click();
        overflow.Find("[role=menuitem]").Click();

        Assert.Equal([("export-csv", "TbButton", null), ("export-csv", "TbMenuItem", null)], Uses());
    }

    [Fact]
    public void MenuItems_ShouldCountACheckboxOrRadioItemChosen_WhenTheyHaveTelemetryNames()
    {
        var checkbox = Render<TbMenuCheckboxItem>(parameters => parameters
            .Add(item => item.TelemetryName, "show-closed").AddChildContent("Show closed trails"));
        var radio = Render<TbMenuRadioGroup<string>>(parameters => parameters.Add(group => group.Label, "Sort")
            .AddChildContent<TbMenuRadioItem<string>>(item => item.Add(i => i.Value, "name")
                .Add(i => i.TelemetryName, "sort-by-name").AddChildContent("Name")));

        checkbox.Find("[role=menuitemcheckbox]").Click();
        radio.Find("[role=menuitemradio]").Click();

        Assert.Empty(Names(_shown));
        Assert.Equal([("show-closed", "TbMenuCheckboxItem", null), ("sort-by-name", "TbMenuRadioItem", null)], Uses());
    }

    [Fact]
    public void ListItem_ShouldCountItselfShownAndEachClick_WhenItHasATelemetryName()
    {
        var cut = Render<TbListItem>(parameters => parameters.Add(item => item.TelemetryName, "open-report")
            .Add(item => item.OnClick, () => { }).AddChildContent("Pine Hollow loop"));

        cut.Find("button").Click();

        Assert.Equal(["open-report"], Names(_shown));
        Assert.Equal([("open-report", "TbListItem", null)], Uses());
    }

    [Fact]
    public void EmptyState_ShouldCountItselfShown_WhenItHasATelemetryName()
    {
        Render<TbEmptyState>(parameters => parameters.Add(empty => empty.Title, "No trails saved yet")
            .Add(empty => empty.TelemetryName, "no-saved-trails"));

        Assert.Equal(["no-saved-trails"], Names(_shown));
        Assert.Empty(Uses());
    }

    [Fact]
    public async Task Menu_ShouldCountNothing_WhenItOpensWithoutATelemetryName()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Export")));
        var events = ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createMenu"].Single()
            .Arguments[3]!).Value;

        await cut.InvokeAsync(() => events.OnToggle(true));

        Assert.Empty(Uses());
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
    public async Task Tab_ShouldCountEachClose_WhenItHasATelemetryNameAndIsClosedByItsMarkOrAKey()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/tabs.js");
        module.Setup<TabsAttachment?>("attachTabs", _ => true).SetResult(null);
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTab>(0);
            builder.AddAttribute(1, nameof(TbTab.Text), "Billing");
            builder.AddAttribute(2, nameof(TbTab.TelemetryName), "billing-tab");
            builder.AddAttribute(3, nameof(TbTab.OnClose), EventCallback.Factory.Create(this, () => { }));
            builder.CloseComponent();
        })));
        var order = (DotNetObjectReference<TabOrder>)module.Invocations["attachTabs"].Single().Arguments[1]!;

        cut.Find("[data-part=close]").Click();
        await cut.InvokeAsync(() => order.Value.OnClose(cut.Find("[role=tab]").Id!));

        Assert.Equal([("billing-tab", "TbTab", "close"), ("billing-tab", "TbTab", "close")], Uses());
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
            .Add(chip => chip.OnRemove, () => { }));

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
    public async Task CommandPalette_ShouldCountEachChoiceWithItsInput_WhenItHasATelemetryName()
    {
        var cut = Render<TbCommandPalette<string>>(parameters => parameters.Add(palette => palette.Items, ["New tab"])
            .Add(palette => palette.TelemetryName, "app-commands").Add(palette => palette.Open, true));

        await cut.Find("[role=option]").ClickAsync(new MouseEventArgs { Detail = 1 });
        cut.Render(parameters => parameters.Add(palette => palette.Open, false));
        cut.Render(parameters => parameters.Add(palette => palette.Open, true));
        await cut.Find("[role=option]").ClickAsync(new MouseEventArgs { Detail = 0 });

        Assert.Equal(["app-commands"], Names(_shown).Distinct());
        Assert.Equal([("app-commands", "TbCommandPalette", "choose"), ("app-commands", "TbCommandPalette", "choose")],
            Uses());
        Assert.Equal(["pointer", "keyboard"],
            _used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.input"]));
    }

    [Fact]
    public void SelectAll_ShouldCountItsUseWithTheInput_WhenTheSelectHasATelemetryName()
    {
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.Label, "Sizes")
            .Add(select => select.TelemetryName, "trail-sizes").Add(select => select.SelectAll, true)
            .AddChildContent<TbOption<string>>(option => option.Add(choice => choice.Value, "L")));

        cut.Find("[data-part=select-all]").Click(new MouseEventArgs { Detail = 1 });
        cut.Find("[data-part=select-all]").Click(new MouseEventArgs { Detail = 0 });

        Assert.Equal([("trail-sizes", "TbMultiSelect", "select_all"), ("trail-sizes", "TbMultiSelect", "select_all")],
            Uses());
        Assert.Equal(["pointer", "keyboard"],
            _used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.input"]));
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
            .Add(grid => grid.ResizableColumns, true).Add(grid => grid.ReorderableColumns, true)
            .Add(grid => grid.HideableColumns, true)
            .AddChildContent<TbColumn<Trail>>(column => column.Add(name => name.Field, trail => trail.Name)
                .Add(name => name.Sortable, true).Add(name => name.Filterable, true)
                .Add(name => name.Groupable, true).Add(name => name.Editable, true))
            .AddChildContent<TbColumn<Trail>>(column => column.Add(id => id.Field, trail => trail.Id))
            .Add(grid => grid.OnCellEdit, (GridCellEdit<Trail> _) => { }));

        cut.Find("th button").Click();
        cut.Find("nav [data-part=next]").Click();
        cut.Find("[data-part=pager] [role=option]:last-child").Click();
        cut.Find("tr[data-part=row] input").Change(true);
        await cut.Tb().Grid("Trails").FilterAsync("Name", "a");
        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(trails[1]));
        cut.Find("tbody td[data-column=Name]").DoubleClick();
        cut.Find("[data-part=editor]").TriggerEvent("ontbeditkey", new GridEditKeyEventArgs { Key = "Enter" });
        await cut.Tb().Select("Group by").ChooseAsync("Name");
        cut.Find("[data-part=group-toggle]").Click();
        cut.Find("[data-part=group-toggle]").Click();
        await cut.Tb().Grid("Trails").ResizeColumnAsync("Name", 200);
        await cut.Find("table").TriggerEventAsync("ontbcolumnorder",
            new ColumnOrderEventArgs { Order = ["Name"] });
        await cut.Tb().Menu("Columns").ChooseAsync("Id");
        await cut.Tb().Menu("Columns").ChooseAsync("Id");

        Assert.Equal(["trails"], Names(_shown));
        Assert.Equal(
            ["sort", "page", "page_size", "select", "filter", "edit", "group", "collapse", "expand", "resize",
                "reorder", "hide", "show"],
            Uses().Where(use => Equals(use.Name, "trails")).Select(use => use.Action));
    }

    [Fact]
    public async Task DataGrid_ShouldCountBatchSavesAndDiscardsButNotCodes_WhenItEditsInBatches()
    {
        Trail[] trails = [new(1, "Pine Hollow"), new(2, "Eagle Ridge")];
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.TelemetryName, "trails").Add(grid => grid.EditMode, GridEditMode.Batch)
            .Add(grid => grid.OnBatchSave, (GridBatchEdit<Trail> _) => { })
            .AddChildContent<TbColumn<Trail>>(column => column.Add(name => name.Field, trail => trail.Name)
                .Add(name => name.Editable, true)));
        async Task EditAsync(string name)
        {
            await cut.Find("tbody td[data-column=Name]").DoubleClickAsync(new());
            await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = name });
            await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey", new GridEditKeyEventArgs { Key = "Enter" });
        }

        await EditAsync("Pine loop");
        await cut.Tb().Grid("Trails").SaveChangesAsync();
        await EditAsync("Pine trail");
        await cut.Tb().Grid("Trails").DiscardChangesAsync();
        await EditAsync("Pine path");
        await cut.InvokeAsync(() => cut.Instance.SaveChangesAsync());

        Assert.Equal(["edit", "save_changes", "edit", "discard_changes", "edit"],
            Uses().Where(use => Equals(use.Name, "trails")).Select(use => use.Action));
    }

    [Fact]
    public async Task DataGrid_ShouldCountCopiesButNotRangeSelections_WhenItSelectsCells()
    {
        Trail[] trails = [new(1, "Pine Hollow"), new(2, "Eagle Ridge")];
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.TelemetryName, "trails").Add(grid => grid.CellSelection, true)
            .AddChildContent<TbColumn<Trail>>(column => column.Add(name => name.Field, trail => trail.Name)));

        await cut.Tb().Grid("Trails").SelectCellsAsync("Pine Hollow", "Name", "Eagle Ridge", "Name");
        await cut.Find("table").TriggerEventAsync("ontbcellscopy", EventArgs.Empty);

        Assert.Equal(["copy"], Uses().Where(use => Equals(use.Name, "trails")).Select(use => use.Action));
    }

    [Fact]
    public async Task TreeView_ShouldCountWhatTheUserDoesButNotWhatCodeDoes_WhenItHasATelemetryName()
    {
        Place[] places = [new(1, "North", [new(2, "Ridge")]), new(4, "South", [new(5, "Lake")])];
        var cut = Render<TbTreeView<Place, int>>(parameters => parameters.Add(tree => tree.Items, places)
            .Add(tree => tree.ChildrenSelector, place => place.Children).Add(tree => tree.KeySelector, place => place.Id)
            .Add(tree => tree.AriaLabel, "Places").Add(tree => tree.TelemetryName, "places")
            .Add(tree => tree.Checkable, true).Add(tree => tree.OnActivate, (Place _) => { })
            .Add(tree => tree.OnMove, (TreeMove<Place> _) => { }));
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
        await cut.Find(".tb-tree").TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs { Rows = ["4"], Target = "1", Position = TreeMovePosition.Before });
        await cut.InvokeAsync(() => cut.Instance.ExpandAsync(4));
        await cut.InvokeAsync(() => cut.Instance.SelectAsync(1));

        Assert.Equal(["places"], Names(_shown));
        Assert.Equal(["expand", "collapse", "select", "expand", "check", "activate", "collapse", "move"],
            Uses().Select(use => use.Action));
    }

    [Fact]
    public async Task TransferList_ShouldCountEachMoveButNotTheItems_WhenItHasATelemetryName()
    {
        var cut = Render<TbTransferList<string>>(parameters => parameters.Add(list => list.Label, "Crew")
            .Add(list => list.Items, ["Avery", "Blake"]).Add(list => list.TelemetryName, "crew"));

        await cut.Tb().TransferList("Crew").MoveAsync("Avery");
        await cut.Find("[data-part=move-all-available]").ClickAsync(new());

        Assert.Equal([("crew", (object?)"TbTransferList", (object?)"move"), ("crew", "TbTransferList", "move")],
            Uses());
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
