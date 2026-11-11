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

    private sealed record Trail(string Name);
}
