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
        Func<IReadOnlyList<string>?, IEnumerable<string>>? validation = null)
    {
        Toppings = value;
        return Render<TbMultiSelect<string>>(parameters => parameters
            .Add(select => select.Label, "Toppings")
            .Add(select => select.Placeholder, "Add toppings")
            .Add(select => select.Clearable, true)
            .Add(select => select.Validation, validation)
            .Add(select => select.Value, value)
            .Add(select => select.ValueChanged, (IReadOnlyList<string> next) => Toppings = next)
            .Add(select => select.ValueExpression, (Expression<Func<IReadOnlyList<string>>>)(() => Toppings))
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, "Cheese");
                AddOption(builder, 10, "Ham");
                AddOption(builder, 20, "Olives");
            })));
    }

    private static void AddOption(RenderTreeBuilder builder, int sequence, string value)
    {
        builder.OpenComponent<TbOption<string>>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbOption<string>.Value), value);
        builder.CloseComponent();
    }
}
