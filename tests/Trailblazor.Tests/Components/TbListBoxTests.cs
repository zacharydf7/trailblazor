using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbListBoxTests : BunitContext
{
    private string? Trail { get; set; }

    private IReadOnlyList<string> Gear { get; set; } = [];

    public TbListBoxTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

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

    private static void AddOption<TValue>(RenderTreeBuilder builder, int sequence, TValue value, bool disabled = false)
    {
        builder.OpenComponent<TbOption<TValue>>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbOption<TValue>.Value), value);
        builder.AddAttribute(sequence + 2, nameof(TbOption<TValue>.Disabled), disabled);
        builder.CloseComponent();
    }
}
