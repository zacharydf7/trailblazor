using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbMultiSelectTests : BunitContext
{
    private IReadOnlyList<string> Toppings { get; set; } = [];

    public TbMultiSelectTests() => this.AddTrailblazor();

    [Fact]
    public void Click_ShouldAddOptionsInTheOrderChosen_WhenSeveralAreClicked()
    {
        var cut = RenderMultiSelect([]);

        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
        cut.FindAll("[role=option]")[2].Click();
        cut.FindAll("[role=option]")[0].Click();

        Assert.Equal(["Olives", "Cheese"], Toppings);
        Assert.Equal(["true", "false", "true"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")));
        Assert.Equal("Cheese, Olives", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Click_ShouldStartAList_WhenTheValueIsNull()
    {
        var cut = RenderMultiSelect(null!);

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal(["Ham"], Toppings);
    }

    [Fact]
    public void Click_ShouldTakeAnOptionOut_WhenItIsAlreadyChosen()
    {
        var cut = RenderMultiSelect(["Cheese", "Ham"]);

        cut.FindAll("[role=option]")[0].Click();

        Assert.Equal(["Ham"], Toppings);
        Assert.Equal("Ham", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Clear_ShouldEmptyTheChoice_WhenTheClearButtonIsClicked()
    {
        var cut = RenderMultiSelect(["Cheese"]);

        cut.Find("[data-part=clear]").Click();

        Assert.Empty(Toppings);
        Assert.Equal("Add toppings", cut.Find("[data-part=placeholder]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Choose_ShouldShowTheValidationMessage_WhenTheChoiceIsInvalid()
    {
        var cut = RenderMultiSelect(["Cheese"],
            validation: toppings => toppings is { Count: > 0 } ? [] : ["Choose a topping."]);

        cut.FindAll("[role=option]")[0].Click();

        Assert.Equal("Choose a topping.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldBeANativeMultipleSelect_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderMultiSelect(["Cheese", "Olives"]);

        var select = cut.Find("select");
        Assert.True(select.HasAttribute("multiple"));
        Assert.Equal("Toppings", select.GetAttribute("name"));
        var options = cut.FindAll("option");
        Assert.Equal(["Cheese", "Ham", "Olives"], options.Select(option => option.GetAttribute("value")));
        Assert.Equal([true, false, true], options.Select(option => option.HasAttribute("selected")));
    }

    [Fact]
    public void SelectAll_ShouldChooseEveryEnabledOptionAfterThoseChosen_WhenSomeAreNotChosen()
    {
        var cut = RenderMultiSelect(["Olives"], selectAll: true, disabled: "Ham");

        var all = cut.Find("[data-part=select-all]");
        Assert.Equal(("option", "false", "Select all"),
            (all.GetAttribute("role"), all.GetAttribute("aria-selected"), all.TextContent.Trim()));
        all.Click();

        Assert.Equal(["Olives", "Cheese"], Toppings);
        Assert.Equal("true", cut.Find("[data-part=select-all]").GetAttribute("aria-selected"));
    }

    [Fact]
    public void SelectAll_ShouldTakeOutEveryEnabledOptionButKeepADisabledOne_WhenAllEnabledAreChosen()
    {
        var cut = RenderMultiSelect(["Ham", "Cheese", "Olives"], selectAll: true, disabled: "Ham");

        cut.Find("[data-part=select-all]").Click();

        Assert.Equal(["Ham"], Toppings);
        Assert.Equal("false", cut.Find("[data-part=select-all]").GetAttribute("aria-selected"));
    }

    [Fact]
    public void SelectAll_ShouldLeaveTheValue_WhenTheSelectIsReadOnly()
    {
        var cut = RenderMultiSelect(["Olives"], selectAll: true, readOnly: true);

        cut.Find("[data-part=select-all]").Click();

        Assert.Equal(["Olives"], Toppings);
    }

    [Fact]
    public void SelectAll_ShouldBeNamedFromTheLabelsAndDoNothing_WhenNoOptionIsEnabled()
    {
        var changes = 0;
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.SelectAll, true)
            .Add(select => select.ValueChanged, (IReadOnlyList<string> _) => changes++)
            .Add(select => select.Labels, new SelectLabels { SelectAll = "Every status" }));

        var all = cut.Find("[data-part=select-all]");
        Assert.Equal(("Every status", "true"), (all.TextContent.Trim(), all.GetAttribute("aria-disabled")));
        all.Click();

        Assert.Equal(0, changes);
    }

    [Fact]
    public void Render_ShouldHaveNoSelectAllOption_WhenNotAskedForOrRenderedStatically()
    {
        Assert.Empty(RenderMultiSelect([]).FindAll("[data-part=select-all]"));

        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        Assert.Empty(RenderMultiSelect([], selectAll: true).FindAll("[data-part=select-all], option[value='']"));
    }

    [Fact]
    public void Chip_ShouldBeNamedByItsOptionsText_WhenTheOptionShowsContentOfItsOwn()
    {
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.Chips, true)
            .Add(select => select.Value, ["gold"])
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<string>>(0);
                builder.AddAttribute(1, nameof(TbOption<string>.Value), "gold");
                builder.AddAttribute(2, nameof(TbOption<string>.Text), "Gold");
                builder.AddAttribute(3, nameof(TbOption<string>.ChildContent),
                    (RenderFragment)(content => content.AddMarkupContent(0, "<img src=\"gold.png\" />")));
                builder.CloseComponent();
            })));

        var chip = cut.Find("[data-part=chip]");
        Assert.Equal("true", chip.QuerySelector("img")!.ParentElement!.GetAttribute("aria-hidden"));
        Assert.Equal("Gold", chip.QuerySelector(".tb-visually-hidden")!.TextContent);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnOptionsValueIsNotTheItemType()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<TbMultiSelect<long>>(parameters =>
            parameters.Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<int>>(0);
                builder.AddAttribute(1, nameof(TbOption<int>.Value), 1);
                builder.CloseComponent();
            }))));

        Assert.Contains("must be a Int64", exception.Message);
    }

    private IRenderedComponent<TbMultiSelect<string>> RenderMultiSelect(IReadOnlyList<string> value,
        Func<IReadOnlyList<string>?, IEnumerable<string>>? validation = null, bool selectAll = false,
        string? disabled = null, bool readOnly = false)
    {
        Toppings = value;
        return Render<TbMultiSelect<string>>(parameters => parameters
            .Add(select => select.Label, "Toppings")
            .Add(select => select.Placeholder, "Add toppings")
            .Add(select => select.Clearable, true)
            .Add(select => select.SelectAll, selectAll)
            .Add(select => select.ReadOnly, readOnly)
            .Add(select => select.Validation, validation)
            .Add(select => select.Value, value)
            .Add(select => select.ValueChanged, (IReadOnlyList<string> next) => Toppings = next)
            .Add(select => select.ValueExpression, (Expression<Func<IReadOnlyList<string>>>)(() => Toppings))
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, "Cheese", disabled == "Cheese");
                AddOption(builder, 10, "Ham", disabled == "Ham");
                AddOption(builder, 20, "Olives", disabled == "Olives");
            })));
    }

    private static void AddOption(RenderTreeBuilder builder, int sequence, string value, bool disabled = false)
    {
        builder.OpenComponent<TbOption<string>>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbOption<string>.Value), value);
        builder.AddAttribute(sequence + 2, nameof(TbOption<string>.Disabled), disabled);
        builder.CloseComponent();
    }
}
