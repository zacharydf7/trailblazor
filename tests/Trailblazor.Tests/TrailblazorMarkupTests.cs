using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

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
    public async Task Tabs_ShouldSelectTheTab_WhenSelectedByItsText()
    {
        var cut = RenderForm();

        await cut.Tb().Tabs().SelectAsync("Route");

        Assert.Equal("Route", cut.Tb().Tabs().Selected);
        Assert.Equal(["Gear", "Route"], cut.Tb().Tabs().Tabs);
    }

    [Fact]
    public void Field_ShouldSayNoFieldHasTheLabel_WhenNoneDoes()
    {
        var cut = RenderForm();

        var error = Assert.Throws<InvalidOperationException>(() => cut.Tb().Select("Color").Options);

        Assert.Equal("No field is labeled \"Color\".", error.Message);
    }

    private IRenderedComponent<ContainerFragment> RenderForm() => Render(BuildForm);

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

    private static RenderFragment Options(params string[] values) => builder =>
    {
        foreach (var (index, value) in values.Index())
        {
            builder.OpenComponent<TbOption<string>>(index * 2);
            builder.AddAttribute(index * 2 + 1, nameof(TbOption<string>.Value), value);
            builder.CloseComponent();
        }
    };
}
