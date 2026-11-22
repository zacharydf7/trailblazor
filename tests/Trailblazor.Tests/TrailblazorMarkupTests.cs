using System.Globalization;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests;

public class TrailblazorMarkupTests : BunitContext
{
    private string? _size;
    private IReadOnlyList<string> _toppings = [];
    private string? _camp;
    private int? _copies;
    private string? _trail;
    private int? _budget;

    public TrailblazorMarkupTests() => this.AddTrailblazor();

    [Fact]
    public async Task Slider_ShouldLandOnTheNearestStepWithinTheRange_WhenSetBetweenStepsOrPastAnEnd()
    {
        var committed = new List<int>();
        var cut = Render<TbSlider<int>>(parameters => parameters.Add(field => field.Label, "Group size")
            .Add(field => field.Min, 2).Add(field => field.Max, 21).Add(field => field.Step, 4)
            .Add(field => field.OnValueCommitted, (int size) => committed.Add(size)));

        await cut.Tb().Slider("Group size").SetAsync(13);
        var between = cut.Tb().Slider("Group size").Value;
        await cut.Tb().Slider("Group size").SetAsync(40);

        // From 2 in steps of 4, 13 lands on 14, and 40 lands on 18, the last step below the maximum of 21.
        Assert.Equal((14m, 18m), (between, cut.Tb().Slider("Group size").Value));
        Assert.Equal([14, 18], committed);
    }

