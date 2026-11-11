using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbFormStateTests : BunitContext
{
    public TbFormStateTests() => this.AddTrailblazor();

    [Fact]
    public void Disabled_ShouldTurnOffEveryFieldAndBackOn_WhenTheFormsDisabledChanges()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Disabled, true)
            .Add(form => form.ChildContent, (RenderFragment)Fields));
        Assert.True(cut.Find("input").HasAttribute("disabled"));
        Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-disabled"));

        cut.Render(parameters => parameters.Add(form => form.Disabled, false));

        Assert.False(cut.Find("input").HasAttribute("disabled"));
        Assert.Null(cut.Find("[role=combobox]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void ReadOnly_ShouldMakeEveryFieldReadOnlyAndBack_WhenTheFormsReadOnlyChanges()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ReadOnly, true)
            .Add(form => form.ChildContent, (RenderFragment)Fields));
        Assert.True(cut.Find("input").HasAttribute("readonly"));
        Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-readonly"));

        cut.Render(parameters => parameters.Add(form => form.ReadOnly, false));

        Assert.False(cut.Find("input").HasAttribute("readonly"));
        Assert.Null(cut.Find("[role=combobox]").GetAttribute("aria-readonly"));
    }

    [Fact]
    public void Disabled_ShouldKeepAFieldsOwnSetting_WhenTheFormIsntDisabled()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
            builder.AddAttribute(2, nameof(TbTextField<string>.Disabled), true);
            builder.CloseComponent();
        })));

        cut.Render(parameters => parameters.Add(form => form.Disabled, true));
        cut.Render(parameters => parameters.Add(form => form.Disabled, false));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void ReadOnly_ShouldKeepAFieldsOwnSetting_WhenTheFormIsntReadOnly()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
            builder.AddAttribute(2, nameof(TbTextField<string>.ReadOnly), true);
            builder.CloseComponent();
        })));

        cut.Render(parameters => parameters.Add(form => form.ReadOnly, true));
        cut.Render(parameters => parameters.Add(form => form.ReadOnly, false));

        Assert.True(cut.Find("input").HasAttribute("readonly"));
    }

    [Fact]
    public void Disabled_ShouldLeaveADataGridsPagerOn_WhenTheGridIsInADisabledForm()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Disabled, true)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbDataGrid<string>>(0);
                builder.AddAttribute(1, nameof(TbDataGrid<string>.Items), (IReadOnlyList<string>)["A", "B", "C"]);
                builder.AddAttribute(2, nameof(TbDataGrid<string>.PageSize), 2);
                builder.AddAttribute(3, nameof(TbDataGrid<string>.PageSizes), (IReadOnlyList<int>)[2, 5]);
                builder.AddAttribute(4, nameof(TbDataGrid<string>.ChildContent), (RenderFragment)(columns =>
                {
                    columns.OpenComponent<TbColumn<string>>(0);
                    columns.AddAttribute(1, nameof(TbColumn<string>.Title), "Trail");
                    columns.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        Assert.Null(cut.Find(".tb-data-grid__page-size [role=combobox]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Disabled_ShouldLeaveACompactPagersPageBoxOn_WhenThePagerIsInADisabledForm()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Disabled, true)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbPagination>(0);
                builder.AddAttribute(1, nameof(TbPagination.PageCount), 5);
                builder.AddAttribute(2, nameof(TbPagination.Compact), true);
                builder.CloseComponent();
            })));

        Assert.False(cut.Find("[data-part=page-box] input").HasAttribute("disabled"));
    }

    // A text field and a select, which renders its own combobox rather than an input.
    private static void Fields(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
        builder.CloseComponent();
        builder.OpenComponent<TbSelect<string>>(2);
        builder.AddAttribute(3, nameof(TbSelect<string>.Label), "Size");
        builder.CloseComponent();
    }
}
