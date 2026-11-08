using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbListBoxTests : BunitContext
{
    private string? Trail { get; set; }

    private IReadOnlyList<string> Gear { get; set; } = [];

    public TbListBoxTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldBeAListboxNamedByItsLabel_WhenInteractive()
    {
        var cut = RenderListBox("Creek path");

        var listbox = cut.Find("[role=listbox]");
        var label = cut.Find("label");
        Assert.Null(label.GetAttribute("for"));
        Assert.Equal(label.Id, listbox.GetAttribute("aria-labelledby"));
        Assert.Equal("0", listbox.GetAttribute("tabindex"));
        Assert.False(listbox.HasAttribute("aria-multiselectable"));
        var options = cut.FindAll("[role=option]");
        Assert.Equal(["false", "true", "false"], options.Select(option => option.GetAttribute("aria-selected")));
        Assert.Equal("true", options[2].GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldShowItsRowsBeforeScrolling_WhenGivenRows()
    {
        var cut = RenderListBox(null, rows: 3);

        Assert.Contains("--tb-listbox-rows: 3", cut.Find("[role=listbox]").GetAttribute("style"));
    }

    [Fact]
    public void Click_ShouldChooseTheOption_WhenAnEnabledOptionIsClicked()
    {
        var cut = RenderListBox("Ridge loop");

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal("Creek path", Trail);
        Assert.Equal("true", cut.FindAll("[role=option]")[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Click_ShouldLeaveTheValue_WhenTheListboxIsDisabled()
    {
        var cut = RenderListBox("Ridge loop", disabled: true);

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal("Ridge loop", Trail);
        Assert.Equal("-1", cut.Find("[role=listbox]").GetAttribute("tabindex"));
        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldBeANativeList_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderListBox("Creek path", rows: 4);

        var select = cut.Find("select");
        Assert.Equal("4", select.GetAttribute("size"));
        Assert.False(select.HasAttribute("multiple"));
        Assert.Equal("Trail", select.GetAttribute("name"));
        Assert.Equal(cut.Find("label").GetAttribute("for"), select.Id);
        Assert.True(cut.FindAll("option")[1].HasAttribute("selected"));
    }

    [Fact]
    public void Render_ShouldNameAWordlessOptionByItsText_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbListBox<string>>(parameters => parameters.Add(listbox => listbox.Label, "Route")
            .AddChildContent<TbOption<string>>(option => option.Add(o => o.Value, "any").Add(o => o.Text, "Any trail")
                .AddChildContent("<svg aria-hidden=\"true\"></svg>")));

        Assert.Equal("Any trail", cut.Find("option[value=any]").InnerHtml.Trim());
    }

    [Fact]
    public void Click_ShouldToggleEachOption_WhenAMultipleListboxsOptionsAreClicked()
    {
        var cut = RenderMultiListBox(["Tent"]);

        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
        cut.FindAll("[role=option]")[1].Click();
        cut.FindAll("[role=option]")[0].Click();

        Assert.Equal(["Stove"], Gear);
    }

    [Fact]
    public void Render_ShouldBeANativeMultipleList_WhenAMultipleListboxIsRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderMultiListBox(["Tent", "Stove"]);

        Assert.True(cut.Find("select").HasAttribute("multiple"));
        Assert.Equal([true, true], cut.FindAll("option").Select(option => option.HasAttribute("selected")));
    }

    [Fact]
    public void Click_ShouldKeepTheValue_WhenReadOnly()
    {
        var cut = RenderListBox("Ridge loop");
        cut.Render(parameters => parameters.Add(listbox => listbox.ReadOnly, true));

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal("Ridge loop", Trail);
        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-readonly"));
    }

    private IRenderedComponent<TbListBox<string?>> RenderListBox(string? value, bool disabled = false, int rows = 5)
    {
        Trail = value;
        return Render<TbListBox<string?>>(parameters => parameters
            .Add(listbox => listbox.Label, "Trail")
            .Add(listbox => listbox.Rows, rows)
            .Add(listbox => listbox.Disabled, disabled)
            .Add(listbox => listbox.Value, value)
            .Add(listbox => listbox.ValueChanged, (string? next) => Trail = next)
            .Add(listbox => listbox.ValueExpression, (Expression<Func<string?>>)(() => Trail))
            .Add(listbox => listbox.ChildContent, (RenderFragment)(builder =>
            {
                AddOption<string?>(builder, 0, "Ridge loop");
                AddOption<string?>(builder, 10, "Creek path");
                AddOption<string?>(builder, 20, "Summit spur", disabled: true);
            })));
    }

    private IRenderedComponent<TbMultiListBox<string>> RenderMultiListBox(IReadOnlyList<string> value)
    {
        Gear = value;
        return Render<TbMultiListBox<string>>(parameters => parameters
            .Add(listbox => listbox.Label, "Gear")
            .Add(listbox => listbox.Value, value)
            .Add(listbox => listbox.ValueChanged, (IReadOnlyList<string> next) => Gear = next)
            .Add(listbox => listbox.ChildContent, (RenderFragment)(builder =>
            {
                AddOption<string>(builder, 0, "Tent");
                AddOption<string>(builder, 10, "Stove");
            })));
    }

    private int _gearChanges;

    private IRenderedComponent<TbMultiListBox<string>> RenderPackingList(IReadOnlyList<string> value,
        bool disabled = false, bool readOnly = false)
    {
        Gear = value;
        return Render<TbMultiListBox<string>>(parameters => parameters
            .Add(listbox => listbox.Label, "Gear")
            .Add(listbox => listbox.Value, value)
            .Add(listbox => listbox.Disabled, disabled)
            .Add(listbox => listbox.ReadOnly, readOnly)
            .Add(listbox => listbox.ValueChanged, (IReadOnlyList<string> next) =>
            {
                Gear = next;
                _gearChanges++;
            })
            .Add(listbox => listbox.ChildContent, (RenderFragment)(builder =>
            {
                AddOption<string>(builder, 0, "Tent");
                AddOption<string>(builder, 10, "Lantern", disabled: true);
                AddOption<string>(builder, 20, "Stove");
                AddOption<string>(builder, 30, "Water filter");
            })));
    }

    private ListBoxEvents Events() =>
        ((DotNetObjectReference<ListBoxEvents>)JSInterop.Invocations["attachListBox"].Single().Arguments[1]!).Value;

    [Fact]
    public async Task ChooseAll_ShouldKeepWhatIsChosenAndSkipDisabledOptions_WhenCalled()
    {
        var cut = RenderPackingList(["Stove"]);

        await cut.InvokeAsync(() => Events().OnChooseAll());

        Assert.Equal(["Stove", "Tent", "Water filter"], Gear);
        Assert.Equal(1, _gearChanges);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ChooseAll_ShouldChooseNothing_WhenTheListIsDisabledOrReadOnly(bool disabled, bool readOnly)
    {
        var cut = RenderPackingList([], disabled, readOnly);

        await cut.InvokeAsync(() => Events().OnChooseAll());

        Assert.Equal(0, _gearChanges);
    }

    [Fact]
    public async Task ChooseRange_ShouldMakeOneChangeOnceTheLastBatchIsIn_WhenTheRangeArrivesInBatches()
    {
        var cut = RenderPackingList([]);
        var ids = cut.FindAll("[role=option]").Select(option => option.Id!).ToArray();

        await cut.InvokeAsync(() => Events().OnChooseRange(ids[..2], false));
        Assert.Equal(0, _gearChanges);
        await cut.InvokeAsync(() => Events().OnChooseRange(ids[2..], true));

        Assert.Equal(["Tent", "Stove", "Water filter"], Gear);
        Assert.Equal(1, _gearChanges);
    }

    [Fact]
    public async Task ChooseRange_ShouldSkipTheId_WhenItsOptionIsGone()
    {
        var cut = RenderPackingList([]);
        var ids = cut.FindAll("[role=option]").Select(option => option.Id!).ToArray();

        await cut.InvokeAsync(() => Events().OnChooseRange(["gone", ids[0]], true));

        Assert.Equal(["Tent"], Gear);
    }

    [Fact]
    public async Task ChooseRange_ShouldChooseAnOptionOnce_WhenTwoRangesOverlap()
    {
        var cut = RenderPackingList([]);
        var ids = cut.FindAll("[role=option]").Select(option => option.Id!).ToArray();

        await cut.InvokeAsync(() => Events().OnChooseRange([ids[0], ids[2]], false));
        await cut.InvokeAsync(() => Events().OnChooseRange([ids[2], ids[3]], true));

        Assert.Equal(["Tent", "Stove", "Water filter"], Gear);
    }

    [Fact]
    public async Task ChooseRange_ShouldDropWhatItHolds_WhenMoreIdsArriveThanTheListHasOptions()
    {
        var cut = RenderPackingList([]);
        var ids = cut.FindAll("[role=option]").Select(option => option.Id!).ToArray();

        await cut.InvokeAsync(() => Events().OnChooseRange(["a", "b", "c", "d", ids[2]], false));
        await cut.InvokeAsync(() => Events().OnChooseRange([ids[0]], true));

        Assert.Equal(["Tent"], Gear);
    }

    [Fact]
    public async Task ChooseRange_ShouldStartAfresh_WhenAnEarlierRangeHasFinished()
    {
        var cut = RenderPackingList([]);
        var ids = cut.FindAll("[role=option]").Select(option => option.Id!).ToArray();
        await cut.InvokeAsync(() => Events().OnChooseRange(ids[..1], true));
        Gear = [];
        cut.Render(parameters => parameters.Add(listbox => listbox.Value, []));

        await cut.InvokeAsync(() => Events().OnChooseRange(ids[2..3], true));

        Assert.Equal(["Stove"], Gear);
    }

    private static void AddOption<TValue>(RenderTreeBuilder builder, int sequence, TValue value, bool disabled = false)
    {
        builder.OpenComponent<TbOption<TValue>>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbOption<TValue>.Value), value);
        builder.AddAttribute(sequence + 2, nameof(TbOption<TValue>.Disabled), disabled);
        builder.CloseComponent();
    }
}