    [Fact]
    public async Task RangeSlider_ShouldMoveBothThumbsInEitherOrder_WhenTheHelperIsUsed()
    {
        var committed = new List<SliderRange<int>>();
        var cut = Render<TbRangeSlider<int>>(parameters => parameters.Add(field => field.Label, "Elevation")
            .Add(field => field.Value, new(20, 40)).Add(field => field.Step, 5)
            .Add(field => field.OnValueCommitted, (SliderRange<int> range) => committed.Add(range)));
        var elevation = cut.Tb().RangeSlider("Elevation");

        // From 20 to 40, a start of 62 is past the old end, so the end has to move first; 62 lands on 60.
        await elevation.SetAsync(62, 90);
        var past = elevation.Range;
        await elevation.SetAsync(10, 30);

        Assert.Equal(((60m, 90m), (10m, 30m)), (past, elevation.Range));
        Assert.Equal([new SliderRange<int>(20, 90), new(60, 90), new(10, 90), new(10, 30)], committed);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => elevation.SetAsync(50, 40));
    }

    [Fact]
    public async Task RangeSlider_ShouldMoveTheEndFirst_WhenTheStartSnapsPastAnEndBetweenSteps()
    {
        var cut = Render<TbRangeSlider<int>>(parameters => parameters.Add(field => field.Label, "Elevation")
            .Add(field => field.Value, new(20, 46)).Add(field => field.Step, 10));

        // From 20 to 46 in steps of 10, a start of 45 lands on 50, past the end.
        await cut.Tb().RangeSlider("Elevation").SetAsync(45, 60);

        Assert.Equal((50m, 60m), cut.Tb().RangeSlider("Elevation").Range);
    }

    [Fact]
    public async Task Splitter_ShouldResizeThePaneAsItsKeysDo_WhenTheHelperIsUsed()
    {
        var sizes = new List<double?>();
        var cut = Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, Panes(
            new("Trails", 25), new("Map", OnSize: sizes.Add), new("Details", 25))));

        await cut.Tb().Splitter("Resize Trails").ResizeAsync(25);
        var unchanged = sizes.Count;
        await cut.Tb().Splitter("Resize Trails").ResizeAsync(40);

        // The map, after the trails, gives up what the trails take, and the details keep their size.
        Assert.Equal((40, 35), (cut.Tb().Splitter("Resize Trails").Size, cut.Tb().Splitter("Resize Map").Size));
        // Resizing the trails to the size they have reports nothing, so the map, with no size of its own, gets none.
        Assert.Equal(0, unchanged);
        Assert.Equal([35d], sizes);
    }

    [Fact]
    public async Task Splitter_ShouldRefuseASize_WhenALimitOrACollapsedPaneKeepsTheKeysFromIt()
    {
        var limited = Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, Panes(
            new("Trails", 25, MaxSize: 30), new("Map"), new("Details", 25, MinSize: 20))));
        var collapsed = Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, Panes(
            new("Notes", 25), new("Photos"), new("Log", 25, Collapsed: true))));

        var capped = await Assert.ThrowsAsync<InvalidOperationException>(
            () => limited.Tb().Splitter("Resize Trails").ResizeAsync(40));
        // The details can't shrink below 20, so the map can grow only to 55.
        var held = await Assert.ThrowsAsync<InvalidOperationException>(
            () => limited.Tb().Splitter("Resize Map").ResizeAsync(60));
        await limited.Tb().Splitter("Resize Map").ResizeAsync(55);
        var shut = await Assert.ThrowsAsync<InvalidOperationException>(
            () => collapsed.Tb().Splitter("Resize Photos").ResizeAsync(40));
        Assert.Equal((25, 55), (limited.Tb().Splitter("Resize Trails").Size, limited.Tb().Splitter("Resize Map").Size));
        Assert.Contains("nearest it reaches is 30%", capped.Message);
        Assert.Contains("nearest it reaches is 55%", held.Message);
        Assert.Contains("collapsed", shut.Message);
    }

    private static RenderFragment Panes(params SplitterPane[] panes) => builder =>
    {
        foreach (var pane in panes)
        {
            builder.OpenComponent<TbSplitterPane>(0);
            builder.AddAttribute(1, nameof(TbSplitterPane.Label), pane.Label);
            builder.AddAttribute(2, nameof(TbSplitterPane.Size), pane.Size);
            builder.AddAttribute(3, nameof(TbSplitterPane.MinSize), pane.MinSize);
            builder.AddAttribute(4, nameof(TbSplitterPane.MaxSize), pane.MaxSize);
            builder.AddAttribute(5, nameof(TbSplitterPane.Collapsible), pane.Collapsible || pane.Collapsed);
            builder.AddAttribute(6, nameof(TbSplitterPane.Collapsed), pane.Collapsed);
            builder.AddAttribute(7, nameof(TbSplitterPane.ChildContent),
                (RenderFragment)(content => content.AddContent(0, pane.Label)));
            if (pane.OnSize is { } onSize)
            {
                builder.AddAttribute(8, nameof(TbSplitterPane.SizeChanged),
                    EventCallback.Factory.Create<double?>(new object(), onSize));
            }

            if (pane.OnCollapsed is { } onCollapsed)
            {
                builder.AddAttribute(9, nameof(TbSplitterPane.CollapsedChanged),
                    EventCallback.Factory.Create<bool>(new object(), onCollapsed));
            }

            builder.CloseComponent();
        }
    };

    private sealed record SplitterPane(string Label, double? Size = null, double MinSize = 0, double MaxSize = 100,
        bool Collapsed = false, Action<double?>? OnSize = null, bool Collapsible = false,
        Action<bool>? OnCollapsed = null);

    [Fact]
    public async Task Splitter_ShouldCollapseAndRestoreThePaneEnterToggles_WhenTheHelperIsUsed()
    {
        var changes = new List<bool>();
        var cut = Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, Panes(
            new("Trails", 25), new("Map", Collapsible: true, OnCollapsed: changes.Add))));
        var separator = cut.Tb().Splitter("Resize Trails");

        var before = separator.Collapsed;
        await separator.CollapseAsync();
        var collapsed = separator.Collapsed;
        await separator.CollapseAsync();
        await separator.RestoreAsync();

        // The trails can't collapse, so Enter on their separator collapses the map, the last pane, after it.
        Assert.Equal((false, true, false), (before, collapsed, separator.Collapsed));
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public async Task Splitter_ShouldRefuseToCollapse_WhenNeitherPaneBesideTheSeparatorCan()
    {
        var cut = Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, Panes(
            new("Trails", 25), new("Map"))));

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Splitter("Resize Trails").CollapseAsync());

        Assert.Contains("Neither pane beside \"Resize Trails\" can collapse", refused.Message);
    }

    [Fact]
    public async Task AccordionItem_ShouldFindTheItemByItsTitleOrFullName_WhenItsHeaderShowsABadge()
    {
        var cut = Render<TbAccordionItem>(parameters => parameters.Add(item => item.TitleContent, builder =>
        {
            builder.AddContent(0, "Reviews ");
            builder.OpenComponent<TbBadge>(1);
            builder.AddComponentParameter(2, nameof(TbBadge.Count), 3);
            builder.CloseComponent();
        }).AddChildContent("Reviews content"));

        await cut.Tb().AccordionItem("Reviews").ExpandAsync();
        var opened = cut.Find("button").GetAttribute("aria-expanded");
        await cut.Tb().AccordionItem("Reviews 3").CollapseAsync();

        Assert.Equal(("true", "false"), (opened, cut.Find("button").GetAttribute("aria-expanded")));
        Assert.Throws<InvalidOperationException>(() => cut.Tb().AccordionItem("3").Trigger);
    }

    [Fact]
    public async Task TextField_ShouldRefuseToType_WhenItsFieldsetIsDisabledOrItIsReadOnlyByAria()
    {
        var fieldset = Render(builder =>
        {
            builder.OpenElement(0, "fieldset");
            builder.AddAttribute(1, "disabled", true);
            builder.OpenComponent<TbTextField<string>>(2);
            builder.AddComponentParameter(3, nameof(TbTextField<string>.Label), "Trail");
            builder.CloseComponent();
            builder.CloseElement();
        });
        var aria = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.InputAttributes, new Dictionary<string, object> { ["aria-readonly"] = "true" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fieldset.Tb().TextField("Trail").EnterAsync("Ash"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => aria.Tb().TextField("Trail").EnterAsync("Ash"));
    }

    [Fact]
    public async Task TagInput_ShouldRefuseToAddATag_WhenItsFieldsetIsDisabled()
    {
        var cut = Render(builder =>
        {
            builder.OpenElement(0, "fieldset");
            builder.AddAttribute(1, "disabled", true);
            builder.OpenComponent<TbTagInput>(2);
            builder.AddComponentParameter(3, nameof(TbTagInput.Label), "Features");
            builder.CloseComponent();
            builder.CloseElement();
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().TagInput("Features").AddAsync("Wi-Fi"));
    }

    [Fact]
    public async Task AccordionItem_ShouldFindTheItemByItsFullName_WhenAnotherItemsTitleHasTheSameTextWithoutABadge()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbAccordionItem>(0);
            builder.AddComponentParameter(1, nameof(TbAccordionItem.Title), "Shipping");
            builder.CloseComponent();
            builder.OpenComponent<TbAccordionItem>(2);
            builder.AddComponentParameter(3, nameof(TbAccordionItem.TitleContent), (RenderFragment)(title =>
            {
                title.AddContent(0, "Shipping ");
                title.AddMarkupContent(1, "<em>(optional)</em>");
            }));
            builder.CloseComponent();
        });

        await cut.Tb().AccordionItem("Shipping").ExpandAsync();

        Assert.Equal(["true", "false"], cut.FindAll("button").Select(button => button.GetAttribute("aria-expanded")));
    }

    [Fact]
    public async Task RangeSlider_ShouldRefuseToMoveIt_WhenItIsReadOnlyOrOnAStaticPage()
    {
        var locked = Render<TbRangeSlider<int>>(parameters => parameters.Add(field => field.Label, "Elevation")
            .Add(field => field.ReadOnly, true));
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var still = Render<TbRangeSlider<int>>(parameters => parameters.Add(field => field.Label, "Elevation"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => locked.Tb().RangeSlider("Elevation").SetAsync(5, 9));
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => still.Tb().RangeSlider("Elevation").SetAsync(5, 9));
        Assert.Contains("static page", failure.Message);
    }

    [Fact]
    public async Task Slider_ShouldRefuseToMoveIt_WhenItIsReadOnlyOrOnAStaticPage()
    {
        var locked = Render<TbSlider<int>>(parameters => parameters.Add(field => field.Label, "Group size")
            .Add(field => field.ReadOnly, true));
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var still = Render<TbSlider<int>>(parameters => parameters.Add(field => field.Label, "Group size"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => locked.Tb().Slider("Group size").SetAsync(5));
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => still.Tb().Slider("Group size").SetAsync(5));
        Assert.Contains("static page", failure.Message);
    }

    [Fact]
    public async Task Autocomplete_ShouldTypeAndChooseASuggestion_WhenTheOptionIsSuggested()
    {
        string? trail = null;
        var cut = Render<TbAutocomplete<string?>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, ["Pine Hollow", "Eagle Ridge", "Pine Bluff"])
            .Add(field => field.ValueChanged, (string? chosen) => trail = chosen));

        await cut.Tb().Autocomplete("Trail").ChooseAsync("pine", "Pine Bluff");
        var missing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Autocomplete("Trail").ChooseAsync("pine", "Eagle Ridge"));

        Assert.Equal("Pine Bluff", trail);
        Assert.Equal("No option reads \"Eagle Ridge\". The options read: \"Pine Hollow\", \"Pine Bluff\".",
            missing.Message);
    }

    [Fact]
    public async Task Autocomplete_ShouldReadTheTextAndSuggestions_WhenTypedWithoutChoosing()
    {
        var cut = Render<TbAutocomplete<string?>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, ["Pine Hollow", "Eagle Ridge", "Pine Bluff"]));
        var trail = cut.Tb().Autocomplete("Trail");

        await trail.TypeAsync("pine");

        Assert.Equal("pine", trail.Text);
        Assert.Equal(["Pine Hollow", "Pine Bluff"], trail.Suggestions);
    }

    [Fact]
    public async Task Autocomplete_ShouldCommitTheTextAndClearIt_WhenEnteredAndCleared()
    {
        string? trail = null;
        var cut = Render<TbAutocomplete<string?>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, ["Pine Hollow"]).Add(field => field.FreeText, true)
            .Add(field => field.Clearable, true).Add(field => field.ValueChanged, (string? chosen) => trail = chosen));
        var field = cut.Tb().Autocomplete("Trail");

        await field.EnterAsync("Cedar Flats");
        var entered = trail;
        await field.ClearAsync();
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(field.ClearAsync);

        Assert.Equal(("Cedar Flats", null, ""), (entered, trail, field.Text));
        Assert.Contains("make it Clearable", refused.Message);
    }

    [Fact]
    public async Task Autocomplete_ShouldChooseWithoutWaitingForTheTextReport_WhenTheAppsClockIsFake()
    {
        Services.AddSingleton<TimeProvider>(new Microsoft.Extensions.Time.Testing.FakeTimeProvider());
        var reported = new List<string>();
        string? trail = "Pine Bluff";
        var cut = Render<TbAutocomplete<string?>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, ["Pine Hollow", "Pine Bluff"]).Add(field => field.Value, trail)
            .Add(field => field.ValueChanged, (string? chosen) => trail = chosen)
            .Add(field => field.OnTextChanged, (string text) => reported.Add(text)));

        // The field's report of the typed text waits out DebounceDelay on a clock that never moves, so a helper that
        // waited for it would never return.
        await cut.Tb().Autocomplete("Trail").ChooseAsync("pine", "Pine Bluff")
            .WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("Pine Bluff", cut.Find("input").GetAttribute("value"));
        Assert.Equal(["Pine Bluff"], reported);
    }

    [Fact]
    public async Task Autocomplete_ShouldRefuseToClear_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbAutocomplete<string?>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, ["Pine Hollow"]).Add(field => field.Clearable, true)
            .Add(field => field.Value, "Pine Hollow"));

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(cut.Tb().Autocomplete("Trail").ClearAsync);

        Assert.Contains("static page", refused.Message);
    }

    [Fact]
    public async Task Autocomplete_ShouldTypeAndEnterWithoutWaitingForTheTextReport_WhenTheAppsClockIsFake()
    {
        Services.AddSingleton<TimeProvider>(new Microsoft.Extensions.Time.Testing.FakeTimeProvider());
        string? trail = null;
        var cut = Render<TbAutocomplete<string?>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, ["Pine Hollow", "Pine Bluff"]).Add(field => field.FreeText, true)
            .Add(field => field.ValueChanged, (string? chosen) => trail = chosen)
            .Add(field => field.OnTextChanged, (string _) => { }));
        var field = cut.Tb().Autocomplete("Trail");

        // The report of the typed text waits out DebounceDelay on a clock that never moves.
        await field.TypeAsync("pine").WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        var suggested = field.Suggestions;
        await field.EnterAsync("Cedar Flats")
            .WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["Pine Hollow", "Pine Bluff"], suggested);
        Assert.Equal("Cedar Flats", trail);
    }

    [Fact]
    public async Task ColorPicker_ShouldTakeTheTypedColorAndRefuseAnotherFormat_WhenAColorIsChosen()
    {
        string? color = null;
        var cut = Render<TbColorPicker>(parameters => parameters.Add(field => field.Label, "Trail color")
            .Add(field => field.ValueChanged, (string? chosen) => color = chosen));

        await cut.Tb().ColorPicker("Trail color").ChooseAsync("#336699");

        Assert.Equal("#336699", color);
        await Assert.ThrowsAsync<ArgumentException>(() => cut.Tb().ColorPicker("Trail color").ChooseAsync("blue"));
    }

    [Fact]
    public async Task ColorPicker_ShouldReadTheColorItHolds_WhenOneIsChosen()
    {
        string? color = null;
        var cut = Render<TbColorPicker>(parameters => parameters.Add(field => field.Label, "Trail color")
            .Bind(field => field.Value, color, chosen => color = chosen));
        var before = cut.Tb().ColorPicker("Trail color").Value;

        await cut.Tb().ColorPicker("Trail color").ChooseAsync("#336699");

        Assert.Equal((null, "#336699"), (before, cut.Tb().ColorPicker("Trail color").Value));
    }

    [Fact]
    public void ColorPicker_ShouldReadTheColorThePlainInputShows_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbColorPicker>(0);
            builder.AddComponentParameter(1, nameof(TbColorPicker.Label), "Accent");
            builder.AddComponentParameter(2, nameof(TbColorPicker.Value), "#336699");
            builder.CloseComponent();
            builder.OpenComponent<TbColorPicker>(3);
            builder.AddComponentParameter(4, nameof(TbColorPicker.Label), "Marker");
            builder.CloseComponent();
        });

        Assert.Equal(("#336699", null), (cut.Tb().ColorPicker("Accent").Value, cut.Tb().ColorPicker("Marker").Value));
    }

    [Fact]
    public async Task FileUpload_ShouldTakeFilesMadeInTheTestAndListTheirNames_WhenChosenInMemory()
    {
        IReadOnlyList<IBrowserFile>? files = null;
        var cut = Render<TbFileUpload>(parameters => parameters.Add(upload => upload.Label, "Trip files")
            .Add(upload => upload.Multiple, true)
            .Bind(upload => upload.Value, null, chosen => files = chosen));
        var before = cut.Tb().FileUpload("Trip files").Files;

        await cut.Tb().FileUpload("Trip files").ChooseAsync(InputFileContent.CreateFromText("Day one", "notes.txt"),
            InputFileContent.CreateFromBinary(new byte[5], "summit.png", contentType: "image/png"));

        Assert.Empty(before);
        Assert.Equal(["notes.txt", "summit.png"], cut.Tb().FileUpload("Trip files").Files);
        Assert.Equal([("notes.txt", 7L), ("summit.png", 5L)], files!.Select(file => (file.Name, file.Size)));
    }

    [Fact]
    public async Task FileUpload_ShouldHandTheFieldTheFilesWithTheTypeTheirExtensionGives_WhenChosenByPath()
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            var photo = Path.Combine(folder.FullName, "summit.png");
            var map = Path.Combine(folder.FullName, "route.pdf");
            await File.WriteAllBytesAsync(photo, new byte[12], Xunit.TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(map, new byte[3], Xunit.TestContext.Current.CancellationToken);
            IReadOnlyList<IBrowserFile>? files = null;
            var cut = Render<TbFileUpload>(parameters => parameters.Add(upload => upload.Label, "Photos")
                .Add(upload => upload.Multiple, true).Add(upload => upload.Accept, "image/*")
                .Add(upload => upload.ValueChanged, (IReadOnlyList<IBrowserFile>? chosen) => files = chosen));

            await cut.Tb().FileUpload("Photos").ChooseAsync(photo, map);

            // The map's type isn't an image, so the field turns it away, as it does in a browser.
            var file = Assert.Single(files!);
            Assert.Equal(("summit.png", "image/png", 12L), (file.Name, file.ContentType, file.Size));
            Assert.Equal("route.pdf isn't one of image/*.", cut.Find("[data-part=rejected]").TextContent.Trim());
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Dialog_ShouldAnswerAConfirmByItsButtonOrEscape_WhenFoundByItsTitle()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var deleting = dialogs.ConfirmAsync("Delete trail", "Delete it?", "Delete");
        var leaving = dialogs.ConfirmAsync("Leave trip", "Leave it?", "Leave");

        await host.Tb().Dialog("Leave trip").CancelAsync();
        await host.Tb().Dialog("Delete trail").ChooseAsync("Delete");

        Assert.True(await deleting);
        Assert.False(await leaving);
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.Tb().Dialog("Delete trail").WaitForAsync());
    }

    [Fact]
    public async Task Dialog_ShouldClickADrawersButtonAndCloseIt_WhenFoundByTheDrawersTitle()
    {
        Render<DialogHost>();
        var (open, shown) = (true, 0);
        var cut = Render<TbDrawer>(parameters => parameters.Add(drawer => drawer.Title, "Trail filters")
            .Add(drawer => drawer.Open, true).Add(drawer => drawer.OpenChanged, (bool now) => open = now)
            .AddChildContent<TbDialogActions>(actions => actions.AddChildContent<TbButton>(button => button
                .Add(each => each.OnClick, () => shown++).AddChildContent("Show trails"))));

        await cut.Tb().Dialog("Trail filters").ChooseAsync("Show trails");
        await cut.Tb().Dialog("Trail filters").CancelAsync();

        Assert.Equal(1, shown);
        Assert.False(open);
    }

    [Fact]
    public async Task Dialog_ShouldFindAndCancelADialogByTheTitleItShows_WhenItsTitleIsRichContent()
    {
        Render<DialogHost>();
        var open = true;
        var cut = Render<TbDialog>(parameters => parameters.Add(dialog => dialog.Title, "Edit trail")
            .Add(dialog => dialog.Open, true).Add(dialog => dialog.OpenChanged, (bool now) => open = now)
            .AddChildContent<TbDialogTitle>(title => title.AddChildContent("Edit <em>North ridge</em>")));

        await cut.Tb().Dialog("Edit North ridge").CancelAsync();

        Assert.False(open);
    }

    [Fact]
    public async Task Dialog_ShouldReadTheOnlyOpenDialog_WhenNoTitleIsGiven()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var closed = host.Tb().Dialog().IsOpen;
        var deleting = host.InvokeAsync(() => dialogs.ConfirmAsync("Delete trail", "Delete it?", "Delete"));

        var (title, open) = (host.Tb().Dialog().Title, host.Tb().Dialog("Delete trail").IsOpen);
        await host.Tb().Dialog().ChooseAsync("Delete");

        Assert.Equal((false, "Delete trail", true), (closed, title, open));
        Assert.True(await deleting);
        Assert.False(host.Tb().Dialog("Delete trail").IsOpen);
    }

    [Fact]
    public async Task Dialog_ShouldRefuseToGuess_WhenNoTitleIsGivenAndTwoDialogsAreOpen()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = host.InvokeAsync(() => dialogs.ConfirmAsync("Delete trail", "Delete it?"));
        _ = host.InvokeAsync(() => dialogs.ConfirmAsync("Leave trip", "Leave it?"));

        var error = Assert.Throws<InvalidOperationException>(() => host.Tb().Dialog().Title);

        Assert.Equal("2 dialogs are open; give the title of the one to use.", error.Message);
    }

    [Fact]
    public async Task Dialog_ShouldMinimizeMaximizeAndRestore_WhenItHasTheWindowButtons()
    {
        Render<DialogHost>();
        var cut = Render<TbDialog>(parameters => parameters.Add(dialog => dialog.Title, "Trail notes")
            .Add(dialog => dialog.Open, true)
            .Add(dialog => dialog.Options, new DialogOptions { Minimizable = true, Maximizable = true }));
        var dialog = cut.Tb().Dialog("Trail notes");
        var states = new List<DialogWindowState> { dialog.WindowState };

        foreach (var step in new Func<Task>[]
            { dialog.MinimizeAsync, dialog.MinimizeAsync, dialog.RestoreAsync, dialog.MaximizeAsync, dialog.RestoreAsync })
        {
            await step();
            states.Add(dialog.WindowState);
        }

        // Minimizing a minimized dialog leaves it so, where its button alone would restore it.
        Assert.Equal([DialogWindowState.Normal, DialogWindowState.Minimized, DialogWindowState.Minimized,
            DialogWindowState.Normal, DialogWindowState.Maximized, DialogWindowState.Normal], states);
    }

    [Fact]
    public async Task Dialog_ShouldWaitForTheHeadersCallback_WhenItsWindowStateChanges()
    {
        Render<DialogHost>();
        var states = new List<DialogWindowState>();
        var cut = Render<TbDialog>(parameters => parameters.Add(dialog => dialog.Title, "Trail notes")
            .Add(dialog => dialog.Open, true).Add(dialog => dialog.Options, new DialogOptions { Minimizable = true })
            .AddChildContent<TbDialogHeader>(header => header.Add(each => each.WindowStateChanged,
                async (DialogWindowState state) =>
                {
                    await Task.Delay(100);
                    states.Add(state);
                })));

        await cut.Tb().Dialog("Trail notes").MinimizeAsync();

        Assert.Equal([DialogWindowState.Minimized], states);
    }

    [Fact]
    public async Task Dialog_ShouldWaitForOpenChanged_WhenCanceledOrClosedByItsCloseButton()
    {
        Render<DialogHost>();
        var answers = new List<bool>();
        var cut = Render<TbDialog>(parameters => parameters.Add(dialog => dialog.Title, "Trail notes")
            .Add(dialog => dialog.Open, true).Add(dialog => dialog.OpenChanged, async (bool open) =>
            {
                await Task.Delay(100);
                answers.Add(open);
            }));

        await cut.Tb().Dialog("Trail notes").CancelAsync();
        var canceled = answers.Count;
        cut.Render(parameters => parameters.Add(dialog => dialog.Open, true));
        await cut.Tb().Dialog("Trail notes").ChooseAsync("Close");

        Assert.Equal((1, 2), (canceled, answers.Count));
    }

    [Fact]
    public async Task Dialog_ShouldThrow_WhenMaximizingADialogWithoutTheButton()
    {
        Render<DialogHost>();
        var cut = Render<TbDialog>(parameters => parameters.Add(dialog => dialog.Title, "Trail notes")
            .Add(dialog => dialog.Open, true));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Dialog("Trail notes").MaximizeAsync());

        Assert.Equal("This dialog has no Maximize button.", error.Message);
    }

    [Fact]
    public async Task Dialog_ShouldWaitUntilTheAppClosesIt_WhenTheAppClosesItLater()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = host.InvokeAsync(() => dialogs.ConfirmAsync("Saving trail", "Saving..."));

        var waiting = host.Tb().Dialog("Saving trail").WaitForClosedAsync();
        var early = waiting.IsCompleted;
        await host.InvokeAsync(() => dialogs.OpenDialogs.Single().CancelAsync());
        await waiting;

        Assert.False(early);
        Assert.False(host.Tb().Dialog("Saving trail").IsOpen);
    }

    [Fact]
    public async Task Dialog_ShouldRefuseEscape_WhenItsOptionsDontLetEscapeDismissIt()
    {
        var host = Render<DialogHost>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = dialogs.ConfirmAsync("Delete trail", "Delete it?", options: new() { DismissOnEscape = false });

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.Tb().Dialog("Delete trail").CancelAsync());

        Assert.Single(dialogs.OpenDialogs);
    }

    [Fact]
    public async Task Select_ShouldChooseTheOptionAndShowIt_WhenChosenByItsText()
    {
        var cut = RenderForm();

        await cut.Tb().Select("Size").ChooseAsync("Medium");

        Assert.Equal("Medium", _size);
        Assert.Equal("Medium", cut.Tb().Select("Size").Text);
        Assert.Equal(["Small", "Medium", "Large"], cut.Tb().Select("Size").Options);
    }

    private IRenderedComponent<TbSelect<string>> RenderSizes(List<bool> opens,
        Action<ComponentParameterCollectionBuilder<TbSelect<string>>>? configure = null) =>
        Render<TbSelect<string>>(parameters =>
        {
            parameters.Add(select => select.Label, "Size")
                .Add(select => select.OpenChanged, (bool open) => opens.Add(open))
                .AddChildContent<TbOption<string>>(option => option.Add(o => o.Value, "s").Add(o => o.Text, "Small"));
            configure?.Invoke(parameters);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Select_ShouldOpenAndCloseTheListAndRaiseOpenChanged_WhenTheHelperIsUsed(bool attachOnFirstUse)
    {
        var opens = new List<bool>();
        var size = RenderSizes(opens, parameters => parameters.Add(select => select.AttachOnFirstUse, attachOnFirstUse))
            .Tb().Select("Size");

        var before = size.IsOpen;
        await size.OpenAsync();
        var opened = size.IsOpen;
        await size.OpenAsync();
        await size.CloseAsync();
        await size.CloseAsync();

        Assert.Equal((false, true, false), (before, opened, size.IsOpen));
        Assert.Equal([true, false], opens);
    }

    [Fact]
    public async Task Select_ShouldRefuseToOpenTheList_WhenReadOnlyDisabledOrStatic()
    {
        var opens = new List<bool>();
        var readOnly = RenderSizes(opens, parameters => parameters.Add(select => select.ReadOnly, true));
        var disabled = RenderSizes(opens, parameters => parameters.Add(select => select.Disabled, true));
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var still = RenderSizes(opens);

        var refused = await Task.WhenAll(new[] { readOnly, disabled, still }.Select(cut =>
            Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Select("Size").OpenAsync())));

        Assert.Contains("read-only", refused[0].Message);
        Assert.Contains("disabled", refused[1].Message);
        Assert.Contains("static", refused[2].Message);
        Assert.Null(await Record.ExceptionAsync(() => still.Tb().Select("Size").CloseAsync()));
        Assert.Empty(opens);
    }

    [Fact]
    public async Task Select_ShouldOpenTheListBeforeChoosingAndCloseItAfter_WhenTheHelperChooses()
    {
        var opens = new List<bool>();
        var size = RenderSizes(opens).Tb().Select("Size");

        await size.ChooseAsync("Small");

        Assert.Equal([true, false], opens);
        Assert.Equal(("Small", false), (size.Text, size.IsOpen));
    }

    [Fact]
    public async Task Select_ShouldOpenTheListOnceForSeveralOptions_WhenAMultipleSelectChoosesAndUnchooses()
    {
        var opens = new List<bool>();
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.Label, "Toppings")
            .Add(select => select.OpenChanged, (bool open) => opens.Add(open))
            .Add(select => select.ChildContent, Options("Olives", "Basil", "Garlic")));
        var toppings = cut.Tb().Select("Toppings");

        await toppings.ChooseAsync("Olives", "Basil");
        await toppings.UnchooseAsync("Olives");

        Assert.Equal([true, false, true, false], opens);
        Assert.Equal(["Basil"], toppings.Chosen);
    }

    [Fact]
    public async Task Select_ShouldRefuseToChoose_WhenTheSelectIsReadOnly()
    {
        var opens = new List<bool>();
        var size = RenderSizes(opens, parameters => parameters.Add(select => select.ReadOnly, true))
            .Tb().Select("Size");

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => size.ChooseAsync("Small"));

        Assert.Contains("read-only", refused.Message);
        Assert.Equal((null, false), (size.Text, size.IsOpen));
        Assert.Empty(opens);
    }

    [Fact]
    public async Task Select_ShouldLetTheAppLoadItsOptions_WhenTheyArriveAsTheListOpens()
    {
        var cut = Render<LazySizes>();

        await cut.Tb().Select("Size").ChooseAsync("Large");

        Assert.Equal("Large", cut.Tb().Select("Size").Text);
    }

    [Fact]
    public async Task Select_ShouldOpenOnlyTheSelectItNames_WhenThePageHasTwo()
    {
        var sizes = new List<bool>();
        var colors = new List<bool>();
        var cut = Render(builder =>
        {
            foreach (var (label, opens) in new[] { ("Size", sizes), ("Color", colors) })
            {
                builder.OpenComponent<TbSelect<string>>(0);
                builder.AddAttribute(1, nameof(TbSelect<string>.Label), label);
                builder.AddAttribute(2, nameof(TbSelect<string>.OpenChanged),
                    EventCallback.Factory.Create<bool>(this, opens.Add));
                builder.CloseComponent();
            }
        });

        await cut.Tb().Select("Size").OpenAsync();

        Assert.Equal((true, false), (cut.Tb().Select("Size").IsOpen, cut.Tb().Select("Color").IsOpen));
        Assert.Equal([true], sizes);
        Assert.Empty(colors);
    }

    [Fact]
    public async Task Select_ShouldReadUnchooseAndClearTheChosenOptions_WhenAsked()
    {
        IReadOnlyList<string> toppings = [];
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.Label, "Toppings")
            .Add(select => select.Clearable, true).Add(select => select.SelectAll, true)
            .Add(select => select.ValueChanged, (IReadOnlyList<string> value) => toppings = value)
            .Add(select => select.ChildContent, Options("Olives", "Basil", "Garlic")));
        var select = cut.Tb().Select("Toppings");
        await select.ChooseAsync("Olives", "Basil", "Garlic");
        Assert.Equal(["Olives", "Basil", "Garlic"], select.Chosen);

        await select.UnchooseAsync("Olives");
        Assert.Equal(["Basil", "Garlic"], select.Chosen);
        await select.ClearAsync();

        Assert.Empty(toppings);
    }

    [Fact]
    public void Select_ShouldThrow_WhenReadingTheChosenOptionsOfAListTooLongToShowWhole()
    {
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.Label, "Trails")
            .Add(select => select.Items, Enumerable.Range(1, 101).Select(number => $"Trail {number}").ToList()));

        Assert.Throws<InvalidOperationException>(() => cut.Tb().Select("Trails").Chosen);
    }

    [Fact]
    public async Task Select_ShouldThrow_WhenUnchoosingInASelectThatTakesOneOption()
    {
        var cut = RenderForm();
        await cut.Tb().Select("Size").ChooseAsync("Small");

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Select("Size").UnchooseAsync("Small"));
    }

    [Fact]
    public async Task Select_ShouldThrow_WhenClearedWithoutAClearButton()
    {
        var cut = RenderForm();
        await cut.Tb().Select("Size").ChooseAsync("Medium");

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Select("Size").ClearAsync());
    }

    [Fact]
    public async Task ListBox_ShouldUnchooseAndClearTheChosenOptions_WhenItTakesSeveral()
    {
        IReadOnlyList<string> gear = [];
        var cut = Render<TbMultiListBox<string>>(parameters => parameters.Add(list => list.Label, "Gear")
            .Add(list => list.ValueChanged, (IReadOnlyList<string> value) => gear = value)
            .Add(list => list.ChildContent, Options("Tent", "Stove", "Map")));
        var list = cut.Tb().ListBox("Gear");
        await list.ChooseAsync("Tent", "Stove", "Map");

        await list.UnchooseAsync("Stove");
        Assert.Equal(["Tent", "Map"], list.Chosen);
        await list.ClearAsync();

        Assert.Empty(gear);
    }

    [Fact]
    public async Task Select_ShouldReadTheOptionsButSayChoosingNeedsAnInteractivePage_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderForm();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Select("Size").ChooseAsync("Large"));

        Assert.Equal(["Small", "Medium", "Large"], cut.Tb().Select("Size").Options);
        Assert.Null(cut.Tb().Select("Size").Text);
        Assert.Contains("interactive page", error.Message);
    }

    [Fact]
    public void Select_ShouldShowAValueNoOptionHasButNotListIt_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        _size = "Huge";
        var cut = RenderForm();

        Assert.Equal(["Small", "Medium", "Large"], cut.Tb().Select("Size").Options);
        Assert.Equal("Huge", cut.Tb().Select("Size").Text);
    }

    [Fact]
    public async Task Select_ShouldNameAnOptionByItsText_WhenItShowsContentOfItsOwn()
    {
        string? medal = null;
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Medal")
            .Add(select => select.ValueChanged, (string value) => medal = value)
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<string>>(0);
                builder.AddAttribute(1, nameof(TbOption<string>.Value), "gold");
                builder.AddAttribute(2, nameof(TbOption<string>.Text), "Gold");
                builder.AddAttribute(3, nameof(TbOption<string>.ChildContent),
                    (RenderFragment)(content => content.AddMarkupContent(0, "<img src=\"gold.png\" /> gold.png")));
                builder.CloseComponent();
            })));
        var select = cut.Tb().Select("Medal");

        await select.ChooseAsync("Gold");

        Assert.Equal(["Gold"], select.Options);
        Assert.Equal("gold", medal);
        Assert.Equal("Gold", select.Text);
    }

    [Fact]
    public async Task Select_ShouldChooseTheOptionThatReadsExactlyTheText_WhenAnotherOptionContainsIt()
    {
        var cut = RenderForm();

        await cut.Tb().Select("Trail").ChooseAsync("Ridge");

        Assert.Equal("Ridge", _trail);
    }

    [Fact]
    public async Task Select_ShouldSayWhichTextIsMissing_WhenNoOptionReadsIt()
    {
        var cut = RenderForm();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Select("Size").ChooseAsync("Huge"));

        Assert.Contains("\"Huge\"", error.Message);
    }

    [Fact]
    public async Task Select_ShouldChooseEachOption_WhenAMultipleSelectIsFilterable()
    {
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.Label, "Toppings")
            .Add(select => select.Filterable, true).Add(select => select.Value, _toppings)
            .Add(select => select.ValueChanged, (IReadOnlyList<string> value) => _toppings = value)
            .Add(select => select.ChildContent, Options("Olives", "Basil", "Garlic")));

        await cut.Tb().Select("Toppings").ChooseAsync("Olives", "Basil");

        Assert.Equal(["Olives", "Basil"], _toppings);
    }

    [Fact]
    public async Task Select_ShouldChooseEachOptionAndShowThemAll_WhenTheSelectIsMultiple()
    {
        var cut = RenderForm();

        await cut.Tb().Select("Toppings").ChooseAsync("Olives", "Basil");

        Assert.Equal(["Olives", "Basil"], _toppings);
        Assert.Equal("Olives, Basil", cut.Tb().Select("Toppings").Text);
    }

    [Fact]
    public async Task Select_ShouldKeepAnOptionAlreadyChosen_WhenTheSelectIsMultiple()
    {
        var cut = RenderForm();
        await cut.Tb().Select("Toppings").ChooseAsync("Olives");

        await cut.Tb().Select("Toppings").ChooseAsync("Olives", "Garlic");

        Assert.Equal(["Olives", "Garlic"], _toppings);
    }

    [Fact]
    public async Task Select_ShouldRefuse_WhenGivenNoOptionsOrSeveralForASingleSelect()
    {
        var cut = RenderForm();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cut.Tb().Select("Size").ChooseAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => cut.Tb().Select("Size").ChooseAsync("Small", "Large"));
        Assert.Null(_size);
    }

    [Fact]
    public async Task Select_ShouldRefuseToChoose_WhenTheSelectIsDisabled()
    {
        var cut = RenderForm();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Select("Region").ChooseAsync("North"));

        Assert.Contains("disabled", error.Message);
    }

    [Fact]
    public async Task Select_ShouldBeFoundByItsLabel_WhenAButtonReadsTheSameText()
    {
        var cut = RenderForm();

        await cut.Tb().Select("Size").ChooseAsync("Small");

        Assert.Equal("Small", _size);
    }

    [Fact]
    public async Task NumberField_ShouldTakeTheNumber_WhenTheFieldIsFormatted()
    {
        var cut = RenderForm();

        await cut.Tb().NumberField("Budget").EnterAsync("1200");

        Assert.Equal(1200, _budget);
    }

    [Fact]
    public async Task ListBox_ShouldChooseTheOption_WhenChosenByItsText()
    {
        var cut = RenderForm();

        await cut.Tb().ListBox("Camp").ChooseAsync("Meadow");

        Assert.Equal("Meadow", _camp);
        Assert.Equal(["Meadow"], cut.Tb().ListBox("Camp").Chosen);
    }

    [Fact]
    public async Task NumberField_ShouldTakeTheNumber_WhenATextIsEntered()
    {
        var cut = RenderForm();

        await cut.Tb().NumberField("Copies").EnterAsync("12");

        Assert.Equal(12, _copies);
        Assert.Equal("12", cut.Tb().NumberField("Copies").Text);
    }

    [Fact]
    public async Task NumberField_ShouldStepUntilItsLimit_WhenStepped()
    {
        var changes = new List<int?>();
        var cut = Render<TbNumberField<int?>>(parameters => parameters.Add(field => field.Label, "Nights")
            .Add(field => field.Min, 1m).Add(field => field.Max, 4m)
            .Bind(field => field.Value, 2, changes.Add));
        var field = cut.Tb().NumberField("Nights");

        await field.StepAsync(5);
        await field.StepAsync(-1);

        Assert.Equal([3, 4, 3], changes);
    }

    [Theory]
    [InlineData(nameof(TbNumberField<int>.ReadOnly))]
    [InlineData(nameof(TbNumberField<int>.HideStepButtons))]
    public async Task NumberField_ShouldThrowOnStepping_WhenTheFieldCantBeStepped(string setting)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbNumberField<int>>(0);
            builder.AddAttribute(1, nameof(TbNumberField<int>.Label), "Nights");
            builder.AddAttribute(2, setting, true);
            builder.CloseComponent();
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().NumberField("Nights").StepAsync(1));
    }

    [Fact]
    public async Task NumberField_ShouldThrowOnStepping_WhenTheCountIsZero()
    {
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Label, "Nights"));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cut.Tb().NumberField("Nights").StepAsync(0));
    }

    [Fact]
    public async Task RichTextEditor_ShouldSetTheValueToWhatTheEditorAllows_WhenHtmlIsEntered()
    {
        string? notes = null;
        var cut = RenderRichTextEditor(parameters => parameters.Add(editor => editor.Formats, RichTextFormats.Bold)
            .Add(editor => editor.ValueChanged, (string html) => notes = html));

        await cut.Tb().RichTextEditor("Notes").EnterAsync("<p><b>Hi</b> <i>there</i></p>");

        Assert.Equal("<p><strong>Hi</strong> there</p>", notes);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task RichTextEditor_ShouldThrow_WhenTheEditorIsReadOnlyOrDisabled(bool readOnly, bool disabled)
    {
        var cut = RenderRichTextEditor(parameters => parameters.Add(editor => editor.ReadOnly, readOnly)
            .Add(editor => editor.Disabled, disabled));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.Tb().RichTextEditor("Notes").EnterAsync("<p>x</p>"));
    }

    [Fact]
    public async Task Menu_ShouldClickTheItemThroughItsSubmenus_WhenChosenByPath()
    {
        var clicked = new List<string>();
        var cut = RenderMenu(clicked);

        await cut.Tb().Menu("Actions").ChooseAsync("Rename");
        await cut.Tb().Menu("Actions").ChooseAsync("Export", "PDF");
        await cut.Tb().Menu("Actions").ChooseAsync("Show hidden");
        await cut.Tb().Menu("Actions").ChooseAsync("Docs (opens in a new tab)");

        Assert.Equal(["Rename", "PDF", "Show hidden", "Docs"], clicked);
    }

    [Fact]
    public async Task Menu_ShouldFollowALinkItemButStayForANewTab_WhenChosen()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Go to")
            .AddChildContent<TbMenuItem>(item => item.Add(each => each.Href, "help").AddChildContent("Help"))
            .AddChildContent<TbMenuItem>(item => item.Add(each => each.Href, "guide").Add(each => each.NewTab, true)
                .AddChildContent("Guide")));

        await cut.Tb().Menu("Go to").ChooseAsync("Help");
        await cut.Tb().Menu("Go to").ChooseAsync("Guide (opens in a new tab)");

        Assert.EndsWith("/help", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Menu_ShouldOpenAndCloseTheMenuAndTellTheApp_WhenAskedThroughItsDriver(bool attachOnFirstUse)
    {
        var heard = new List<bool>();
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.AttachOnFirstUse, attachOnFirstUse).Add(menu => menu.OpenChanged, heard.Add)
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));
        var menu = cut.Tb().Menu("Actions");

        await menu.OpenAsync();
        var opened = menu.IsOpen;
        await menu.CloseAsync();

        Assert.True(opened);
        Assert.False(menu.IsOpen);
        Assert.Equal([true, false], heard);
    }

    [Fact]
    public async Task Menu_ShouldLeaveAClosedMenuAsItIs_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions"));

        var failure = await Record.ExceptionAsync(() => cut.Tb().Menu("Actions").CloseAsync());

        Assert.Null(failure);
    }

    [Fact]
    public async Task Menu_ShouldCloseADisabledMenu_WhenTheAppOpenedIt()
    {
        var heard = new List<bool>();
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.Disabled, true).Add(menu => menu.Open, true).Add(menu => menu.OpenChanged, heard.Add));

        await cut.Tb().Menu("Actions").CloseAsync();

        Assert.Equal([false], heard);
    }

    [Fact]
    public async Task Menu_ShouldThrowOnOpening_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions"));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Menu("Actions").OpenAsync());

        Assert.Contains("static page", failure.Message);
    }

    [Fact]
    public async Task Menu_ShouldThrowOnOpening_WhenTheMenuIsDisabled()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.Disabled, true));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Menu("Actions").OpenAsync());

        Assert.Contains("is disabled", failure.Message);
    }

    [Theory]
    [InlineData("Actions", "Archive", "is disabled")]
    [InlineData("Actions", "Print", "no item named")]
    [InlineData("Actions", "PDF", "no item named")]
    [InlineData("Actions", "Rename/PDF", "no submenu named")]
    [InlineData("Nope", "Rename", "Expected one menu trigger")]
    public async Task Menu_ShouldThrow_WhenThePathCantBeChosen(string menu, string path, string message)
    {
        var cut = RenderMenu([]);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Menu(menu).ChooseAsync(path.Split('/')));

        Assert.Contains(message, failure.Message);
    }

    [Fact]
    public async Task Menu_ShouldThrow_WhenAButtonItemIsChosenOnAStaticPage()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderMenu([]);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Menu("Actions").ChooseAsync("Rename"));

        Assert.Contains("static page", failure.Message);
        await cut.Tb().Menu("Actions").ChooseAsync("Docs (opens in a new tab)");
    }

    [Fact]
    public async Task Menu_ShouldThrow_WhenTheMenuIsDisabled()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.Disabled, true).AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Menu("Actions").ChooseAsync("Rename"));

        Assert.Contains("is disabled", failure.Message);
    }

    [Fact]
    public async Task ContextMenu_ShouldClickTheItemForTheKeyThroughItsSubmenus_WhenChosenOnAKey()
    {
        var clicked = new List<string>();
        var cut = RenderContextMenu(clicked);

        await cut.Tb().ContextMenu("Trail actions").ChooseAsync("Rename");
        await cut.Tb().ContextMenu().On("ridge").ChooseAsync("Move to", "Archive");
        await cut.Tb().ContextMenu().On("ridge").ChooseAsync("Rename");
        await cut.Tb().ContextMenu().ChooseAsync("Rename");

        Assert.Equal(["Rename all", "Archive ridge", "Rename ridge", "Rename all"], clicked);
    }

    [Fact]
    public async Task ContextMenu_ShouldFollowALinkItem_WhenChosen()
    {
        var cut = Render<TbContextMenu>(parameters => parameters.Add(menu => menu.AriaLabel, "Trail actions")
            .AddChildContent("<p>Ridge loop</p>").Add(menu => menu.Items, _ => builder =>
            {
                builder.OpenComponent<TbMenuItem>(0);
                builder.AddAttribute(1, nameof(TbMenuItem.Href), "trails/ridge");
                builder.AddAttribute(2, nameof(TbMenuItem.ChildContent),
                    (RenderFragment)(text => text.AddContent(0, "Open")));
                builder.CloseComponent();
            }));

        await cut.Tb().ContextMenu().ChooseAsync("Open");

        Assert.EndsWith("/trails/ridge", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Theory]
    [InlineData("Trail actions", "", "Delete", "no item named")]
    [InlineData("Trail actions", "", "Rename/Archive", "no submenu named")]
    [InlineData("Trail actions", "lake", "Rename", "no element with the key \"lake\"")]
    [InlineData("Nope", "", "Rename", "Expected one context menu named \"Nope\"")]
    public async Task ContextMenu_ShouldThrow_WhenThePathCantBeChosen(string name, string key, string path,
        string message)
    {
        var cut = RenderContextMenu([]);
        var menu = cut.Tb().ContextMenu(name);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => (key.Length > 0 ? menu.On(key) : menu).ChooseAsync(path.Split('/')));

        Assert.Contains(message, failure.Message);
    }

    [Theory]
    [InlineData(false, "is disabled")]
    [InlineData(true, "static page")]
    public async Task ContextMenu_ShouldThrow_WhenItIsDisabledOrOnAStaticPage(bool staticPage, string message)
    {
        if (staticPage)
        {
            SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        }

        var cut = RenderContextMenu([], disabled: !staticPage);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().ContextMenu().ChooseAsync("Rename"));

        Assert.Contains(message, failure.Message);
    }

    [Fact]
    public async Task ContextMenu_ShouldAskForANameOrThrow_WhenTheMarkupHasMoreThanOne()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbContextMenu>(0);
            builder.CloseComponent();
            builder.OpenComponent<TbContextMenu>(1);
            builder.CloseComponent();
        });

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().ContextMenu().ChooseAsync("Rename"));

        Assert.Contains("Expected one context menu, found 2", failure.Message);
    }

    private IRenderedComponent<TbContextMenu> RenderContextMenu(List<string> clicked, bool disabled = false)
    {
        return Render<TbContextMenu>(parameters => parameters.Add(menu => menu.AriaLabel, "Trail actions")
            .Add(menu => menu.Disabled, disabled).AddChildContent("<p data-context-key=\"ridge\">Ridge loop</p>")
            .Add(menu => menu.Items, context => builder =>
            {
                var key = context.Key ?? "all";
                ContextItem(builder, "Rename", () => clicked.Add($"Rename {key}"));
                builder.OpenComponent<TbSubMenu>(10);
                builder.AddAttribute(11, nameof(TbSubMenu.Label), "Move to");
                builder.AddAttribute(12, nameof(TbSubMenu.ChildContent), (RenderFragment)(sub =>
                    ContextItem(sub, "Archive", () => clicked.Add($"Archive {key}"))));
                builder.CloseComponent();
            }));
    }

    private void ContextItem(RenderTreeBuilder builder, string text, Action onClick)
    {
        builder.OpenComponent<TbMenuItem>(0);
        builder.AddAttribute(1, nameof(TbMenuItem.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, onClick));
        builder.AddAttribute(2, nameof(TbMenuItem.ChildContent), (RenderFragment)(content => content.AddContent(0,
            text)));
        builder.CloseComponent();
    }

    private IRenderedComponent<TbMenu> RenderMenu(List<string> clicked)
    {
        return Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions").AddChildContent(builder =>
        {
            Item(builder, "Rename");
            builder.OpenComponent<TbMenuItem>(10);
            builder.AddAttribute(11, nameof(TbMenuItem.Disabled), true);
            builder.AddAttribute(12, nameof(TbMenuItem.ChildContent), Text("Archive"));
            builder.CloseComponent();
            builder.OpenComponent<TbSubMenu>(20);
            builder.AddAttribute(21, nameof(TbSubMenu.Label), "Export");
            builder.AddAttribute(22, nameof(TbSubMenu.ChildContent), (RenderFragment)(sub => Item(sub, "PDF")));
            builder.CloseComponent();
            builder.OpenComponent<TbMenuCheckboxItem>(30);
            builder.AddAttribute(31, nameof(TbMenuCheckboxItem.CheckedChanged),
                EventCallback.Factory.Create<bool>(this, () => clicked.Add("Show hidden")));
            builder.AddAttribute(32, nameof(TbMenuCheckboxItem.ChildContent), Text("Show hidden"));
            builder.CloseComponent();
            builder.OpenComponent<TbMenuItem>(40);
            builder.AddAttribute(41, nameof(TbMenuItem.Href), "docs");
            builder.AddAttribute(42, nameof(TbMenuItem.NewTab), true);
            builder.AddAttribute(43, nameof(TbMenuItem.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked.Add("Docs")));
            builder.AddAttribute(44, nameof(TbMenuItem.ChildContent), Text("Docs"));
            builder.CloseComponent();
        }));

        void Item(RenderTreeBuilder builder, string text)
        {
            builder.OpenComponent<TbMenuItem>(0);
            builder.AddAttribute(1, nameof(TbMenuItem.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked.Add(text)));
            builder.AddAttribute(2, nameof(TbMenuItem.ChildContent), Text(text));
            builder.CloseComponent();
        }

        static RenderFragment Text(string text) => content => content.AddContent(0, text);
    }

    [Fact]
    public async Task Tabs_ShouldSelectTheTab_WhenSelectedByItsText()
    {
        var cut = RenderForm();

        await cut.Tb().Tabs().SelectAsync("Route");

        Assert.Equal("Route", cut.Tb().Tabs().Selected);
        Assert.Equal(["Gear", "Route"], cut.Tb().Tabs().Tabs);
    }

    [Fact]
    public async Task Tabs_ShouldFindAFieldInOneTabsPanel_WhenEveryPanelHasOneWithTheSameLabel()
    {
        var notes = new List<string>();
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.RenderAllPanels, true)
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                foreach (var tab in new[] { "Gear", "Route" })
                {
                    builder.OpenComponent<TbTab>(0);
                    builder.AddAttribute(1, nameof(TbTab.Text), tab);
                    builder.AddAttribute(2, nameof(TbTab.ChildContent), (RenderFragment)(panel =>
                    {
                        panel.OpenComponent<TbTextField<string>>(0);
                        panel.AddAttribute(1, nameof(TbTextField<string>.Label), "Notes");
                        panel.AddAttribute(2, nameof(TbTextField<string>.ValueChanged),
                            EventCallback.Factory.Create<string>(this, (string value) => notes.Add($"{tab} {value}")));
                        panel.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            })));

        await cut.Tb().Tabs().Panel("Route").TextField("Notes").EnterAsync("Steep");

        Assert.Equal(["Route Steep"], notes);
    }

    [Fact]
    public async Task Tabs_ShouldHandBackATabsPanel_WhenAskedForItsElement()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            foreach (var (sequence, tab) in new[] { (0, "Gear"), (10, "Route") })
            {
                builder.OpenComponent<TbTab>(sequence);
                builder.AddAttribute(sequence + 1, nameof(TbTab.Text), tab);
                builder.AddAttribute(sequence + 2, nameof(TbTab.ChildContent),
                    (RenderFragment)(panel => panel.AddContent(0, $"{tab} details")));
                builder.CloseComponent();
            }
        })));

        var gear = cut.Tb().Tabs().PanelElement("Gear").TextContent.Trim();
        var unshown = Assert.Throws<InvalidOperationException>(() => cut.Tb().Tabs().PanelElement("Route"));
        await cut.Tb().Tabs().SelectAsync("Route");

        Assert.Equal(("Gear details", "Route details"), (gear, cut.Tb().Tabs().PanelElement("Route").TextContent.Trim()));
        Assert.Contains("isn't in the markup", unshown.Message);
    }

    [Fact]
    public void Tabs_ShouldFindTheOuterList_WhenAnUnnamedListHasTabsNestedInAPanel()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTab>(0);
            builder.AddAttribute(1, nameof(TbTab.Text), "Route");
            builder.AddAttribute(2, nameof(TbTab.ChildContent), (RenderFragment)(panel =>
            {
                panel.OpenComponent<TbTabs>(0);
                panel.AddAttribute(1, nameof(TbTabs.AriaLabel), "Legs");
                panel.AddAttribute(2, nameof(TbTabs.ChildContent), (RenderFragment)(legs =>
                {
                    legs.OpenComponent<TbTab>(0);
                    legs.AddAttribute(1, nameof(TbTab.Text), "North");
                    legs.CloseComponent();
                }));
                panel.CloseComponent();
            }));
            builder.CloseComponent();
        })));

        Assert.Equal(["Route"], cut.Tb().Tabs().Tabs);
        Assert.Equal(["North"], cut.Tb().Tabs("Legs").Tabs);
        Assert.Equal(["North"], cut.Tb().Tabs().Panel("Route").Tabs().Tabs);
    }

    [Fact]
    public async Task Tabs_ShouldThrow_WhenAskedToCloseATabThatIsNotClosable()
    {
        var cut = RenderForm();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Tabs().CloseAsync("Route"));

        Assert.Equal("The tab \"Route\" can't be closed.", error.Message);
    }

    [Fact]
    public async Task Tabs_ShouldClickTheAddButton_WhenScrollButtonsComeBeforeIt()
    {
        var added = 0;
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ScrollButtons, true)
            .Add(tabs => tabs.OnAdd, () => added++)
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTab>(0);
                builder.AddAttribute(1, nameof(TbTab.Text), "Overview");
                builder.CloseComponent();
            })));

        await cut.Tb().Tabs().AddAsync();

        Assert.Equal(1, added);
    }

    [Fact]
    public async Task Tabs_ShouldThrow_WhenAskedToAddToTabsWithNoAddButton()
    {
        var cut = RenderForm();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Tabs().AddAsync());

        Assert.Equal("The tabs have no add button.", error.Message);
    }

    [Fact]
    public async Task Grid_ShouldClickAndSelectRowsByACellsText_WhenTheRowsCanBeSelected()
    {
        Trail? clicked = null;
        IReadOnlyCollection<Trail> selected = [];
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.Items, [new("Pine Hollow"), new("Cedar Flats"), new("Lake camp")])
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple)
            .Add(grid => grid.OnRowClick, (Trail trail) => clicked = trail)
            .Add(grid => grid.SelectedItemsChanged, (IReadOnlyCollection<Trail> trails) => selected = trails)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
                    (System.Linq.Expressions.Expression<Func<Trail, object?>>)(trail => trail.Name));
                builder.CloseComponent();
            })));
        var grid = cut.Tb().Grid("Trails");

        await grid.ClickRowAsync("Cedar Flats");
        await grid.SelectAsync("Lake camp");
        await grid.SelectAsync("Lake camp");

        Assert.Equal("Cedar Flats", clicked?.Name);
        Assert.Equal(["Cedar Flats", "Lake camp"], selected.Select(trail => trail.Name).Order());
        Assert.Equal(3, grid.RowCount);
        Assert.Equal([["Pine Hollow"], ["Cedar Flats"], ["Lake camp"]], grid.Rows);
        Assert.Equal([["Cedar Flats"], ["Lake camp"]], grid.Selected);
        Assert.Throws<InvalidOperationException>(() => grid.Row("Elk Pass"));
        Assert.Throws<InvalidOperationException>(() => cut.Tb().Grid("Camps").Row("Lake camp"));
    }

    [Fact]
    public async Task Grid_ShouldReadBusyUntilTheRowsLoad_WhenItsDataProviderAnswersLater()
    {
        var rows = new TaskCompletionSource<GridResult<Trail>>();
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.DataProvider, (_, _) => rows.Task).Add(grid => grid.ChildContent, NameColumn));
        var grid = cut.Tb().Grid("Trails");
        var busy = grid.IsBusy;

        rows.SetResult(new GridResult<Trail>([new("Pine Hollow"), new("Cedar Flats")], 2));
        await grid.WaitForRowsAsync();

        Assert.True(busy);
        Assert.False(grid.IsBusy);
        Assert.Equal(2, grid.RowCount);
    }

    [Fact]
    public async Task Grid_ShouldSortByAColumnAndFindACell_WhenAskedByTitle()
    {
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.Items,
                [new("Pine Hollow", "North"), new("Cedar Flats", "East"), new("Lake camp", "East")])
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
                    (System.Linq.Expressions.Expression<Func<Trail, object?>>)(trail => trail.Name));
                builder.AddAttribute(2, nameof(TbColumn<Trail>.Sortable), true);
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Trail>>(3);
                builder.AddAttribute(4, nameof(TbColumn<Trail>.Field),
                    (System.Linq.Expressions.Expression<Func<Trail, object?>>)(trail => trail.Region));
                builder.AddAttribute(5, nameof(TbColumn<Trail>.Sortable), true);
                builder.CloseComponent();
            })));
        var grid = cut.Tb().Grid("Trails");
        // Sorting by Name and then Region, with Shift, leaves Name sorted ascending but not alone.
        await cut.FindAll("thead [data-part=sort]")[0].ClickAsync(new());
        await cut.FindAll("thead [data-part=sort]")[1].ClickAsync(new() { ShiftKey = true });

        await grid.SortAsync("Name");

        Assert.Equal(["Cedar Flats", "Lake camp", "Pine Hollow"], cut.FindAll("tbody tr[data-part=row]")
            .Select(row => row.QuerySelector("td")!.TextContent.Trim()));
        Assert.Empty(cut.FindAll("[data-part=sort-order]"));
        Assert.Equal("East", grid.Cell("Lake camp", "Region").TextContent.Trim());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 3)]
    public async Task TextField_ShouldCommitTheText_WhenEntered(bool updateOnInput, int lines)
    {
        var values = new List<string?>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.UpdateOnInput, updateOnInput).Add(field => field.Lines, lines)
            .Add(field => field.ValueChanged, (string value) => values.Add(value)));

        await cut.Tb().TextField("Trail").EnterAsync("Pine Hollow");

        Assert.Equal("Pine Hollow", values.Last());
        Assert.Equal("Pine Hollow", cut.Tb().TextField("Trail").Text);
    }

    [Fact]
    public async Task Within_ShouldDriveTheFieldInTheScope_WhenTheLabelRepeats()
    {
        string? first = null;
        string? second = null;
        var cut = Render(builder =>
        {
            (string Id, Action<string> Set)[] fields =
                [("first", value => first = value), ("second", value => second = value)];
            foreach (var (id, set) in fields)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", id);
                builder.OpenComponent<TbTextField<string>>(2);
                builder.AddAttribute(3, nameof(TbTextField<string>.Label), "Count");
                builder.AddAttribute(4, nameof(TbTextField<string>.ValueChanged),
                    EventCallback.Factory.Create(this, set));
                builder.CloseComponent();
                builder.CloseElement();
            }
        });

        await cut.Tb().Within(() => cut.Find("#second")).TextField("Count").EnterAsync("2");

        Assert.Equal((null, "2"), (first, second));
        Assert.Throws<InvalidOperationException>(() => cut.Tb().TextField("Count").Text);
    }

    [Fact]
    public async Task Within_ShouldChooseFromTheMenuInTheScope_WhenItsTriggerRepeats()
    {
        var chosen = new List<string>();
        var cut = Render(builder =>
        {
            foreach (var row in new[] { "first", "second" })
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", row);
                builder.OpenComponent<TbMenu>(2);
                builder.AddAttribute(3, nameof(TbMenu.Label), "Actions");
                builder.AddAttribute(4, nameof(TbMenu.ChildContent), (RenderFragment)(items =>
                {
                    items.OpenComponent<TbMenuItem>(0);
                    items.AddAttribute(1, nameof(TbMenuItem.ChildContent),
                        (RenderFragment)(text => text.AddContent(0, "Delete")));
                    items.AddAttribute(2, nameof(TbMenuItem.OnClick),
                        EventCallback.Factory.Create<MouseEventArgs>(this, () => chosen.Add(row)));
                    items.CloseComponent();
                }));
                builder.CloseComponent();
                builder.CloseElement();
            }
        });

        await cut.Tb().Within(() => cut.Find("#second")).Menu("Actions").ChooseAsync("Delete");

        Assert.Equal(["second"], chosen);
    }

    [Fact]
    public async Task TextField_ShouldThrow_WhenTheFieldIsReadOnly()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.ReadOnly, true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().TextField("Trail").EnterAsync("Pine"));
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "N0")]
    public async Task NumberField_ShouldThrow_WhenTheFieldIsReadOnlyOrDisabled(bool readOnly, string? format)
    {
        int? copies = 3;
        var cut = Render<TbNumberField<int?>>(parameters => parameters.Add(field => field.Label, "Copies")
            .Add(field => field.Value, copies).Add(field => field.ValueChanged, (int? value) => copies = value)
            .Add(field => field.Format, format).Add(field => field.ReadOnly, readOnly)
            .Add(field => field.Disabled, !readOnly));

        // The driver throws through its task, not when called, so a test can hold the task and await it later.
        var entered = cut.Tb().NumberField("Copies").EnterAsync("12");

        await Assert.ThrowsAsync<InvalidOperationException>(() => entered);
        Assert.Equal(3, copies);
    }

    [Fact]
    public void TextField_ShouldThrow_WhenTheLabelNamesAnotherKindOfField()
    {
        var cut = Render<TbTagInput>(parameters => parameters.Add(field => field.Label, "Features"));

        Assert.Throws<InvalidOperationException>(() => cut.Tb().TextField("Features").Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pagination_ShouldGoToAPageItDoesntShow_WhenAskedForIt(bool compact)
    {
        var page = 1;
        var cut = Render<TbPagination>(parameters => parameters.Add(pager => pager.AriaLabel, "Trail pages")
            .Add(pager => pager.PageCount, 20).Add(pager => pager.Compact, compact)
            .Add(pager => pager.PageChanged, (int number) => page = number));

        await cut.Tb().Pagination("Trail pages").GoToPageAsync(12);

        Assert.Equal(12, page);
        Assert.Equal(12, cut.Tb().Pagination().Page);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cut.Tb().Pagination().GoToPageAsync(21));
    }

    [Theory]
    [InlineData(20, 3, 1)]
    [InlineData(1, 12, 0)]
    [InlineData(20, 12, 0)]
    public async Task Pagination_ShouldStepThroughThePagesItShows_WhenTheTargetIsFarAway(int from, int to, int siblings)
    {
        var cut = Render<TbPagination>(parameters => parameters.Add(pager => pager.PageCount, 20)
            .Add(pager => pager.Page, from).Add(pager => pager.SiblingCount, siblings));

        await cut.Tb().Pagination().GoToPageAsync(to);

        Assert.Equal(to, cut.Tb().Pagination().Page);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pagination_ShouldThrow_WhenItIsDisabled(bool compact)
    {
        var page = 1;
        var cut = Render<TbPagination>(parameters => parameters.Add(pager => pager.PageCount, 20)
            .Add(pager => pager.Disabled, true).Add(pager => pager.Compact, compact)
            .Add(pager => pager.PageChanged, (int number) => page = number));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Pagination().GoToPageAsync(12));
        Assert.Equal(1, page);
    }

    [Fact]
    public async Task Grid_ShouldGoToAPage_WhenItHasAPager()
    {
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.Items, [new("Pine Hollow"), new("Cedar Flats"), new("Lake camp")])
            .Add(grid => grid.PageSize, 1)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
                    (System.Linq.Expressions.Expression<Func<Trail, object?>>)(trail => trail.Name));
                builder.CloseComponent();
            })));

        await cut.Tb().Grid("Trails").GoToPageAsync(3);

        Assert.Equal("Lake camp", cut.Find("tbody tr[data-part=row]").TextContent.Trim());
    }

    [Fact]
    public async Task Sortable_ShouldMoveTheItemAPlaceAtATimeThroughItsMenu_WhenMovedToAPlace()
    {
        List<string> rules = ["Block spam", "Flag invoices", "Star the team", "Catch-all"];
        var moves = new List<int>();
        var cut = RenderSortable(rules, move =>
        {
            moves.Add(move.To);
            move.ApplyTo(rules);
        });

        await cut.Tb().Sortable("Rules").MoveAsync("Block spam", 2);
        Assert.Equal(["Flag invoices", "Star the team", "Block spam", "Catch-all"], cut.Tb().Sortable("Rules").Items);
        await cut.Tb().Sortable("Rules").MoveAsync("Catch-all", 0);

        Assert.Equal(["Catch-all", "Flag invoices", "Star the team", "Block spam"], cut.Tb().Sortable().Items);
        await cut.Tb().Sortable("Rules").MoveAsync("Flag invoices", 3);

        Assert.Equal(["Catch-all", "Star the team", "Block spam", "Flag invoices"], cut.Tb().Sortable().Items);
        Assert.Equal([1, 2, 0, 3], moves);
    }

    [Fact]
    public async Task Sortable_ShouldFindAnItemByItsLabel_WhenItHoldsAField()
    {
        List<string> steps = ["Tent", "Stove"];
        var cut = Render<TbSortable<string>>(parameters => parameters.Add(sortable => sortable.Label, "Packing steps")
            .Add(sortable => sortable.Items, steps).Add(sortable => sortable.ItemLabel, step => step)
            .Add(sortable => sortable.OnMove, (SortableMove<string> move) => move.ApplyTo(steps))
            .Add(sortable => sortable.ItemTemplate, step => builder =>
            {
                builder.AddContent(0, step);
                builder.OpenComponent<TbTextField<string>>(1);
                builder.AddAttribute(2, nameof(TbTextField<string>.Label), $"Note for {step}");
                builder.CloseComponent();
            }));

        await cut.Tb().Sortable("Packing steps").MoveAsync("Stove", 0);

        Assert.Equal(["Stove", "Tent"], steps);
        Assert.Equal(["Stove", "Tent"], cut.Tb().Sortable("Packing steps").Items);
    }

    [Fact]
    public async Task Sortable_ShouldStopAtTheStepTheAppRefuses_WhenMovingAnItem()
    {
        List<string> rules = ["Block spam", "Flag invoices", "Star the team", "Catch-all"];
        var cut = RenderSortable(rules, move => move.ApplyTo(rules), move => Task.FromResult(
            move.To == 2 ? MoveCheck.Refuse("The third place is taken.") : MoveCheck.Allow));

        await cut.Tb().Sortable("Rules").MoveAsync("Block spam", 2);

        Assert.Equal(["Flag invoices", "Block spam", "Star the team", "Catch-all"], cut.Tb().Sortable("Rules").Items);
    }

    [Fact]
    public async Task Sortable_ShouldThrow_WhenMovingPastTheEndOrInADisabledList()
    {
        List<string> rules = ["Block spam", "Flag invoices"];
        var cut = RenderSortable(rules, move => move.ApplyTo(rules));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => cut.Tb().Sortable("Rules").MoveAsync("Block spam", 2));
        cut.Render(parameters => parameters.Add(sortable => sortable.Disabled, true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Sortable("Rules").MoveAsync("Block spam", 1));
        Assert.Equal(["Block spam", "Flag invoices"], rules);
    }

    [Fact]
    public async Task Sortable_ShouldMoveTheItemToTheEndOfAnotherListOrThrow_WhenMovedToAListOfItsGroup()
    {
        List<string> todo = ["Pack food", "Check weather"];
        List<string> done = ["Book campsite"];
        var cut = Render(builder =>
        {
            Column(builder, 0, "To do", todo);
            Column(builder, 10, "Done", done);
        });

        await cut.Tb().Sortable("To do").MoveToListAsync("Check weather", "Done");

        // The handler isn't a component's, so nothing redraws the lists; the app's data shows the move.
        Assert.Equal(["Book campsite", "Check weather"], done);
        Assert.Equal(["Pack food"], todo);
        // The driver throws through its task, not when called, so a test can hold the task and await it later.
        var refused = cut.Tb().Sortable("To do").MoveToListAsync("Pack food", "Archive");
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => refused);
        Assert.Contains("no other list in its group has that label", failure.Message);

        void Column(RenderTreeBuilder builder, int sequence, string label, List<string> tasks)
        {
            builder.OpenComponent<TbSortable<string>>(sequence);
            builder.AddComponentParameter(sequence + 1, nameof(TbSortable<string>.Items), tasks);
            builder.AddComponentParameter(sequence + 2, nameof(TbSortable<string>.Label), label);
            builder.AddComponentParameter(sequence + 3, nameof(TbSortable<string>.Group), "board");
            builder.AddComponentParameter(sequence + 4, nameof(TbSortable<string>.ListId), label);
            builder.AddComponentParameter(sequence + 5, nameof(TbSortable<string>.ItemTemplate),
                (RenderFragment<string>)(task => content => content.AddContent(0, task)));
            builder.AddComponentParameter(sequence + 6, nameof(TbSortable<string>.OnMove),
                EventCallback.Factory.Create<SortableMove<string>>(this, move =>
                    move.ApplyBetween(move.FromList == "To do" ? todo : done, move.ToList == "To do" ? todo : done)));
            builder.CloseComponent();
        }
    }

    [Fact]
    public async Task Sortable_ShouldAskAndMoveTheItemWhereItsDropped_WhenDraggedBeforeOrAfterAnother()
    {
        List<string> rules = ["Block spam", "Flag invoices", "Star the team", "Catch-all"];
        var asked = new List<string>();
        var cut = RenderSortable(rules, move => move.ApplyTo(rules), move =>
        {
            asked.Add(move.Item);
            return Task.FromResult(MoveCheck.Allow);
        });

        await cut.Tb().Sortable("Rules").DragBeforeAsync("Catch-all", "Block spam");
        await cut.Tb().Sortable("Rules").DragAfterAsync("Catch-all", "Star the team");

        // Each drag is asked about as it hovers and again as it drops.
        Assert.Equal(["Block spam", "Flag invoices", "Star the team", "Catch-all"], rules);
        Assert.Equal(["Catch-all", "Catch-all", "Catch-all", "Catch-all"], asked);
    }

    [Fact]
    public async Task Sortable_ShouldDragInTheListShownNow_WhenAnEarlierListWithItsNameIsGone()
    {
        RenderSortable(["Block spam", "Flag invoices"], _ => { });
        await DisposeComponentsAsync();
        List<string> rules = ["Block spam", "Flag invoices"];

        var cut = RenderSortable(rules, move => move.ApplyTo(rules));
        await cut.Tb().Sortable("Rules").DragBeforeAsync("Flag invoices", "Block spam");

        Assert.Equal(["Flag invoices", "Block spam"], rules);
    }

    [Fact]
    public async Task Sortable_ShouldAskForAnotherName_WhenDraggingInAListWhoseNameAnotherListShares()
    {
        var cut = Render(builder =>
        {
            foreach (var card in new[] { 0, 10 })
            {
                builder.OpenElement(card, "section");
                builder.OpenComponent<TbSortable<string>>(card + 1);
                builder.AddComponentParameter(card + 2, nameof(TbSortable<string>.Label), "Tags");
                builder.AddComponentParameter(card + 3, nameof(TbSortable<string>.Items), new[] { "Lake", "Ridge" });
                builder.AddComponentParameter(card + 4, nameof(TbSortable<string>.ItemTemplate),
                    (RenderFragment<string>)(tag => content => content.AddContent(0, tag)));
                builder.AddComponentParameter(card + 5, nameof(TbSortable<string>.OnMove),
                    EventCallback.Factory.Create<SortableMove<string>>(this, _ => { }));
                builder.CloseComponent();
                builder.CloseElement();
            }
        });

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb()
            .Within(() => cut.FindAll("section")[0]).Sortable("Tags").DragBeforeAsync("Ridge", "Lake"));

        Assert.Contains("give each its own name", failure.Message);
    }

    [Fact]
    public async Task Sortable_ShouldThrow_WhenDraggingInADisabledList()
    {
        List<string> rules = ["Block spam", "Flag invoices"];
        var cut = RenderSortable(rules, move => move.ApplyTo(rules));
        cut.Render(parameters => parameters.Add(sortable => sortable.Disabled, true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Sortable("Rules").DragBeforeAsync("Flag invoices", "Block spam"));

        Assert.Equal(["Block spam", "Flag invoices"], rules);
    }

    [Fact]
    public async Task Sortable_ShouldDropTheItemInAnotherListOrThrow_WhenDraggedToAListOfItsGroup()
    {
        List<string> todo = ["Pack food", "Check weather", "Fill water"];
        List<string> done = ["Book campsite"];
        var cut = Render(builder =>
        {
            Column(builder, 0, "To do", todo);
            Column(builder, 10, "Done", done);
        });

        await cut.Tb().Sortable("To do").DragToListAsync("Check weather", "Done");
        await cut.Tb().Sortable("To do").DragToListAsync("Pack food", "Done", before: "Book campsite");

        Assert.Equal(["Pack food", "Book campsite", "Check weather"], done);
        Assert.Equal(["Fill water"], todo);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Sortable("To do").DragToListAsync("Fill water", "Archive"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Sortable("To do").DragToListAsync("Fill water", "To do"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Sortable("To do").DragToListAsync("Fill water", "Done", before: "Sleep"));

        void Column(RenderTreeBuilder builder, int sequence, string label, List<string> tasks)
        {
            builder.OpenComponent<TbSortable<string>>(sequence);
            builder.AddComponentParameter(sequence + 1, nameof(TbSortable<string>.Items), tasks);
            builder.AddComponentParameter(sequence + 2, nameof(TbSortable<string>.Label), label);
            builder.AddComponentParameter(sequence + 3, nameof(TbSortable<string>.Group), "board");
            builder.AddComponentParameter(sequence + 4, nameof(TbSortable<string>.ListId), label);
            builder.AddComponentParameter(sequence + 5, nameof(TbSortable<string>.ItemTemplate),
                (RenderFragment<string>)(task => content => content.AddContent(0, task)));
            builder.AddComponentParameter(sequence + 6, nameof(TbSortable<string>.OnMove),
                EventCallback.Factory.Create<SortableMove<string>>(this, move =>
                    move.ApplyBetween(move.FromList == "To do" ? todo : done, move.ToList == "To do" ? todo : done)));
            builder.CloseComponent();
        }
    }

    [Fact]
    public async Task Grid_ShouldMoveARowWhereItsDropped_WhenDraggedBeforeOrAfterAnother()
    {
        List<Trail> trails = [new("Pine Hollow"), new("Cedar Flats"), new("Lake camp")];
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.Items, trails)
            .Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails))
            .Add(grid => grid.ChildContent, NameColumn));

        await cut.Tb().Grid("Trails").DragRowBeforeAsync("Lake camp", "Pine Hollow");
        Assert.Equal(["Lake camp", "Pine Hollow", "Cedar Flats"], trails.Select(trail => trail.Name));
        await cut.Tb().Grid("Trails").DragRowAfterAsync("Lake camp", "Cedar Flats");

        Assert.Equal(["Pine Hollow", "Cedar Flats", "Lake camp"], trails.Select(trail => trail.Name));
    }

    [Fact]
    public async Task Grid_ShouldMoveARowAcrossPages_WhenMovedToAPlaceAmongAllTheRows()
    {
        List<Trail> trails = [new("Pine Hollow"), new("Cedar Flats"), new("Lake camp")];
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.Items, trails).Add(grid => grid.PageSize, 2)
            .Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails))
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
                    (System.Linq.Expressions.Expression<Func<Trail, object?>>)(trail => trail.Name));
                builder.CloseComponent();
            })));

        await cut.Tb().Grid("Trails").MoveRowAsync("Pine Hollow", 2);
        Assert.Equal(["Cedar Flats", "Lake camp", "Pine Hollow"], trails.Select(trail => trail.Name));
        Assert.Equal(2, cut.Tb().Pagination().Page);
        await cut.Tb().Grid("Trails").GoToPageAsync(1);
        await cut.Tb().Grid("Trails").MoveRowAsync("Lake camp", 0);

        Assert.Equal(["Lake camp", "Cedar Flats", "Pine Hollow"], trails.Select(trail => trail.Name));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => cut.Tb().Grid("Trails").MoveRowAsync("Lake camp", 3));
    }

    [Fact]
    public async Task Grid_ShouldThrow_WhenMovingARowTheGridCantMove()
    {
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.Items, [new("Pine Hollow"), new("Cedar Flats")])
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
                    (System.Linq.Expressions.Expression<Func<Trail, object?>>)(trail => trail.Name));
                builder.CloseComponent();
            })));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Grid("Trails").MoveRowAsync("Pine Hollow", 1));
    }

    [Fact]
    public async Task DatePicker_ShouldTypeTheDateInTheFieldsFormat_WhenChosen()
    {
        DateOnly? value = null;
        var cut = Render<TbDatePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Label, "Start")
            .Add(picker => picker.ValueChanged, (DateOnly? date) => value = date));

        await cut.Tb().DatePicker("Start").ChooseAsync(new DateOnly(2026, 3, 14));
        Assert.Equal(new DateOnly(2026, 3, 14), value);
        Assert.Equal(new DateOnly(2026, 3, 14), cut.Tb().DatePicker("Start").Value);
        await cut.Tb().DatePicker("Start").ChooseAsync(null);

        Assert.Null(value);
    }

    [Fact]
    public async Task DateTimePicker_ShouldTypeTheDateAndTime_WhenChosen()
    {
        DateTime? value = null;
        var cut = Render<TbDateTimePicker<DateTime>>(parameters => parameters.Add(picker => picker.Label, "Departs")
            .Add(picker => picker.ValueChanged, (DateTime? chosen) => value = chosen));

        await cut.Tb().DateTimePicker("Departs").ChooseAsync(new DateTime(2026, 3, 14, 9, 30, 0));

        Assert.Equal(new DateTime(2026, 3, 14, 9, 30, 0), value);
        Assert.Equal(new DateTime(2026, 3, 14, 9, 30, 0), cut.Tb().DateTimePicker("Departs").Value);
    }

    [Fact]
    public async Task DateRangePicker_ShouldTypeBothDates_WhenChosen()
    {
        var cut = Render<TbDateRangePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Label, "Trip"));

        await cut.Tb().DateRangePicker("Trip").ChooseAsync(new DateOnly(2026, 3, 14), new DateOnly(2026, 3, 20));

        Assert.Equal((new DateOnly(2026, 3, 14), new DateOnly(2026, 3, 20)), cut.Tb().DateRangePicker("Trip").Range);
    }

    [Fact]
    public async Task DatePickers_ShouldShowTheFieldsError_WhenTextTheyCantReadIsEntered()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbDatePicker<DateOnly>>(0);
            builder.AddAttribute(1, nameof(TbDatePicker<DateOnly>.Label), "Start");
            builder.CloseComponent();
            builder.OpenComponent<TbDateTimePicker<DateTime>>(2);
            builder.AddAttribute(3, nameof(TbDateTimePicker<DateTime>.Label), "Departs");
            builder.CloseComponent();
            builder.OpenComponent<TbDateRangePicker<DateOnly>>(4);
            builder.AddAttribute(5, nameof(TbDateRangePicker<DateOnly>.Label), "Trip");
            builder.CloseComponent();
        });

        var time = cut.Find("[data-part=time-input]").GetAttribute("value");
        await cut.Tb().DatePicker("Start").EnterAsync("not a date");
        await cut.Tb().DateTimePicker("Departs").EnterAsync("soon");
        await cut.Tb().DateRangePicker("Trip").EnterAsync("someday", null);

        Assert.All(new TbFieldDriver[] { cut.Tb().DatePicker("Start"), cut.Tb().DateTimePicker("Departs"),
            cut.Tb().DateRangePicker("Trip") }, field => Assert.True(field.Invalid));
        Assert.Equal("not a date", cut.Find("[data-part=input]").GetAttribute("value"));
        Assert.Equal("soon", cut.Find("[data-part=date-input]").GetAttribute("value"));
        Assert.Equal(time, cut.Find("[data-part=time-input]").GetAttribute("value"));
    }

    [Fact]
    public async Task DateRangePicker_ShouldLeaveABoxAsItIs_WhenItsTextIsNull()
    {
        var cut = Render<TbDateRangePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Label, "Trip"));
        await cut.Tb().DateRangePicker("Trip").ChooseAsync(new DateOnly(2026, 3, 14), new DateOnly(2026, 3, 20));
        var start = cut.Find("[data-part=start-input]").GetAttribute("value");

        await cut.Tb().DateRangePicker("Trip").EnterAsync(null, "someday");

        Assert.Equal(start, cut.Find("[data-part=start-input]").GetAttribute("value"));
        Assert.Equal("someday", cut.Find("[data-part=end-input]").GetAttribute("value"));
    }

    [Fact]
    public async Task DatePickers_ShouldTypeInTheFieldsCultureAndCalendar_WhenItsCultureIsGiven()
    {
        var thai = CultureInfo.GetCultureInfo("th-TH");
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbDatePicker<DateOnly>>(0);
            builder.AddAttribute(1, "Label", "Start");
            builder.AddAttribute(2, "Culture", thai);
            builder.CloseComponent();
            builder.OpenComponent<TbDateTimePicker<DateTime>>(3);
            builder.AddAttribute(4, "Label", "Departs");
            builder.AddAttribute(5, "Culture", thai);
            builder.CloseComponent();
            builder.OpenComponent<TbDateRangePicker<DateOnly>>(6);
            builder.AddAttribute(7, "Label", "Trip");
            builder.AddAttribute(8, "Culture", thai);
            builder.CloseComponent();
        });

        await cut.Tb().DatePicker("Start").ChooseAsync(new DateOnly(2026, 9, 14));
        await cut.Tb().DateTimePicker("Departs").ChooseAsync(new DateTime(2026, 9, 14, 9, 30, 0));
        await cut.Tb().DateRangePicker("Trip").ChooseAsync(new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 20));

        // The Thai culture's own calendar is Buddhist, where 2026 is 2569, so a field reading dates in it would differ.
        Assert.Equal(new DateOnly(2026, 9, 14), cut.Tb().DatePicker("Start").Value);
        Assert.Equal(new DateTime(2026, 9, 14, 9, 30, 0), cut.Tb().DateTimePicker("Departs").Value);
        Assert.Equal((new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 20)), cut.Tb().DateRangePicker("Trip").Range);
        Assert.Contains("2026", cut.Find("[data-part=input]").GetAttribute("value"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DatePickers_ShouldReadButNotChoose_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbDatePicker<DateOnly>>(0);
            builder.AddAttribute(1, "Label", "Start");
            builder.AddAttribute(2, "Value", (DateOnly?)new DateOnly(2026, 3, 14));
            builder.CloseComponent();
            builder.OpenComponent<TbDateTimePicker<DateTime>>(3);
            builder.AddAttribute(4, "Label", "Departs");
            builder.AddAttribute(5, "Value", (DateTime?)new DateTime(2026, 3, 14, 9, 30, 0));
            builder.CloseComponent();
        });

        Assert.Equal(new DateOnly(2026, 3, 14), cut.Tb().DatePicker("Start").Value);
        Assert.Equal(new DateTime(2026, 3, 14, 9, 30, 0), cut.Tb().DateTimePicker("Departs").Value);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().DatePicker("Start").ChooseAsync(new DateOnly(2026, 3, 15)));
        Assert.Contains("static page", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DatePicker_ShouldThrow_WhenTheFieldIsReadOnly()
    {
        var cut = Render<TbDatePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Label, "Start")
            .Add(picker => picker.ReadOnly, true));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().DatePicker("Start").ChooseAsync(new DateOnly(2026, 3, 14)));
        Assert.Contains("read-only", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccordionItem_ShouldOpenAndCloseTheItem_WhenAskedByItsHeader()
    {
        var expanded = false;
        var cut = Render<TbAccordionItem>(parameters => parameters.Add(item => item.Title, "Gear")
            .Add(item => item.ExpandedChanged, (bool open) => expanded = open).AddChildContent("Tent and stove"));

        var panel = await cut.Tb().AccordionItem("Gear").ExpandAsync();
        Assert.True(expanded);
        Assert.Contains("Tent and stove", panel.TextContent, StringComparison.Ordinal);
        await cut.Tb().AccordionItem("Gear").CollapseAsync();

        Assert.False(expanded);
    }

    [Fact]
    public async Task AccordionItem_ShouldThrow_WhenTheClosedItemIsDisabled()
    {
        var cut = Render<TbAccordionItem>(parameters => parameters.Add(item => item.Title, "Gear")
            .Add(item => item.Disabled, true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().AccordionItem("Gear").ExpandAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Popover_ShouldOpenAndCloseABoundPopover_WhenTheHelperIsUsed(bool attachOnFirstUse)
    {
        var opens = new List<bool>();
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.AttachOnFirstUse, attachOnFirstUse)
            .Add(popover => popover.OpenChanged, (bool open) => opens.Add(open))
            .AddChildContent("Trail filters"));

        var panel = await cut.Tb().Popover("Filters").OpenAsync();
        await cut.Tb().Popover("Filters").CloseAsync();

        Assert.Equal("Trail filters", panel.TextContent.Trim());
        Assert.Equal([true, false], opens);
    }

    [Fact]
    public async Task Popover_ShouldHandBackThePanel_WhenNothingIsBoundToOpen()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .AddChildContent("Trail filters"));

        var panel = await cut.Tb().Popover("Filters").OpenAsync();
        await cut.Tb().Popover("Filters").CloseAsync();

        Assert.Equal("Trail filters", panel.TextContent.Trim());
    }

    [Fact]
    public void Popover_ShouldReadThePanelWithoutOpeningIt_WhenItIsClosed()
    {
        var opens = new List<bool>();
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.OpenChanged, (bool open) => opens.Add(open)).AddChildContent("Trail filters"));

        var panel = cut.Tb().Popover("Filters").Panel;

        Assert.Equal("Trail filters", panel.TextContent.Trim());
        Assert.Empty(opens);
    }

    [Fact]
    public async Task Popover_ShouldRefuseToOpen_WhenItIsDisabledOrNotThere()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.Disabled, true));

        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Popover("Filters").OpenAsync());
        var missing = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Popover("Layers").OpenAsync());

        Assert.Contains("disabled", disabled.Message);
        Assert.Contains("No popover's trigger", missing.Message);
    }

    [Fact]
    public async Task Toasts_ShouldReturnTheNewestToastWithTheText_WhenItShows()
    {
        var cut = Render<TbRoot>();
        var toasts = Services.GetRequiredService<IToastService>();

        await cut.InvokeAsync(() => toasts.Show("Report archived", Tone.Success));
        await cut.InvokeAsync(() => toasts.Show("Trail saved"));
        await cut.InvokeAsync(() => toasts.Show("Report \n ARCHIVED again"));
        // As in Playwright, case and runs of whitespace, in the toast or the text looked for, don't matter.
        var toast = await cut.Tb().Toasts.WaitForAsync("report  archived");

        Assert.Contains("ARCHIVED again", toast.TextContent);
        await Assert.ThrowsAsync<Bunit.Extensions.WaitForHelpers.WaitForFailedException>(
            () => cut.Tb().Toasts.WaitForAsync("Trail deleted"));
    }

    [Fact]
    public async Task Toasts_ShouldDismissOneToastAndTakeAnothersAction_WhenTheHelperIsUsed()
    {
        var cut = Render<TbRoot>();
        var toasts = Services.GetRequiredService<IToastService>();
        var undone = 0;
        var saved = await cut.InvokeAsync(() => toasts.Show("Trail saved", options: new()
            { Action = new("Share", EventCallback.Factory.Create(this, () => undone += 10)) }));
        var deleted = await cut.InvokeAsync(() => toasts.Show("Trail deleted", options: new()
            { Action = new("Undo", EventCallback.Factory.Create(this, () => undone++)) }));

        await cut.Tb().Toasts.DismissAsync("Trail saved");
        var wrong = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Toasts.ChooseAsync("Trail deleted", "Restore"));
        await cut.Tb().Toasts.ChooseAsync("Trail deleted", "Undo");

        Assert.Equal((ToastCloseReason.CloseButton, ToastCloseReason.Action, 1),
            (await saved.Closed, await deleted.Closed, undone));
        Assert.Equal("The toast showing \"Trail deleted\" has no action named \"Restore\".", wrong.Message);
    }

    [Fact]
    public async Task Toasts_ShouldPassOverAToastThatIsLeaving_WhenItShowsTheText()
    {
        // The app's clock stands still, so a closed toast stays in the markup, fading, until the test ends.
        Services.AddSingleton<TimeProvider>(new Microsoft.Extensions.Time.Testing.FakeTimeProvider());
        var cut = Render<TbRoot>();
        var toasts = Services.GetRequiredService<IToastService>();

        await cut.InvokeAsync(() => toasts.Show("Report archived"));
        var newer = await cut.InvokeAsync(() => toasts.Show("Report archived again"));
        await cut.InvokeAsync(newer.Dismiss);
        var toast = await cut.Tb().Toasts.WaitForAsync("Report archived");

        Assert.Single(cut.FindAll("[data-part=toast][data-leaving]"));
        Assert.DoesNotContain("again", toast.TextContent);
    }

    [Fact]
    public async Task TimePicker_ShouldChooseReadAndClearTheTime_WhenTheHelperIsUsed()
    {
        TimeOnly? start = new(8, 30);
        var cut = Render<TbTimePicker<TimeOnly>>(parameters => parameters.Add(field => field.Label, "Start time")
            .Add(field => field.Value, start).Add(field => field.ValueChanged, (TimeOnly? time) => start = time)
            .Add(field => field.Step, TimeSpan.FromSeconds(1)));
        var picker = cut.Tb().TimePicker("Start time");

        await picker.ChooseAsync(new TimeOnly(14, 45));
        var chosen = (start, picker.Value);
        await picker.ChooseAsync(new TimeOnly(14, 45, 30));
        var withSeconds = (start, picker.Value);
        await picker.ChooseAsync(null);

        Assert.Equal((new TimeOnly(14, 45), new TimeOnly(14, 45)), chosen);
        Assert.Equal((new TimeOnly(14, 45, 30), new TimeOnly(14, 45, 30)), withSeconds);
        Assert.Equal((null, null), (start, picker.Value));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task TimePicker_ShouldRefuseATime_WhenItIsReadOnlyDisabledOrStatic(bool readOnly, bool disabled,
        bool isStatic)
    {
        if (isStatic)
        {
            SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        }

        var cut = Render<TbTimePicker<TimeOnly>>(parameters => parameters.Add(field => field.Label, "Gates open")
            .Add(field => field.ReadOnly, readOnly).Add(field => field.Disabled, disabled));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().TimePicker("Gates open").ChooseAsync(new TimeOnly(6, 0)));
    }

    [Fact]
    public async Task Alert_ShouldReadAndCloseTheAlert_WhenTheHelperIsUsed()
    {
        var closes = 0;
        var cut = Render<TbAlert>(parameters => parameters.Add(alert => alert.Title, "Trail closed")
            .Add(alert => alert.OnClose, () => closes++).AddChildContent("Pine  Hollow is closed for repairs."));

        var alert = cut.Tb().Alert("pine hollow");
        var text = alert.Text;
        await alert.CloseAsync();

        Assert.Equal(("Pine Hollow is closed for repairs.", 1), (text, closes));
    }

    [Fact]
    public void Alerts_ShouldReadEveryAlertsMessageInOrder_WhenThePageShowsSeveral()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbAlert>(0);
            builder.AddComponentParameter(1, nameof(TbAlert.Title), "Trail closed");
            builder.AddComponentParameter(2, nameof(TbAlert.ChildContent), (RenderFragment)(text =>
                text.AddContent(0, "Pine  Hollow is closed.")));
            builder.CloseComponent();
            builder.OpenComponent<TbAlert>(3);
            builder.AddComponentParameter(4, nameof(TbAlert.ChildContent), (RenderFragment)(text =>
                text.AddContent(0, "Bring water.")));
            builder.CloseComponent();
        });

        Assert.Equal(["Pine Hollow is closed.", "Bring water."], cut.Tb().Alerts);
    }

    [Fact]
    public async Task Banner_ShouldReadAndCloseTheBanner_WhenFoundByItsTitle()
    {
        var closes = 0;
        var cut = Render<TbBanner>(parameters => parameters.Add(banner => banner.Title, "Maintenance tonight")
            .Add(banner => banner.OnClose, () => closes++).AddChildContent("Trail maps are offline."));

        var banner = cut.Tb().Banner("Maintenance tonight");
        var text = banner.Text;
        await banner.CloseAsync();

        Assert.Equal(("Trail maps are offline.", 1), (text, closes));
    }

    [Fact]
    public async Task Notices_ShouldRefuseToClose_WhenThereIsNoCloseButtonOrThePageIsStatic()
    {
        var open = Render<TbAlert>(parameters => parameters.AddChildContent("Pine Hollow is closed."));
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var still = Render<TbBanner>(parameters => parameters.Add(banner => banner.AriaLabel, "Notice")
            .Add(banner => banner.OnClose, () => { }).AddChildContent("Trail maps are offline."));

        var none = await Assert.ThrowsAsync<InvalidOperationException>(() => open.Tb().Alert("Pine").CloseAsync());
        var stuck = await Assert.ThrowsAsync<InvalidOperationException>(() => still.Tb().Banner("Notice").CloseAsync());

        Assert.Contains("OnClose", none.Message);
        Assert.Contains("static page", stuck.Message);
    }

    [Fact]
    public async Task Chat_ShouldSendWithAFileAndReadTheMessages_WhenTheHelperIsUsed()
    {
        ChatSend? sent = null;
        var robin = new ChatAuthor("robin", "Robin");
        var cut = Render<TbChat>(parameters => parameters.Add(chat => chat.AriaLabel, "Trip planning")
            .Add(chat => chat.Messages, [new ChatMessage("1", robin, "Bring water", DateTimeOffset.Now)])
            .Add(chat => chat.TypingAuthors, [robin]).Add(chat => chat.AllowAttachments, true)
            .Add(chat => chat.OnSend, (ChatSend send) => sent = send));
        var chat = cut.Tb().Chat("Trip planning");

        await chat.AttachAsync(InputFileContent.CreateFromText("Day one", "route.txt"));
        await chat.SendAsync("Here's the route");

        Assert.Equal(("Here's the route", "route.txt"), (sent!.Text, sent.Files.Single().Name));
        Assert.Equal(["Robin: Bring water"], chat.Messages);
        Assert.Equal("Robin is typing…", chat.Typing);
    }

    [Fact]
    public async Task Chat_ShouldRefuseToSendOrAttach_WhenTheChatCantTakeIt()
    {
        var cut = Render<TbChat>(parameters => parameters.Add(chat => chat.AriaLabel, "Trip planning")
            .Add(chat => chat.Disabled, true));
        var reading = Render<TbChat>(parameters => parameters.Add(chat => chat.AriaLabel, "Trail log")
            .Add(chat => chat.ShowComposer, false));

        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Chat().SendAsync("Hi"));
        var noComposer = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reading.Tb().Chat().SendAsync("Hi"));
        var noFiles = await Assert.ThrowsAsync<InvalidOperationException>(() => Render<TbChat>(parameters =>
            parameters.Add(chat => chat.AriaLabel, "Trail notes")).Tb().Chat().AttachAsync("route.txt"));

        Assert.Equal(("This chat is disabled.", "This chat has no composer; turn ShowComposer on.",
            "This chat doesn't take files; turn AllowAttachments on."),
            (disabled.Message, noComposer.Message, noFiles.Message));
    }

    [Fact]
    public async Task Carousel_ShouldStepAndJumpBetweenSlides_WhenTheHelperIsUsed()
    {
        var shown = new List<int>();
        var cut = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .Add(carousel => carousel.ActiveIndexChanged, (int index) => shown.Add(index))
            .AddChildContent<TbCarouselSlide>(slide => slide.AddChildContent("Pine Hollow"))
            .AddChildContent<TbCarouselSlide>(slide => slide.AddChildContent("Cedar Flats"))
            .AddChildContent<TbCarouselSlide>(slide => slide.AddChildContent("Eagle Ridge")));
        var carousel = cut.Tb().Carousel("Featured trails");

        await carousel.NextAsync();
        await carousel.GoToAsync(2);
        await carousel.NextAsync();
        await carousel.PreviousAsync();

        // From the last slide, Next loops back to the first, and Previous goes back to the last.
        Assert.Equal([1, 2, 0, 2], shown);
        Assert.Equal((3, 2), (carousel.Count, carousel.ActiveIndex));
    }

    private IRenderedComponent<TbVirtualList<string>> RenderReports() => Render<TbVirtualList<string>>(parameters =>
        parameters.Add(list => list.AriaLabel, "Trail reports")
            .Add(list => list.Items, Enumerable.Range(1, 1000).Select(number => $"Report {number}").ToArray())
            .Add(list => list.ChildContent, report => builder => builder.AddContent(0, report)));

    [Fact]
    public void VirtualList_ShouldReadTheCountAndTheDrawnItems_WhenTheHelperIsUsed()
    {
        var cut = RenderReports();
        var list = cut.Tb().VirtualList();

        var count = list.Count;
        var second = list.Item(1).TextContent;
        var far = Assert.Throws<InvalidOperationException>(() => list.Item(999));
        var past = Assert.Throws<ArgumentOutOfRangeException>(() => list.Item(1000));
        var misnamed = Assert.Throws<InvalidOperationException>(() => cut.Tb().VirtualList("Trail notes").Count);

        Assert.Equal((1000, "Report 2"), (count, second));
        Assert.Contains("Playwright", far.Message);
        Assert.Equal("index", past.ParamName);
        Assert.Equal("No virtual list is named \"Trail notes\".", misnamed.Message);
    }

    [Fact]
    public void VirtualList_ShouldReachEveryItem_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderReports();

        Assert.Equal("Report 1000", cut.Tb().VirtualList("Trail reports").Item(999).TextContent);
    }

    [Fact]
    public async Task Carousel_ShouldRefuseAMove_WhenItDoesntLoopHasNoPickerOrIsStatic()
    {
        var cut = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Trip steps")
            .Add(carousel => carousel.Loop, false).Add(carousel => carousel.ShowPicker, false)
            .AddChildContent<TbCarouselSlide>(slide => slide.AddChildContent("Park"))
            .AddChildContent<TbCarouselSlide>(slide => slide.AddChildContent("Walk")));
        var steps = cut.Tb().Carousel("Trip steps");

        var first = await Assert.ThrowsAsync<InvalidOperationException>(() => steps.PreviousAsync());
        var noPicker = await Assert.ThrowsAsync<InvalidOperationException>(() => steps.GoToAsync(1));

        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var still = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .AddChildContent<TbCarouselSlide>(slide => slide.AddChildContent("Pine Hollow")));
        var stuck = await Assert.ThrowsAsync<InvalidOperationException>(
            () => still.Tb().Carousel("Featured trails").NextAsync());

        Assert.Contains("doesn't loop", first.Message);
        Assert.Contains("no picker", noPicker.Message);
        Assert.Contains("static page", stuck.Message);
        Assert.Equal(0, steps.ActiveIndex);
    }

    [Fact]
    public async Task Chip_ShouldToggleClickAndRemoveTheChip_WhenTheHelperIsUsed()
    {
        var (selected, clicks, removed) = (false, 0, 0);
        var cut = Render<TbChipGroup>(parameters => parameters.Add(group => group.AriaLabel, "Filters")
            .AddChildContent<TbChip>(chip => chip.Add(each => each.Text, "Running")
                .Add(each => each.SelectedChanged, (bool on) => selected = on))
            .AddChildContent<TbChip>(chip => chip.Add(each => each.Text, "Archive")
                .Add(each => each.OnClick, () => clicks++).Add(each => each.OnRemove, () => removed++)));
        var filters = cut.Tb().ChipGroup("Filters");

        await filters.Chip("Running").ClickAsync();
        await cut.Tb().Chip("Archive").ClickAsync();
        await filters.Chip("Archive").RemoveAsync();

        Assert.Equal((true, 1, 1), (selected, clicks, removed));
        Assert.True(filters.Chip("Running").Selected);
        Assert.Equal(["Running", "Archive"], filters.Chips);
    }

    [Fact]
    public async Task Helpers_ShouldFindTheOnlyBannerCarouselAndToolbar_WhenNoNameIsGiven()
    {
        var pins = 0;
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbBanner>(0);
            builder.AddComponentParameter(1, nameof(TbBanner.Title), "Trail closed");
            builder.AddComponentParameter(2, nameof(TbBanner.ChildContent),
                (RenderFragment)(text => text.AddContent(0, "The ridge trail is closed today.")));
            builder.CloseComponent();
            builder.OpenComponent<TbCarousel>(3);
            builder.AddComponentParameter(4, nameof(TbCarousel.AriaLabel), "Views");
            builder.AddComponentParameter(5, nameof(TbCarousel.ChildContent), (RenderFragment)(slides =>
            {
                slides.OpenComponent<TbCarouselSlide>(0);
                slides.CloseComponent();
            }));
            builder.CloseComponent();
            builder.OpenComponent<TbToolbar>(6);
            builder.AddComponentParameter(7, nameof(TbToolbar.AriaLabel), "Map tools");
            builder.AddComponentParameter(8, nameof(TbToolbar.ChildContent), (RenderFragment)(tools =>
            {
                tools.OpenComponent<TbToolbarButton>(0);
                tools.AddComponentParameter(1, nameof(TbToolbarButton.AriaLabel), "Pin");
                tools.AddComponentParameter(2, nameof(TbToolbarButton.Icon), BuiltInIcons.Close);
                tools.AddComponentParameter(3, nameof(TbToolbarButton.OnClick),
                    EventCallback.Factory.Create<MouseEventArgs>(this, () => pins++));
                tools.CloseComponent();
            }));
            builder.CloseComponent();
        });

        await cut.Tb().Toolbar().ClickAsync("Pin");

        Assert.Equal(("The ridge trail is closed today.", 0, 1),
            (cut.Tb().Banner().Text, cut.Tb().Carousel().ActiveIndex, pins));
    }

    [Fact]
    public void ChipGroup_ShouldFindTheOnlyGroupOrAskForAName_WhenNoNameIsGiven()
    {
        var one = Render<TbChipGroup>(parameters => parameters.Add(group => group.AriaLabel, "Filters")
            .AddChildContent<TbChip>(chip => chip.Add(each => each.Text, "Running")));
        var two = Render(builder =>
        {
            foreach (var name in new[] { "Filters", "Tags" })
            {
                builder.OpenComponent<TbChipGroup>(0);
                builder.AddComponentParameter(1, nameof(TbChipGroup.AriaLabel), name);
                builder.CloseComponent();
            }
        });

        var unnamed = Assert.Throws<InvalidOperationException>(() => two.Tb().ChipGroup().Chips);

        Assert.Equal(["Running"], one.Tb().ChipGroup().Chips);
        Assert.Equal("Expected one chip group, found 2; name the one to use.", unnamed.Message);
    }

    [Fact]
    public async Task Chip_ShouldFindTheChipAndFollowItsLink_WhenTagsShareTheChipsClass()
    {
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbTagInput>(tags => tags.Add(input => input.Label, "Labels")
                .Add(input => input.Value, ["Pine Hollow"]))
            .AddChildContent<TbChip>(chip => chip.Add(each => each.Text, "Projects")
                .Add(each => each.Href, "projects")));

        await cut.Tb().Chip("Projects").ClickAsync();

        Assert.EndsWith("/projects", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Chip_ShouldRefuseWhatAUserCantDo_WhenTheChipIsPlainDisabledOrStatic()
    {
        var plain = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Invoices"));
        var off = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Invoices")
            .Add(chip => chip.Disabled, true).Add(chip => chip.OnRemove, () => { }));
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var still = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Invoices")
            .Add(chip => chip.OnClick, () => { }));

        var nothing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => plain.Tb().Chip("Invoices").ClickAsync());
        var noRemove = await Assert.ThrowsAsync<InvalidOperationException>(
            () => plain.Tb().Chip("Invoices").RemoveAsync());
        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => off.Tb().Chip("Invoices").RemoveAsync());
        var stuck = await Assert.ThrowsAsync<InvalidOperationException>(() => still.Tb().Chip("Invoices").ClickAsync());

        Assert.Contains("does nothing", nothing.Message);
        Assert.Contains("OnRemove", noRemove.Message);
        Assert.Contains("disabled", disabled.Message);
        Assert.Contains("static page", stuck.Message);
    }

    [Fact]
    public async Task Stepper_ShouldReadTheStepsAndGoBackToAFinishedOne_WhenTheHelperIsUsed()
    {
        string? active = "Payment";
        var cut = Render<TbStepper>(parameters => parameters.Add(stepper => stepper.AriaLabel, "Trip booking")
            .Add(stepper => stepper.ActiveKey, active)
            .Add(stepper => stepper.ActiveKeyChanged, (string? key) => active = key)
            .AddChildContent<TbStep>(step => step.Add(each => each.Title, "Trip").Add(each => each.Completed, true))
            .AddChildContent<TbStep>(step => step.Add(each => each.Title, "Gear").Add(each => each.Completed, true))
            .AddChildContent<TbStep>(step => step.Add(each => each.Title, "Payment"))
            .AddChildContent<TbStep>(step => step.Add(each => each.Title, "Review")));
        var stepper = cut.Tb().Stepper("Trip booking");
        var current = stepper.Current;

        await stepper.GoToAsync("Gear");
        // The steps are linear, so a step ahead that isn't finished can't be chosen.
        var ahead = await Assert.ThrowsAsync<InvalidOperationException>(() => stepper.GoToAsync("Review"));

        Assert.Equal(["Trip", "Gear", "Payment", "Review"], stepper.Steps);
        Assert.Equal(("Payment", "Gear"), (current, active));
        Assert.Contains("can't be chosen", ahead.Message);
    }

    [Fact]
    public async Task Breadcrumbs_ShouldReadTheTrailAndFollowALink_WhenTheHelperIsUsed()
    {
        var cut = Render<TbBreadcrumbs>(parameters => parameters
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Href, "projects")
                .AddChildContent("Projects"))
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Current, true).AddChildContent("Trails")));
        var trail = cut.Tb().Breadcrumbs();

        await trail.FollowAsync("Projects");

        Assert.Equal(["Projects", "Trails"], trail.Items);
        Assert.Equal("Trails", trail.Current);
        Assert.EndsWith("/projects", Services.GetRequiredService<NavigationManager>().Uri);
        var plain = await Assert.ThrowsAsync<InvalidOperationException>(() => trail.FollowAsync("Trails"));
        Assert.Contains("isn't a link", plain.Message);
    }

    [Fact]
    public async Task Form_ShouldSubmitAndReadTheFieldsMessages_WhenTheHelperIsUsed()
    {
        var (valid, invalid) = (0, 0);
        var cut = Render<TbForm>(parameters => parameters.AddUnmatched("aria-label", "Trip sign-up")
            .Add(form => form.OnValidSubmit, () => valid++).Add(form => form.OnInvalidSubmit, () => invalid++)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Leader's name");
                builder.AddAttribute(2, nameof(TbTextField<string>.Required), true);
                builder.AddAttribute(3, nameof(TbTextField<string>.RequiredMessage), "Enter the leader's name.");
                builder.CloseComponent();
            })));
        var form = cut.Tb().Form("Trip sign-up");

        await form.SubmitAsync();
        var errors = form.Errors;
        await cut.Tb().TextField("Leader's name").EnterAsync("Ada");
        await form.SubmitAsync();

        Assert.Equal(["Enter the leader's name."], errors);
        Assert.Equal((1, 1), (invalid, valid));
        Assert.Empty(form.Errors);
    }

    [Fact]
    public void Form_ShouldFindTheForm_WhenAHeadingNamesIt()
    {
        var cut = Render<TbStack>(parameters => parameters.AddChildContent("<h2 id=\"trip\">Trip sign-up</h2>")
            .AddChildContent<TbForm>(form => form.AddUnmatched("aria-labelledby", "trip")));

        Assert.Empty(cut.Tb().Form("Trip sign-up").Errors);
    }

    [Fact]
    public async Task Form_ShouldRefuseToSubmit_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.FormName, "sign-up"));

        var still = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Form().SubmitAsync());

        Assert.Contains("static page", still.Message);
    }

    [Fact]
    public void Tooltip_ShouldReadItsOwnText_WhenFoundByItsTriggersNameInsideAnotherTooltip()
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Map tools")
            .AddChildContent<TbTooltip>(inner => inner.Add(tooltip => tooltip.Text, "Add an item")
                .AddChildContent<TbIconButton>(button => button.Add(each => each.Icon, BuiltInIcons.Close)
                    .Add(each => each.AriaLabel, "Add"))));

        var missing = Assert.Throws<InvalidOperationException>(() => cut.Tb().Tooltip("Remove").Text);

        Assert.Equal("Add an item", cut.Tb().Tooltip("Add").Text);
        Assert.Contains("No tooltip's trigger", missing.Message);
    }

    [Fact]
    public void Tooltip_ShouldReadALinksTooltipAndRefuseWhatItCantRead_WhenTriggersRepeatOrShowNothing()
    {
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbTooltip>(tooltip => tooltip.Add(each => each.TextContent, "Opens the\n    trail map")
                .AddChildContent<TbLink>(link => link.Add(each => each.Href, "map").AddChildContent("Map")))
            .AddChildContent<TbTooltip>(tooltip => tooltip.Add(each => each.Text, "Adds a trail")
                .AddChildContent<TbButton>(button => button.AddChildContent("Add")))
            .AddChildContent<TbTooltip>(tooltip => tooltip.Add(each => each.Text, "Adds a stop")
                .AddChildContent<TbButton>(button => button.AddChildContent("Add")))
            .AddChildContent<TbTooltip>(tooltip => tooltip.Add(each => each.Text, "Saves the trip")
                .Add(each => each.Disabled, true).AddChildContent<TbButton>(button => button.AddChildContent("Save"))));

        var repeated = Assert.Throws<InvalidOperationException>(() => cut.Tb().Tooltip("Add").Text);
        var silent = Assert.Throws<InvalidOperationException>(() => cut.Tb().Tooltip("Save").Text);

        Assert.Equal("Opens the trail map", cut.Tb().Tooltip("Map").Text);
        Assert.Contains("2 tooltips' triggers", repeated.Message);
        Assert.Contains("disabled or has no text", silent.Message);
    }

    [Fact]
    public async Task Toolbar_ShouldClickItsButtonsByNameAndRefuseADisabledOne_WhenTheHelperIsUsed()
    {
        var clicked = new List<string>();
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Map tools")
            .AddChildContent<TbToolbarButton>(button => button.Add(each => each.Label, "Pan")
                .Add(each => each.OnClick, () => clicked.Add("Pan")))
            .AddChildContent<TbToolbarButton>(button => button.Add(each => each.AriaLabel, "Zoom in")
                .Add(each => each.Icon, BuiltInIcons.Close).Add(each => each.OnClick, () => clicked.Add("Zoom in")))
            .AddChildContent<TbToolbarButton>(button => button.Add(each => each.Label, "Print")
                .Add(each => each.Disabled, true)));

        await cut.Tb().Toolbar("Map tools").ClickAsync("Pan");
        await cut.Tb().Toolbar("Map tools").ClickAsync("Zoom in");
        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Toolbar("Map tools").ClickAsync("Print"));

        Assert.Equal(["Pan", "Zoom in"], clicked);
        Assert.Contains("disabled", disabled.Message);
    }

    [Fact]
    public async Task Toolbar_ShouldClickItsOwnButton_WhenTheOverflowMenusButtonHasTheSameName()
    {
        var clicked = 0;
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Map tools")
            .AddChildContent<TbToolbarButton>(button => button.Add(each => each.Label, "More")
                .Add(each => each.OnClick, () => clicked++)));

        await cut.Tb().Toolbar("Map tools").ClickAsync("More");

        Assert.Equal(1, clicked);
    }

    [Fact]
    public async Task Toolbar_ShouldClickItsOwnButton_WhenAMenuInTheToolbarHasAnItemWithTheSameName()
    {
        var clicked = 0;
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Map tools")
            .AddChildContent<TbMenu>(menu => menu.Add(each => each.Label, "Edit")
                .AddChildContent<TbMenuItem>(item => item.AddChildContent("Copy")))
            .AddChildContent<TbToolbarButton>(button => button.Add(each => each.Label, "Copy")
                .Add(each => each.OnClick, () => clicked++)));

        await cut.Tb().Toolbar("Map tools").ClickAsync("Copy");

        Assert.Equal(1, clicked);
    }

    [Fact]
    public async Task Toolbar_ShouldRefuseToClick_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Map tools")
            .AddChildContent<TbToolbarButton>(button => button.Add(each => each.Label, "Pan")));

        var still = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Toolbar("Map tools").ClickAsync("Pan"));

        Assert.Contains("static page", still.Message);
    }

    [Fact]
    public async Task InsertDivider_ShouldRaiseOnInsertAndRefuseADisabledOne_WhenTheHelperIsUsed()
    {
        var inserted = 0;
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbInsertDivider>(divider => divider.Add(each => each.Label, "Add a step at the start")
                .Add(each => each.OnInsert, () => inserted++))
            .AddChildContent<TbInsertDivider>(divider => divider.Add(each => each.Label, "Add a step at the end")
                .Add(each => each.Disabled, true)));

        await cut.Tb().InsertDivider("Add a step at the start").InsertAsync();
        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().InsertDivider("Add a step at the end").InsertAsync());

        Assert.Equal(1, inserted);
        Assert.Contains("disabled", disabled.Message);
    }

    [Fact]
    public async Task InsertDivider_ShouldRefuseToInsert_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbInsertDivider>(parameters => parameters.Add(divider => divider.Label, "Add a step"));

        var still = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().InsertDivider("Add a step").InsertAsync());

        Assert.Contains("static page", still.Message);
    }

    private static readonly RenderFragment AccountMenu = builder =>
    {
        builder.OpenElement(0, "nav");
        builder.AddAttribute(1, "aria-label", "Account");
        builder.OpenComponent<TbNavMenu>(2);
        builder.AddComponentParameter(3, nameof(TbNavMenu.ChildContent), (RenderFragment)(links =>
        {
            links.OpenComponent<TbNavLink>(0);
            links.AddComponentParameter(1, nameof(TbNavLink.Href), "");
            links.AddComponentParameter(2, nameof(TbNavLink.Match), NavLinkMatch.All);
            links.AddComponentParameter(3, nameof(TbNavLink.ChildContent), (RenderFragment)(text => text.AddContent(0,
                "Home")));
            links.CloseComponent();
            links.OpenComponent<TbNavLink>(4);
            links.AddComponentParameter(5, nameof(TbNavLink.Href), "settings");
            links.AddComponentParameter(6, nameof(TbNavLink.ChildContent), (RenderFragment)(text => text.AddContent(0,
                "All settings")));
            links.CloseComponent();
            links.OpenComponent<TbNavGroup>(7);
            links.AddComponentParameter(8, nameof(TbNavGroup.Text), "Settings");
            links.AddComponentParameter(9, nameof(TbNavGroup.ChildContent), (RenderFragment)(group =>
            {
                group.OpenComponent<TbNavLink>(0);
                group.AddComponentParameter(1, nameof(TbNavLink.Href), "settings/profile");
                group.AddComponentParameter(2, nameof(TbNavLink.ChildContent), (RenderFragment)(text =>
                    text.AddContent(0, "Profile")));
                group.CloseComponent();
                group.OpenComponent<TbNavGroup>(3);
                group.AddComponentParameter(4, nameof(TbNavGroup.Text), "Advanced");
                group.AddComponentParameter(5, nameof(TbNavGroup.ChildContent), (RenderFragment)(inner =>
                {
                    inner.OpenComponent<TbNavLink>(0);
                    inner.AddComponentParameter(1, nameof(TbNavLink.Href), "settings/advanced/backups");
                    inner.AddComponentParameter(2, nameof(TbNavLink.ChildContent), (RenderFragment)(text =>
                        text.AddContent(0, "Backups")));
                    inner.CloseComponent();
                }));
                group.CloseComponent();
                group.OpenComponent<TbNavLink>(6);
                group.AddComponentParameter(7, nameof(TbNavLink.Href), "settings/overview");
                group.AddComponentParameter(8, nameof(TbNavLink.ChildContent), (RenderFragment)(text =>
                    text.AddContent(0, "Overview")));
                group.CloseComponent();
            }));
            links.CloseComponent();
            links.OpenComponent<TbNavLink>(10);
            links.AddComponentParameter(11, nameof(TbNavLink.Href), "overview");
            links.AddComponentParameter(12, nameof(TbNavLink.ChildContent), (RenderFragment)(text => text.AddContent(0,
                "Overview")));
            links.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    };

    [Fact]
    public async Task NavMenu_ShouldFollowALinkThroughItsGroupAndReadTheCurrentPage_WhenTheHelperIsUsed()
    {
        var cut = Render(AccountMenu);
        var before = cut.Tb().NavMenu("Account").Current;

        await cut.Tb().NavMenu("Account").FollowAsync("Settings", "Profile");

        Assert.Equal("Home", before);
        Assert.EndsWith("/settings/profile", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Equal("Profile", cut.Tb().NavMenu("Account").Current);
    }

    [Fact]
    public async Task NavMenu_ShouldFollowTheLinkAtEachLevel_WhenALabelRepeatsInAGroup()
    {
        var cut = Render(AccountMenu);
        var navigation = Services.GetRequiredService<NavigationManager>();

        await cut.Tb().NavMenu("Account").FollowAsync("Settings", "Overview");
        var inGroup = navigation.Uri;
        await cut.Tb().NavMenu("Account").FollowAsync("Overview");

        Assert.EndsWith("/settings/overview", inGroup);
        Assert.EndsWith("/overview", navigation.Uri);
        Assert.DoesNotContain("settings", navigation.Uri);
    }

    [Fact]
    public async Task NavMenu_ShouldRefuseALinkInAClosedGroup_WhenThePathDoesntNameTheGroup()
    {
        var cut = Render(AccountMenu);

        var closed = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().NavMenu("Account").FollowAsync("Profile"));

        Assert.Contains("closed group", closed.Message);
    }

    [Fact]
    public async Task NavMenu_ShouldRefuseAGroupInAClosedGroup_WhenThePathDoesntNameTheOuterGroup()
    {
        var cut = Render(AccountMenu);

        var closed = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().NavMenu("Account").FollowAsync("Advanced", "Backups"));

        Assert.Contains("closed group", closed.Message);
        Assert.All(cut.FindAll("[data-part=group-toggle]"), toggle => Assert.Equal("false",
            toggle.GetAttribute("aria-expanded")));
    }

    [Fact]
    public void NavMenu_ShouldFindTheMenu_WhenAHeadingNamesItsLandmark()
    {
        var cut = Render(builder =>
        {
            builder.AddMarkupContent(0, "<h2 id=\"trail-menu\">Trail menu</h2>");
            builder.OpenElement(1, "nav");
            builder.AddAttribute(2, "aria-labelledby", "trail-menu");
            builder.OpenComponent<TbNavMenu>(3);
            builder.CloseComponent();
            builder.CloseElement();
        });

        Assert.Null(cut.Tb().NavMenu("Trail menu").Current);
    }

    [Fact]
    public async Task NavMenu_ShouldRefuseToOpenAGroup_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render(AccountMenu);

        var still = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().NavMenu("Account").FollowAsync("Settings", "Profile"));

        Assert.Contains("static page", still.Message);
    }

    [Fact]
    public async Task NavMenu_ShouldOpenCloseAndReadAGroup_WhenTheHelperIsUsed()
    {
        var cut = Render(AccountMenu);
        var menu = cut.Tb().NavMenu("Account");

        await menu.ExpandAsync("Settings", "Advanced");
        var opened = (menu.Expanded("Settings"), menu.Expanded("Settings", "Advanced"));
        await menu.CollapseAsync("Settings");
        var closed = (menu.Expanded("Settings"), menu.Expanded("Settings", "Advanced"));
        await menu.CollapseAsync("Settings", "Advanced");

        Assert.Equal((true, true), opened);
        Assert.Equal((false, true), closed);
        Assert.Equal((true, false), (menu.Expanded("Settings"), menu.Expanded("Settings", "Advanced")));
    }

    [Fact]
    public async Task NavMenu_ShouldRefuseToExpandAGroupInAClosedGroup_WhenThePathDoesntNameTheOuterGroup()
    {
        var cut = Render(AccountMenu);

        var closed = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().NavMenu("Account").ExpandAsync("Advanced"));

        Assert.Contains("closed group", closed.Message);
        Assert.False(cut.Tb().NavMenu("Account").Expanded("Settings", "Advanced"));
    }

    [Fact]
    public async Task NavMenu_ShouldRefuseToCloseAGroup_WhenThePageIsStatic()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("settings/profile");
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render(AccountMenu);

        var still = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().NavMenu("Account").CollapseAsync("Settings"));

        Assert.Contains("doesn't close", still.Message);
        Assert.True(cut.Tb().NavMenu("Account").Expanded("Settings"));
    }

    [Fact]
    public async Task NavMenu_ShouldFollowTheDrawersLink_WhenTheAppShellsDrawerIsOpen()
    {
        var cut = Render<TbAppShell>(parameters => parameters.Add(shell => shell.NavigationOpen, true)
            .Add(shell => shell.Navigation, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbNavMenu>(0);
                builder.AddComponentParameter(1, nameof(TbNavMenu.ChildContent), (RenderFragment)(links =>
                {
                    links.OpenComponent<TbNavLink>(0);
                    links.AddComponentParameter(1, nameof(TbNavLink.Href), "trails");
                    links.AddComponentParameter(2, nameof(TbNavLink.ChildContent), (RenderFragment)(text =>
                        text.AddContent(0, "Trails")));
                    links.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        await cut.Tb().NavMenu("Main").FollowAsync("Trails");

        Assert.EndsWith("/trails", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task TextField_ShouldEnterOnlyWhatACodeFieldTakes_WhenTheFieldIsACodeField()
    {
        var (value, completed) = ((string?)null, new List<string>());
        var cut = Render<TbCodeField>(parameters => parameters.Add(field => field.Label, "Verification code")
            .Add(field => field.ValueChanged, (string now) => value = now)
            .Add(field => field.OnComplete, (string code) => completed.Add(code)));

        await cut.Tb().TextField("Verification code").EnterAsync("246-810");

        Assert.Equal("246810", cut.Tb().TextField("Verification code").Text);
        Assert.Equal("246810", value);
        Assert.Equal(["246810"], completed);
    }

    [Fact]
    public void Button_ShouldReadWhetherAToggleIsPressedOrMixed_WhenTheHelperIsUsed()
    {
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbButton>(button => button.Add(each => each.Pressed, true)
                .Add(each => each.PressedChanged, (bool _) => { }).AddChildContent("Bold"))
            .AddChildContent<TbButton>(button => button.Add(each => each.Mixed, true)
                .Add(each => each.PressedChanged, (bool _) => { }).AddChildContent("Italic"))
            .AddChildContent<TbButton>(button => button.AddChildContent("Save")));
        var (bold, italic, save) = (cut.Tb().Button("Bold"), cut.Tb().Button("Italic"), cut.Tb().Button("Save"));

        Assert.Equal((true, false, false, true, false, false),
            (bold.Pressed, bold.Mixed, italic.Pressed, italic.Mixed, save.Pressed, save.Mixed));
    }

    [Fact]
    public void Checkbox_ShouldReadTheMixedState_WhenABoolCheckboxIsNull()
    {
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbCheckbox<bool?>>(box => box.Add(each => each.Label, "Every day"))
            .AddChildContent<TbCheckbox<bool?>>(box => box.Add(each => each.Label, "Weekends")
                .Add(each => each.Value, true))
            .AddChildContent<TbCheckbox<bool?>>(box => box.Add(each => each.Label, "Holidays")
                .Add(each => each.Value, false)));

        Assert.Equal((true, false, false, true), (cut.Tb().Checkbox("Every day").Indeterminate,
            cut.Tb().Checkbox("Every day").Checked, cut.Tb().Checkbox("Weekends").Indeterminate,
            cut.Tb().Checkbox("Weekends").Checked));
        Assert.Equal((false, false), (cut.Tb().Checkbox("Holidays").Indeterminate,
            cut.Tb().Checkbox("Holidays").Checked));
    }

    [Fact]
    public async Task Button_ShouldClickAndReadWhetherItsBusy_WhenTheHelperIsUsed()
    {
        var saved = 0;
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbButton>(button => button.Add(each => each.OnClick, () => saved++)
                .AddChildContent("Save"))
            .AddChildContent<TbButton>(button => button.Add(each => each.Loading, true).AddChildContent("Send"))
            .AddChildContent<TbButton>(button => button.Add(each => each.Disabled, true).AddChildContent("Delete")));

        await cut.Tb().Button("Save").ClickAsync();
        var busy = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Button("Send").ClickAsync());
        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Button("Delete").ClickAsync());

        Assert.Equal(1, saved);
        Assert.Equal((false, true), (cut.Tb().Button("Save").Busy, cut.Tb().Button("Send").Busy));
        Assert.Contains("busy", busy.Message);
        Assert.Contains("disabled", disabled.Message);
    }

    [Fact]
    public async Task Link_ShouldRunOnClickAndFollowTheAddress_WhenClicked()
    {
        var (copied, followed) = (0, 0);
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbLink>(link => link.Add(each => each.OnClick, () => copied++).AddChildContent("Copy"))
            .AddChildContent<TbLink>(link => link.Add(each => each.Href, "guides/theming")
                .Add(each => each.OnClick, () => followed++).AddChildContent("Theming"))
            .AddChildContent<TbLink>(link => link.Add(each => each.Href, "guides/testing")
                .Add(each => each.Disabled, true).AddChildContent("Testing")));

        await cut.Tb().Link("Copy").ClickAsync();
        await cut.Tb().Link("Theming").ClickAsync();
        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Link("Testing").ClickAsync());

        Assert.Equal((1, 1), (copied, followed));
        Assert.EndsWith("/guides/theming", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("disabled", disabled.Message);
    }

    [Fact]
    public async Task Link_ShouldLeaveTheAddressAlone_WhenTheLinkOpensANewTab()
    {
        var uri = Services.GetRequiredService<NavigationManager>().Uri;
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "guides/theming")
            .Add(link => link.NewTab, true).AddChildContent("Theming"));

        await cut.Tb().Link("Theming (opens in a new tab)").ClickAsync();

        Assert.Equal(uri, Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Link_ShouldThrow_WhenItHasNoAddressOnAStaticPage()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.OnClick, () => { })
            .AddChildContent("Copy"));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Link("Copy").ClickAsync());

        Assert.Contains("static page", failure.Message);
    }

    [Fact]
    public async Task Button_ShouldClickAnIconButton_WhenFoundByItsName()
    {
        var added = 0;
        var cut = Render<TbIconButton>(parameters => parameters.Add(button => button.AriaLabel, "Add")
            .Add(button => button.Icon, BuiltInIcons.Plus).Add(button => button.OnClick, () => added++));

        await cut.Tb().Button("Add").ClickAsync();

        Assert.Equal(1, added);
    }

    [Fact]
    public async Task Button_ShouldFollowItsLink_WhenItHasAnHref()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Href, "trails")
            .AddChildContent("Trails"));

        await cut.Tb().Button("Trails").ClickAsync();

        Assert.EndsWith("/trails", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Button_ShouldLeaveTheAddressAlone_WhenItsLinkOpensANewTab()
    {
        var uri = Services.GetRequiredService<NavigationManager>().Uri;
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Href, "trails")
            .AddUnmatched("target", "_blank").AddChildContent("Trails"));

        await cut.Tb().Button("Trails").ClickAsync();

        Assert.Equal(uri, Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Button_ShouldRefuseToClick_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbButton>(parameters => parameters.AddChildContent("Save"));

        var still = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Button("Save").ClickAsync());

        Assert.Contains("static page", still.Message);
    }

    [Fact]
    public async Task Popconfirm_ShouldRaiseConfirmOrCancel_WhenItsButtonsArePressed()
    {
        var answers = new List<string>();
        var cut = Render<TbPopconfirm>(parameters => parameters.Add(confirm => confirm.Label, "Delete trail")
            .Add(confirm => confirm.Title, "Delete Pine Hollow?")
            .Add(confirm => confirm.OnConfirm, () => answers.Add("confirm"))
            .Add(confirm => confirm.OnCancel, () => answers.Add("cancel")));

        await cut.Tb().Popconfirm("Delete trail").ConfirmAsync();
        await cut.Tb().Popconfirm("Delete trail").CancelAsync();

        Assert.Equal(["confirm", "cancel"], answers);
    }

    [Theory]
    [InlineData("Delete trail", true)]
    [InlineData("Archive trail", false)]
    public async Task Popconfirm_ShouldThrow_WhenItIsDisabledOrNotThere(string trigger, bool disabled)
    {
        var confirmed = false;
        var cut = Render<TbPopconfirm>(parameters => parameters.Add(confirm => confirm.Label, "Delete trail")
            .Add(confirm => confirm.Title, "Delete Pine Hollow?").Add(confirm => confirm.Disabled, disabled)
            .Add(confirm => confirm.OnConfirm, () => confirmed = true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Popconfirm(trigger).ConfirmAsync());
        Assert.False(confirmed);
    }

    [Fact]
    public async Task Tree_ShouldExpandSelectAndCheckRowsByTheirTexts_WhenRowsRepeatATextAndHoldATree()
    {
        IReadOnlyCollection<string> selected = [];
        IReadOnlyCollection<string> checkedKeys = [];
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Outline")
            .Add(tree => tree.Checkable, true)
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<string> keys) => selected = keys)
            .Add(tree => tree.CheckedKeysChanged, (IReadOnlyCollection<string> keys) => checkedKeys = keys)
            .Add(tree => tree.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTreeItem<string>>(0);
                builder.AddAttribute(1, nameof(TbTreeItem<string>.Text), "Part one");
                builder.AddAttribute(2, nameof(TbTreeItem<string>.TextContent), (RenderFragment)(row =>
                {
                    row.OpenComponent<TbTreeView<string, string>>(0);
                    row.AddAttribute(1, nameof(TbTreeView<string, string>.AriaLabel), "Inner");
                    row.AddAttribute(2, nameof(TbTreeView<string, string>.ChildContent), Items(("inner", "P")));
                    row.CloseComponent();
                }));
                builder.AddAttribute(3, nameof(TbTreeItem<string>.ChildContent), Items(("p-1", "P"), ("p-2", "P")));
                builder.CloseComponent();
            })));
        var outline = cut.Tb().Tree("Outline");

        await outline.ExpandAsync("Part one");
        await outline.SelectAsync("Part one", "P");
        await outline.CheckAsync("Part one", "P");

        Assert.Equal(["p-1", "p-2"], outline.Items("Part one", "P").Select(row => row.GetAttribute("data-key")));
        Assert.Equal(["Part one", "P", "P"], outline.Texts);
        Assert.Equal(["p-1"], selected);
        Assert.Equal(["p-1"], checkedKeys);
        Assert.Equal(["P"], cut.Tb().Tree("Inner").Texts);
    }

    [Fact]
    public async Task Tree_ShouldCollapseAndActivateRows_WhenAskedByPath()
    {
        string? activated = null;
        IReadOnlyCollection<string> selected = [];
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Sections")
            .Add(tree => tree.OnActivate, (string item) => activated = item)
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<string> keys) => selected = keys)
            .Add(tree => tree.ChildContent, Section("docs", "Docs", Items(("guide", "Guide"), ("intro", "Intro")))));
        var tree = cut.Tb().Tree("Sections");
        await tree.ExpandAsync("Docs");

        await tree.CollapseAsync("Docs");
        var collapsed = tree.Texts;
        await tree.ActivateAsync("Docs", "Guide");

        Assert.Equal(["Docs"], collapsed);
        Assert.Equal("guide", activated);
        Assert.Equal(["guide"], selected);
        Assert.Equal(["Docs", "Guide", "Intro"], tree.Texts);
    }

    [Fact]
    public async Task Tree_ShouldOpenAParentThatCantBeSelectedOnce_WhenActivatingIt()
    {
        string? activated = null;
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Sections")
            .Add(tree => tree.SelectionMode, SelectionMode.None)
            .Add(tree => tree.OnActivate, (string item) => activated = item)
            .Add(tree => tree.ChildContent, Section("docs", "Docs", Items(("guide", "Guide")))));
        var tree = cut.Tb().Tree("Sections");

        await tree.ActivateAsync("Docs");

        Assert.Equal("docs", activated);
        Assert.Equal(["Docs", "Guide"], tree.Texts);
    }

    [Fact]
    public async Task Tree_ShouldFindARowByItsTextAtAnyDepth_WhenOneRowShowsIt()
    {
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Sections")
            .Add(tree => tree.ChildContent, (RenderFragment)(builder =>
            {
                builder.AddContent(0, Section("docs", "Docs", Items(("docs-guide", "Guide"), ("intro", "Intro"))));
                builder.AddContent(1, Section("blog", "Blog", Items(("blog-guide", "Guide"))));
            })));
        var tree = cut.Tb().Tree("Sections");

        await tree.ExpandAllAsync();

        Assert.Equal("intro", tree.Row("Intro").GetAttribute("data-key"));
        Assert.Contains("2 rows show Guide", Assert.Throws<InvalidOperationException>(() => tree.Row("Guide")).Message);
        Assert.Contains("No row shows Notes",
            Assert.Throws<InvalidOperationException>(() => tree.Row("Notes")).Message);
    }

    [Fact]
    public async Task Tree_ShouldStartPathsInsideTheRow_WhenGivenABranch()
    {
        IReadOnlyCollection<string> selected = [];
        var cut = RenderSections(parameters => parameters
            .Add(tree => tree.SelectedKeysChanged, (IReadOnlyCollection<string> keys) => selected = keys));
        var tree = cut.Tb().Tree("Sections");
        var blog = tree.Branch("Blog");
        await tree.ExpandAsync("Docs");

        await blog.Branch("Posts").SelectAsync("Guide");
        var guide = blog.Row("Guide").GetAttribute("data-key");
        var item = blog.Item("Posts", "Guide").GetAttribute("data-key");
        await blog.CollapseAsync("Posts");

        Assert.Equal(["blog-guide"], selected);
        Assert.Equal("blog-guide", guide);
        Assert.Equal("blog-guide", item);
        Assert.Equal(["Posts"], blog.Texts);
    }

    [Fact]
    public async Task Tree_ShouldOpenOnlyTheBranchsRows_WhenExpandingAllInABranch()
    {
        var cut = RenderSections();

        await cut.Tb().Tree("Sections").Branch("Blog").ExpandAllAsync();

        Assert.Equal(["Docs", "Blog", "Posts", "Guide"], cut.Tb().Tree("Sections").Texts);
    }

    private IRenderedComponent<TbTreeView<string, string>> RenderSections(
        Action<ComponentParameterCollectionBuilder<TbTreeView<string, string>>>? configure = null) =>
        Render<TbTreeView<string, string>>(parameters =>
        {
            var posts = Section("posts", "Posts", Items(("blog-guide", "Guide")));
            parameters.Add(tree => tree.AriaLabel, "Sections")
                .Add(tree => tree.ChildContent, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, Section("docs", "Docs", Items(("docs-guide", "Guide"))));
                    builder.AddContent(1, Section("blog", "Blog", posts));
                }));
            configure?.Invoke(parameters);
        });

    private static RenderFragment Section(string value, string text, RenderFragment rows) => builder =>
    {
        builder.OpenComponent<TbTreeItem<string>>(0);
        builder.AddAttribute(1, nameof(TbTreeItem<string>.Value), value);
        builder.AddAttribute(2, nameof(TbTreeItem<string>.Text), text);
        builder.AddAttribute(3, nameof(TbTreeItem<string>.ChildContent), rows);
        builder.CloseComponent();
    };

    [Fact]
    public async Task Tree_ShouldFollowTheRowThatLeadsOn_WhenTheFirstRowWithTheTextDoesnt()
    {
        RenderFragment Part(string child) => builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddAttribute(1, nameof(TbTreeItem<string>.Text), "Part");
            builder.AddAttribute(2, nameof(TbTreeItem<string>.ChildContent), Items((child, child)));
            builder.CloseComponent();
        };
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Book")
            .Add(tree => tree.ChildContent, (RenderFragment)(builder =>
            {
                builder.AddContent(0, Part("Note"));
                builder.AddContent(1, Part("Q"));
            })));

        await cut.Tb().Tree().ExpandAllAsync();

        Assert.Equal("Q", cut.Tb().Tree().Item("Part", "Q").GetAttribute("data-key"));
    }

    [Fact]
    public async Task Tree_ShouldSayARowDidntOpen_WhenExpandingAllInADisabledTree()
    {
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.Disabled, true).Add(tree => tree.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTreeItem<string>>(0);
                builder.AddAttribute(1, nameof(TbTreeItem<string>.Value), "docs");
                builder.AddAttribute(2, nameof(TbTreeItem<string>.ChildContent), Items(("guide", "guide")));
                builder.CloseComponent();
            })));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Tree().ExpandAllAsync());

        Assert.Contains("didn't open", error.Message);
    }

    [Fact]
    public async Task Tree_ShouldOpenRowsAsTheyAppear_WhenExpandingAll()
    {
        RenderFragment Lazy(string value, RenderFragment inside) => builder =>
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddAttribute(1, nameof(TbTreeItem<string>.Value), value);
            builder.AddAttribute(2, nameof(TbTreeItem<string>.HasChildren), true);
            builder.AddAttribute(3, nameof(TbTreeItem<string>.ChildContent), inside);
            builder.CloseComponent();
        };
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Files")
            .Add(tree => tree.ChildContent, Lazy("docs", Lazy("guide", Items(("intro", "intro"))))));

        await cut.Tb().Tree().ExpandAllAsync();

        Assert.Equal(["docs", "guide", "intro"], cut.Tb().Tree().Texts);
    }

    [Fact]
    public void Field_ShouldSayNoFieldHasTheLabelAndListTheLabelsThere_WhenNoneDoes()
    {
        var cut = RenderForm();

        var error = Assert.Throws<InvalidOperationException>(() => cut.Tb().Select("Color").Options);

        Assert.StartsWith("No field is labeled \"Color\". The fields are labeled: \"Size\", ", error.Message);
        Assert.EndsWith(".", error.Message);
    }

    [Fact]
    public void Field_ShouldSayOnlyThatNoFieldHasTheLabel_WhenTheMarkupHasNoFields()
    {
        var cut = Render<TbButton>(parameters => parameters.AddChildContent("Save"));

        var error = Assert.Throws<InvalidOperationException>(() => cut.Tb().TextField("Color").Text);

        Assert.Equal("No field is labeled \"Color\".", error.Message);
    }

    [Fact]
    public void Field_ShouldCountAndListTheFieldsNamedWithoutALabel_WhenALookupFails()
    {
        var cut = Render(builder =>
        {
            builder.AddMarkupContent(0, "<span id=\"o'brien\">Guide</span>");
            foreach (var (sequence, name, value) in new[]
            {
                (1, "aria-label", "Count"), (2, "aria-label", "Count"), (3, "aria-labelledby", "o'brien"),
            })
            {
                builder.OpenComponent<TbTextField<string>>(sequence);
                builder.AddAttribute(0, nameof(TbTextField<string>.InputAttributes),
                    new Dictionary<string, object> { [name] = value });
                builder.CloseComponent();
            }
        });

        // Two fields share an aria-label, and the third is named by an element whose id a selector couldn't hold.
        Assert.Equal("2 fields are labeled \"Count\".",
            Assert.Throws<InvalidOperationException>(() => cut.Tb().TextField("Count").Text).Message);
        Assert.Equal("No field is labeled \"Cnt\". The fields are labeled: \"Count\", \"Guide\".",
            Assert.Throws<InvalidOperationException>(() => cut.Tb().TextField("Cnt").Text).Message);
        Assert.Equal("", cut.Tb().TextField("Guide").Text ?? "");
    }

    [Fact]
    public async Task Button_ShouldBeFoundByItsOwnText_WhenItsAriaLabelledByNamesNothingThere()
    {
        var clicks = 0;
        var cut = Render<TbButton>(parameters => parameters.AddUnmatched("aria-labelledby", "gone")
            .Add(button => button.OnClick, () => clicks++).AddChildContent("Save"));

        await cut.Tb().Button("Save").ClickAsync();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public async Task Field_ShouldBeFoundByAnElementItsAriaLabelledByNames_WhenItHasNoLabel()
    {
        string? note = null;
        var cut = Render(builder =>
        {
            builder.AddMarkupContent(0, "<h2 id=\"trip\">Trip</h2><span id=\"note\">note</span>");
            builder.OpenComponent<TbTextField<string>>(1);
            builder.AddAttribute(2, nameof(TbTextField<string>.InputAttributes),
                new Dictionary<string, object> { ["aria-labelledby"] = "trip note" });
            builder.AddAttribute(3, nameof(TbTextField<string>.ValueChanged),
                EventCallback.Factory.Create<string>(this, value => note = value));
            builder.CloseComponent();
        });

        // Either element that labels it finds it, as Playwright's GetByLabel does; their joined text doesn't.
        await cut.Tb().TextField("Trip").EnterAsync("Bring water");

        Assert.Equal("Bring water", note);
        Assert.Equal("Bring water", cut.Tb().TextField("note").Text);
        Assert.Throws<InvalidOperationException>(() => cut.Tb().TextField("Trip note").Text);
    }

    [Fact]
    public async Task Button_ShouldBeFoundByTheElementsItsAriaLabelledByNames_WhenItHasThatAttribute()
    {
        var clicks = 0;
        var cut = Render(builder =>
        {
            builder.AddMarkupContent(0, "<span id=\"verb\">Delete</span><span id=\"what\">Ridge loop</span>");
            builder.OpenComponent<TbButton>(1);
            builder.AddAttribute(2, "aria-labelledby", "verb what");
            builder.AddAttribute(3, nameof(TbButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this,
                () => clicks++));
            builder.AddAttribute(4, nameof(TbButton.ChildContent), (RenderFragment)(text => text.AddContent(0, "X")));
            builder.CloseComponent();
        });

        // The named elements win over the button's own text, in the order the attribute lists them, as in a browser.
        await cut.Tb().Button("Delete Ridge loop").ClickAsync();

        Assert.Equal(1, clicks);
        Assert.StartsWith("No button is named \"X\". The buttons are named: \"Delete Ridge loop\".",
            Assert.Throws<InvalidOperationException>(() => cut.Tb().Button("X").Busy).Message);
    }

    [Fact]
    public void Button_ShouldListADozenNamesAndCountTheRest_WhenNoneOfManyMatches()
    {
        var cut = Render(builder =>
        {
            for (var number = 1; number <= 14; number++)
            {
                var name = $"Stop {number}";
                builder.OpenComponent<TbButton>(number);
                builder.AddAttribute(0, nameof(TbButton.ChildContent), (RenderFragment)(text => text.AddContent(0, name)));
                builder.CloseComponent();
            }
        });

        var error = Assert.Throws<InvalidOperationException>(() => cut.Tb().Button("Start").Busy);

        Assert.Equal("No button is named \"Start\". The buttons are named: "
            + string.Join(", ", Enumerable.Range(1, 12).Select(number => $"\"Stop {number}\"")) + " and 2 more.",
            error.Message);
    }

    [Fact]
    public async Task Link_ShouldListTheLinksThere_WhenNoneHasTheName()
    {
        var cut = Render<TbLink>(parameters => parameters.Add(link => link.Href, "trails")
            .AddChildContent("All trails"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Link("Trails").ClickAsync());

        Assert.Equal("No link is named \"Trails\". The links are named: \"All trails\".", error.Message);
    }

    [Fact]
    public async Task Select_ShouldListTheOptions_WhenNoOptionReadsTheText()
    {
        var cut = RenderForm();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Select("Size").ChooseAsync("Huge"));

        Assert.Equal("No option reads \"Huge\". The options read: \"Small\", \"Medium\", \"Large\".", error.Message);
    }

    [Fact]
    public async Task Select_ShouldSaySeveralOptionsReadTheText_WhenTwoShareIt()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                foreach (var (index, value) in new[] { "pine-north", "pine-south" }.Index())
                {
                    builder.OpenComponent<TbOption<string>>(index * 3);
                    builder.AddAttribute(index * 3 + 1, nameof(TbOption<string>.Value), value);
                    builder.AddAttribute(index * 3 + 2, nameof(TbOption<string>.Text), "Pine");
                    builder.CloseComponent();
                }
            })));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Select("Trail").ChooseAsync("Pine"));

        Assert.Equal("2 options read \"Pine\"; give each option a name of its own.", error.Message);
    }

    [Fact]
    public async Task NumberField_ShouldFindTheNumberField_WhenASliderSharesItsLabel()
    {
        int? threshold = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbSlider<int>>(0);
            builder.AddAttribute(1, nameof(TbSlider<int>.Label), "Threshold");
            builder.CloseComponent();
            builder.OpenComponent<TbNumberField<int?>>(2);
            builder.AddAttribute(3, nameof(TbNumberField<int?>.Label), "Threshold");
            builder.AddAttribute(4, nameof(TbNumberField<int?>.ValueChanged),
                EventCallback.Factory.Create<int?>(this, value => threshold = value));
            builder.CloseComponent();
        });

        await cut.Tb().NumberField("Threshold").EnterAsync("40");

        Assert.Equal(40, threshold);
        Assert.Throws<InvalidOperationException>(() => cut.Tb().TextField("Threshold").Text);
    }

    [Fact]
    public async Task Checkbox_ShouldCheckAndUncheckIt_WhenTheHelperIsUsed()
    {
        var updates = false;
        var cut = Render<TbCheckbox<bool>>(parameters => parameters.Add(box => box.Label, "Email me updates")
            .Add(box => box.Value, updates).Add(box => box.ValueChanged, (bool value) => updates = value));

        await cut.Tb().Checkbox("Email me updates").CheckAsync();
        Assert.True(updates);
        cut.Render(parameters => parameters.Add(box => box.Value, updates));
        Assert.True(cut.Tb().Checkbox("Email me updates").Checked);
        await cut.Tb().Checkbox("Email me updates").UncheckAsync();

        Assert.False(updates);
    }

    [Fact]
    public async Task Switch_ShouldTurnItOnAndRefuseADisabledOne_WhenTheHelperIsUsed()
    {
        var on = false;
        var cut = Render<TbSwitch>(parameters => parameters.Add(toggle => toggle.Label, "Auto-save")
            .Add(toggle => toggle.ValueChanged, (bool value) => on = value));

        await cut.Tb().Switch("Auto-save").CheckAsync();
        Assert.True(on);
        Assert.Throws<InvalidOperationException>(() => cut.Tb().Checkbox("Auto-save").Checked);
        cut.Render(parameters => parameters.Add(toggle => toggle.Disabled, true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Switch("Auto-save").UncheckAsync());
    }

    [Fact]
    public async Task Checkbox_ShouldSayItPostsWithItsForm_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbCheckbox<bool>>(parameters => parameters.Add(box => box.Label, "Email me updates"));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Checkbox("Email me updates").CheckAsync());

        Assert.Contains("static page", failure.Message);
    }

    [Fact]
    public async Task CheckboxGroup_ShouldCheckAndUncheckOptionsByName_WhenTheHelperIsUsed()
    {
        IReadOnlyList<string> days = [];
        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.Label, "Trail days")
            .Add(group => group.ValueChanged, (IReadOnlyList<string> value) => days = value)
            .AddChildContent<TbCheckboxItem<string>>(item => item.Add(day => day.Value, "Monday")
                .AddChildContent("Monday"))
            .AddChildContent<TbCheckboxItem<string>>(item => item.Add(day => day.Value, "Friday")
                .AddChildContent("Friday")));

        await cut.Tb().CheckboxGroup("Trail days").CheckAsync("Monday", "Friday");
        Assert.Equal(["Monday", "Friday"], days);
        cut.Render(parameters => parameters.Add(group => group.Value, days));
        Assert.Equal(["Monday", "Friday"], cut.Tb().CheckboxGroup("Trail days").Checked);
        await cut.Tb().CheckboxGroup("Trail days").UncheckAsync("Monday");

        Assert.Equal(["Friday"], days);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().CheckboxGroup("Trail days").CheckAsync("Sunday"));
    }

    [Fact]
    public void CheckboxGroup_ShouldBeFoundByItsLabelAndNameOptionsByTheirOwn_WhenItHasInfoAndRichOptions()
    {
        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.Label, "Gear to carry")
            .Add(group => group.Info, "What the trip needs.").Add(group => group.Value, ["kit"])
            .AddChildContent<TbCheckboxItem<string>>(item => item.Add(gear => gear.Value, "kit")
                .AddChildContent(content =>
            {
                content.OpenElement(0, "span");
                content.AddAttribute(1, "aria-hidden", "true");
                content.AddContent(2, "+ ");
                content.CloseElement();
                content.AddContent(3, "First-aid kit");
                content.OpenElement(4, "span");
                content.AddAttribute(5, "data-part", "label");
                content.AddContent(6, " Recommended");
                content.CloseElement();
            })));

        Assert.Equal(["First-aid kit Recommended"], cut.Tb().CheckboxGroup("Gear to carry").Checked);
    }

    [Fact]
    public async Task ToggleGroup_ShouldRefuseWhatItsGroupDoesntAllow_WhenPressingOrReleasing()
    {
        var cut = Render<TbToggleGroup<string>>(parameters => parameters.Add(group => group.AriaLabel, "Map view")
            .Add(group => group.Value, "Trails")
            .AddChildContent<TbToggleItem<string>>(item => item.Add(view => view.Value, "Trails")
                .Add(view => view.Text, "Trails"))
            .AddChildContent<TbToggleItem<string>>(item => item.Add(view => view.Value, "Huts")
                .Add(view => view.Text, "Huts")));

        await Assert.ThrowsAsync<ArgumentException>(
            () => cut.Tb().ToggleGroup("Map view").ChooseAsync("Trails", "Huts"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().ToggleGroup("Map view").UnchooseAsync("Trails"));
        cut.Render(parameters => parameters.Add(group => group.Clearable, true));
        await cut.Tb().ToggleGroup("Map view").UnchooseAsync("Trails");

        Assert.Empty(cut.Tb().ToggleGroup("Map view").Chosen);
    }

    [Fact]
    public async Task ToggleGroup_ShouldSayItCantBePressed_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TbToggleGroup<string>>(parameters => parameters.Add(group => group.AriaLabel, "Map view")
            .AddChildContent<TbToggleItem<string>>(item => item.Add(view => view.Value, "Trails")
                .Add(view => view.Text, "Trails")));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().ToggleGroup("Map view").ChooseAsync("Trails"));

        Assert.Contains("static page", failure.Message);
    }

    [Fact]
    public async Task RadioGroup_ShouldChooseAnOptionAndRefuseADisabledOne_WhenTheHelperIsUsed()
    {
        string? paper = null;
        var cut = Render<TbRadioGroup<string>>(parameters => parameters.Add(group => group.Label, "Paper size")
            .Add(group => group.ValueChanged, (string value) => paper = value)
            .AddChildContent<TbRadio<string>>(radio => radio.Add(size => size.Value, "Letter")
                .AddChildContent("<span aria-hidden=\"true\">~ </span>Letter"))
            .AddChildContent<TbRadio<string>>(radio => radio.Add(size => size.Value, "Legal")
                .Add(size => size.Disabled, true)
                .AddChildContent("Legal")));

        await cut.Tb().RadioGroup("Paper size").ChooseAsync("Letter");
        Assert.Equal("Letter", paper);
        cut.Render(parameters => parameters.Add(group => group.Value, paper));

        Assert.Equal("Letter", cut.Tb().RadioGroup("Paper size").Chosen);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().RadioGroup("Paper size").ChooseAsync("Legal"));
    }

    [Fact]
    public async Task Rating_ShouldRateReadAndClearTheStars_WhenTheHelperIsUsed()
    {
        var stars = 0;
        var cut = Render<TbRating>(parameters => parameters.Add(rating => rating.Label, "Rate the ridge loop")
            .Add(rating => rating.Clearable, true).Add(rating => rating.ValueChanged, (int value) => stars = value));
        var rating = cut.Tb().Rating("Rate the ridge loop");

        await rating.RateAsync(4);
        Assert.Equal(4, stars);
        cut.Render(parameters => parameters.Add(field => field.Value, stars));
        Assert.Equal((4, 5), (rating.Value, rating.Max));
        await rating.ClearAsync();

        Assert.Equal(0, stars);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => rating.RateAsync(6));
    }

    [Theory]
    [InlineData(false, false, false, 4, false)]
    [InlineData(true, true, false, 4, false)]
    [InlineData(true, false, true, 4, false)]
    [InlineData(true, false, true, 0, false)]
    [InlineData(true, false, false, 0, true)]
    public async Task Rating_ShouldRefuseWhatAUserCantDo_WhenTheRatingIsLockedOrCantClear(bool clearable,
        bool readOnly, bool disabled, int stars, bool isStatic)
    {
        if (isStatic)
        {
            SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        }

        var cut = Render<TbRating>(parameters => parameters.Add(rating => rating.Label, "Your last rating")
            .Add(rating => rating.Value, stars).Add(rating => rating.Clearable, clearable)
            .Add(rating => rating.ReadOnly, readOnly).Add(rating => rating.Disabled, disabled));
        var rating = cut.Tb().Rating("Your last rating");

        await Assert.ThrowsAsync<InvalidOperationException>(() => rating.ClearAsync());
        if (readOnly || disabled || isStatic)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => rating.RateAsync(4));
        }
    }

    [Fact]
    public async Task ToggleGroup_ShouldPressAndLetGoOfItemsByName_WhenTheHelperIsUsed()
    {
        IReadOnlyList<string> layers = [];
        var cut = Render<TbMultiToggleGroup<string>>(parameters => parameters.Add(group => group.AriaLabel, "Layers")
            .Add(group => group.ValueChanged, (IReadOnlyList<string> value) => layers = value)
            .AddChildContent<TbToggleItem<string>>(item => item.Add(layer => layer.Value, "Trails")
                .Add(layer => layer.Text, "Trails"))
            .AddChildContent<TbToggleItem<string>>(item => item.Add(layer => layer.Value, "Huts")
                .Add(layer => layer.Text, "Huts")));

        await cut.Tb().ToggleGroup("Layers").ChooseAsync("Trails", "Huts");
        Assert.Equal(["Trails", "Huts"], cut.Tb().ToggleGroup("Layers").Chosen);
        await cut.Tb().ToggleGroup("Layers").UnchooseAsync("Trails");

        Assert.Equal(["Huts"], layers);
    }

    [Fact]
    public async Task List_ShouldFollowTheLink_WhenTheItemIsALink()
    {
        var cut = Render<TbList>(parameters => parameters.AddUnmatched("aria-label", "Camps")
            .AddChildContent<TbListItem>(item => item.Add(each => each.Href, "camps/lake")
                .AddChildContent("Lake camp")));

        await cut.Tb().List("Camps").ClickAsync("Lake camp");

        Assert.EndsWith("/camps/lake", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void List_ShouldNameTheListsThereAre_WhenNoneHasTheName()
    {
        var cut = Render<TbStack>(parameters => parameters
            .AddChildContent<TbList>(list => list.AddUnmatched("aria-label", "Camps"))
            .AddChildContent<TbList>(list => list.AddUnmatched("aria-label", "Trails")));

        var failure = Assert.Throws<InvalidOperationException>(() => cut.Tb().List("Huts").Items);

        Assert.Equal("No list is named \"Huts\". The lists are named: \"Camps\", \"Trails\".", failure.Message);
    }

    [Fact]
    public async Task List_ShouldReadTheItemsAndClickOne_WhenTheHelperIsUsed()
    {
        string? shown = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbList>(0);
            builder.AddAttribute(1, "aria-label", "Campsites");
            builder.AddComponentParameter(2, nameof(TbList.ChildContent), (RenderFragment)(items =>
            {
                Item(items, 0, "Pine Hollow", disabled: false);
                Item(items, 10, "Cedar Flats", disabled: true);
                items.OpenComponent<TbListItem>(20);
                items.AddComponentParameter(21, nameof(TbListItem.Href), "/camps/lake");
                items.AddComponentParameter(22, nameof(TbListItem.Disabled), true);
                items.AddComponentParameter(23, nameof(TbListItem.ChildContent), (RenderFragment)(text =>
                    text.AddContent(0, "Lake camp")));
                items.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.Equal(["Pine Hollow", "Cedar Flats", "Lake camp"], cut.Tb().List("Campsites").Items);
        await cut.Tb().List().ClickAsync("Pine Hollow");

        Assert.Equal("Pine Hollow", shown);
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().List().ClickAsync("Cedar Flats"));
        // A disabled item that would be a link draws as plain content.
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().List().ClickAsync("Lake camp"));
        Assert.Contains("is disabled", failure.Message);

        void Item(RenderTreeBuilder items, int sequence, string name, bool disabled)
        {
            items.OpenComponent<TbListItem>(sequence);
            items.AddComponentParameter(sequence + 1, nameof(TbListItem.Disabled), disabled);
            items.AddComponentParameter(sequence + 2, nameof(TbListItem.Description), (RenderFragment)(text =>
                text.AddContent(0, "4 sites")));
            items.AddComponentParameter(sequence + 3, nameof(TbListItem.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, () => shown = name));
            items.AddComponentParameter(sequence + 4, nameof(TbListItem.ChildContent), (RenderFragment)(text =>
                text.AddContent(0, name)));
            items.CloseComponent();
        }
    }

    [Fact]
    public async Task CommandPalette_ShouldSearchAndRunACommand_WhenItIsOpen()
    {
        string? chosen = null;
        var cut = RenderCommandPalette(open: true, command => chosen = command);

        await cut.Tb().CommandPalette().SearchAsync("tab");
        Assert.Equal(["New tab", "Close tab"], cut.Tb().CommandPalette("Command palette").Results);
        await cut.Tb().CommandPalette().ChooseAsync("Close tab");

        Assert.Equal("Close tab", chosen);
        Assert.False(cut.Tb().CommandPalette().IsOpen);
    }

    [Fact]
    public async Task CommandPalette_ShouldOpenAndStayOpen_WhenItsShortcutIsPressed()
    {
        var cut = RenderCommandPalette(open: false, _ => { });

        await cut.Tb().CommandPalette().OpenAsync();
        await cut.Tb().CommandPalette().OpenAsync();

        Assert.True(cut.Tb().CommandPalette().IsOpen);
        cut.Render(parameters => parameters.Add(palette => palette.Open, false).Add(palette => palette.Shortcut, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().CommandPalette().OpenAsync());
    }

    [Fact]
    public async Task CommandPalette_ShouldThrow_WhenClosedOrTheCommandIsDisabled()
    {
        var cut = RenderCommandPalette(open: false, _ => { });

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().CommandPalette().SearchAsync("tab"));
        cut.Render(parameters => parameters.Add(palette => palette.Open, true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().CommandPalette().ChooseAsync("Delete everything"));
        Assert.True(cut.Tb().CommandPalette().IsOpen);
    }

    [Fact]
    public async Task CommandPalette_ShouldReadAndRunTemplatedCommands_WhenItsLabelHasAnApostrophe()
    {
        string? chosen = null;
        var cut = Render<TbCommandPalette<string>>(parameters => parameters.Add(palette => palette.Items, ["Pine loop"])
            .Add(palette => palette.Label, "Today's picks").Add(palette => palette.Open, true)
            .Add(palette => palette.OnChoose, (string pick) => chosen = pick)
            .Add(palette => palette.ItemTemplate, pick => builder =>
            {
                // A part named label inside the template, as a chip or button in it would have.
                builder.AddContent(0, $"{pick} ");
                builder.OpenElement(1, "small");
                builder.AddAttribute(2, "data-part", "label");
                builder.AddContent(3, "new");
                builder.CloseElement();
            }));

        Assert.Equal(["Pine loop new"], cut.Tb().CommandPalette("Today's picks").Results);
        await cut.Tb().CommandPalette("Today's picks").ChooseAsync("Pine loop new");

        Assert.Equal("Pine loop", chosen);
    }

    private static RenderFragment NameColumn => builder =>
    {
        builder.OpenComponent<TbColumn<Trail>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
            (System.Linq.Expressions.Expression<Func<Trail, object?>>)(trail => trail.Name));
        builder.CloseComponent();
    };

    private IRenderedComponent<TbCommandPalette<string>> RenderCommandPalette(bool open, Action<string> chosen) =>
        Render<TbCommandPalette<string>>(parameters => parameters
            .Add(palette => palette.Items, ["New tab", "Close tab", "Delete everything"])
            .Add(palette => palette.ItemShortcut, command => command == "New tab" ? "Ctrl+T" : null)
            .Add(palette => palette.ItemDisabled, command => command == "Delete everything")
            .Add(palette => palette.Open, open).Add(palette => palette.OnChoose, chosen));

    private IRenderedComponent<ContainerFragment> RenderForm() => Render(BuildForm);

    private IRenderedComponent<TbSortable<string>> RenderSortable(List<string> items,
        Action<SortableMove<string>> onMove, Func<SortableMove<string>, Task<MoveCheck>>? canMove = null) =>
        Render<TbSortable<string>>(parameters => parameters.Add(sortable => sortable.Label, "Rules")
            .Add(sortable => sortable.Items, items).Add(sortable => sortable.OnMove, onMove)
            .Add(sortable => sortable.CanMove, canMove)
            .Add(sortable => sortable.ItemTemplate, item => builder => builder.AddContent(0, item)));

    private IRenderedComponent<TbRichTextEditor> RenderRichTextEditor(
        Action<ComponentParameterCollectionBuilder<TbRichTextEditor>> parameters)
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/rich-text-editor.js")
            .SetupModule("attachRichTextEditor", _ => true);
        handle.SetupVoid("setValue", _ => true).SetVoidResult();
        return Render<TbRichTextEditor>(builder => parameters(builder.Add(editor => editor.Label, "Notes")));
    }

    private void BuildForm(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbSelect<string>>(0);
        builder.AddAttribute(1, nameof(TbSelect<string>.Label), "Size");
        builder.AddAttribute(2, nameof(TbSelect<string>.Required), true);
        builder.AddAttribute(27, nameof(TbSelect<string>.Placeholder), "Pick a size");
        builder.AddAttribute(3, nameof(TbSelect<string>.Value), _size);
        builder.AddAttribute(4, nameof(TbSelect<string>.ValueChanged),
            EventCallback.Factory.Create<string>(this, value => _size = value));
        builder.AddAttribute(5, nameof(TbSelect<string>.ChildContent), Options("Small", "Medium", "Large"));
        builder.CloseComponent();

        builder.OpenComponent<TbMultiSelect<string>>(6);
        builder.AddAttribute(7, nameof(TbMultiSelect<string>.Label), "Toppings");
        builder.AddAttribute(8, nameof(TbMultiSelect<string>.Value), _toppings);
        builder.AddAttribute(9, nameof(TbMultiSelect<string>.ValueChanged),
            EventCallback.Factory.Create<IReadOnlyList<string>>(this, value => _toppings = value));
        builder.AddAttribute(10, nameof(TbMultiSelect<string>.ChildContent), Options("Olives", "Basil", "Garlic"));
        builder.CloseComponent();

        builder.OpenComponent<TbListBox<string>>(11);
        builder.AddAttribute(12, nameof(TbListBox<string>.Label), "Camp");
        builder.AddAttribute(13, nameof(TbListBox<string>.Value), _camp);
        builder.AddAttribute(14, nameof(TbListBox<string>.ValueChanged),
            EventCallback.Factory.Create<string>(this, value => _camp = value));
        builder.AddAttribute(15, nameof(TbListBox<string>.ChildContent), Options("Lakeside", "Meadow"));
        builder.CloseComponent();

        builder.OpenComponent<TbNumberField<int?>>(16);
        builder.AddAttribute(17, nameof(TbNumberField<int?>.Label), "Copies");
        builder.AddAttribute(18, nameof(TbNumberField<int?>.Value), _copies);
        builder.AddAttribute(19, nameof(TbNumberField<int?>.ValueChanged),
            EventCallback.Factory.Create<int?>(this, value => _copies = value));
        builder.CloseComponent();

        builder.OpenComponent<TbSelect<string>>(22);
        builder.AddAttribute(23, nameof(TbSelect<string>.Label), "Trail");
        builder.AddAttribute(24, nameof(TbSelect<string>.Value), _trail);
        builder.AddAttribute(25, nameof(TbSelect<string>.ValueChanged),
            EventCallback.Factory.Create<string>(this, value => _trail = value));
        builder.AddAttribute(26, nameof(TbSelect<string>.ChildContent), Options("Ridge loop", "Ridge"));
        builder.CloseComponent();

        builder.OpenComponent<TbButton>(28);
        builder.AddAttribute(29, nameof(TbButton.ChildContent), (RenderFragment)(text => text.AddContent(0, "Size")));
        builder.CloseComponent();

        builder.OpenComponent<TbSelect<string>>(30);
        builder.AddAttribute(31, nameof(TbSelect<string>.Label), "Region");
        builder.AddAttribute(32, nameof(TbSelect<string>.Disabled), true);
        builder.AddAttribute(33, nameof(TbSelect<string>.ChildContent), Options("North", "South"));
        builder.CloseComponent();

        builder.OpenComponent<TbNumberField<int?>>(34);
        builder.AddAttribute(35, nameof(TbNumberField<int?>.Label), "Budget");
        builder.AddAttribute(36, nameof(TbNumberField<int?>.Format), "N0");
        builder.AddAttribute(37, nameof(TbNumberField<int?>.Value), _budget);
        builder.AddAttribute(38, nameof(TbNumberField<int?>.ValueChanged),
            EventCallback.Factory.Create<int?>(this, value => _budget = value));
        builder.CloseComponent();

        builder.OpenComponent<TbTabs>(20);
        builder.AddAttribute(21, nameof(TbTabs.ChildContent), (RenderFragment)(tabs =>
        {
            foreach (var (index, text) in new[] { "Gear", "Route" }.Index())
            {
                tabs.OpenComponent<TbTab>(index * 2);
                tabs.AddAttribute(index * 2 + 1, nameof(TbTab.Text), text);
                tabs.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }

    private static RenderFragment Items(params (string Value, string Text)[] items) => builder =>
    {
        foreach (var (index, (value, text)) in items.Index())
        {
            builder.OpenComponent<TbTreeItem<string>>(index * 3);
            builder.AddAttribute(index * 3 + 1, nameof(TbTreeItem<string>.Value), value);
            builder.AddAttribute(index * 3 + 2, nameof(TbTreeItem<string>.Text), text);
            builder.CloseComponent();
        }
    };

    [Fact]
    public void Option_ShouldTellWhetherAnOptionIsDisabledOrChosen_WhenASelectOrListBoxReadsIt()
    {
        RenderFragment options = builder =>
        {
            builder.OpenComponent<TbOption<string>>(0);
            builder.AddAttribute(1, nameof(TbOption<string>.Value), "Lakeside");
            builder.CloseComponent();
            builder.OpenComponent<TbOption<string>>(2);
            builder.AddAttribute(3, nameof(TbOption<string>.Value), "Meadow");
            builder.AddAttribute(4, nameof(TbOption<string>.Disabled), true);
            builder.CloseComponent();
        };
        var select = Render<TbSelect<string>>(parameters => parameters.Add(field => field.Label, "Camp")
            .Add(field => field.Value, "Lakeside").Add(field => field.ChildContent, options));
        var list = Render<TbListBox<string>>(parameters => parameters.Add(field => field.Label, "Site")
            .Add(field => field.Value, "Lakeside").Add(field => field.ChildContent, options));

        var (camp, site) = (select.Tb().Select("Camp"), list.Tb().ListBox("Site"));
        Assert.Equal(new OptionInfo("Lakeside", Disabled: false, Chosen: true), camp.Option("Lakeside"));
        Assert.Equal(new OptionInfo("Meadow", Disabled: true, Chosen: false), camp.Option("Meadow"));
        Assert.Equal(new OptionInfo("Lakeside", Disabled: false, Chosen: true), site.Option("Lakeside"));
        Assert.Equal(new OptionInfo("Meadow", Disabled: true, Chosen: false), site.Option("Meadow"));
    }

    [Theory]
    [InlineData(typeof(TbSelect<string>), "Select")]
    [InlineData(typeof(TbListBox<string>), "ListBox")]
    [InlineData(typeof(TbTreeSelect<string, string>), "TreeSelect")]
    public void Disabled_ShouldBeTrueOnlyForTheDisabledField_WhenReadFromItsHelper(Type field, string helper)
    {
        var cut = Render(builder =>
        {
            foreach (var (label, disabled) in new[] { ("Open camp", false), ("Closed camp", true) })
            {
                builder.OpenComponent(0, field);
                builder.AddAttribute(1, nameof(TbSelect<string>.Label), label);
                builder.AddAttribute(2, nameof(TbSelect<string>.Disabled), disabled);
                if (helper == "TreeSelect")
                {
                    builder.AddAttribute(3, nameof(TbTreeSelect<string, string>.ItemText),
                        (Func<string, string>)(item => item));
                }

                builder.CloseComponent();
            }
        });

        bool Disabled(string label) => helper switch
        {
            "Select" => cut.Tb().Select(label).Disabled,
            "ListBox" => cut.Tb().ListBox(label).Disabled,
            _ => cut.Tb().TreeSelect(label).Disabled,
        };

        Assert.False(Disabled("Open camp"));
        Assert.True(Disabled("Closed camp"));
    }

    private static RenderFragment Options(params string[] values) => builder =>
    {
        foreach (var (index, value) in values.Index())
        {
            builder.OpenComponent<TbOption<string>>(index * 2);
            builder.AddAttribute(index * 2 + 1, nameof(TbOption<string>.Value), value);
            builder.CloseComponent();
        }
    };

    private sealed record Trail(string Name, string Region = "");

    // A select whose options load as its list first opens, as an app might fetch them only when needed.
    private sealed class LazySizes : ComponentBase
    {
        private string? _size;
        private bool _loaded;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbSelect<string>>(0);
            builder.AddComponentParameter(1, nameof(TbSelect<string>.Label), "Size");
            builder.AddComponentParameter(2, nameof(TbSelect<string>.Value), _size);
            builder.AddComponentParameter(3, nameof(TbSelect<string>.ValueChanged),
                EventCallback.Factory.Create<string?>(this, size => _size = size));
            builder.AddComponentParameter(4, nameof(TbSelect<string>.OpenChanged),
                EventCallback.Factory.Create<bool>(this, open => _loaded |= open));
            builder.AddComponentParameter(5, nameof(TbSelect<string>.ChildContent), (RenderFragment)(options =>
            {
                if (_loaded)
                {
                    options.OpenComponent<TbOption<string>>(0);
                    options.AddComponentParameter(1, nameof(TbOption<string>.Value), "l");
                    options.AddComponentParameter(2, nameof(TbOption<string>.Text), "Large");
                    options.CloseComponent();
                }
            }));
            builder.CloseComponent();
        }
    }
}
