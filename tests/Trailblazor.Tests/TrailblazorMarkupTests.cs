using System.Globalization;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

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
    public async Task Select_ShouldChooseTheOptionAndShowIt_WhenChosenByItsText()
    {
        var cut = RenderForm();

        await cut.Tb().Select("Size").ChooseAsync("Medium");

        Assert.Equal("Medium", _size);
        Assert.Equal("Medium", cut.Tb().Select("Size").Text);
        Assert.Equal(["Small", "Medium", "Large"], cut.Tb().Select("Size").Options);
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
        Assert.Throws<InvalidOperationException>(() => grid.Row("Elk Pass"));
        Assert.Throws<InvalidOperationException>(() => cut.Tb().Grid("Camps").Row("Lake camp"));
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
    public void Field_ShouldSayNoFieldHasTheLabel_WhenNoneDoes()
    {
        var cut = RenderForm();

        var error = Assert.Throws<InvalidOperationException>(() => cut.Tb().Select("Color").Options);

        Assert.Equal("No field is labeled \"Color\".", error.Message);
    }

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
}
