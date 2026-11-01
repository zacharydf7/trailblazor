using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Field;

public class FieldIdTests : BunitContext
{
    public FieldIdTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Id_ShouldNameTheInputItsLabelAndItsDescription_WhenGivenToATextField()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Email")
            .Add(field => field.HelperText, "We never share it.").AddUnmatched("id", "email")
            .AddUnmatched("data-section", "contact"));

        var input = cut.Find("input");
        Assert.Equal("email", input.Id);
        Assert.Equal("email", cut.Find("label").GetAttribute("for"));
        Assert.Contains("We never share it.", cut.Find($"#{input.GetAttribute("aria-describedby")}").TextContent);
        var root = cut.Find(".tb-field");
        Assert.True(string.IsNullOrEmpty(root.Id));
        Assert.Equal("contact", root.GetAttribute("data-section"));
    }

    [Fact]
    public void Id_ShouldNameTheBox_WhenGivenToACheckbox()
    {
        var cut = Render<TbCheckbox<bool>>(parameters => parameters.Add(box => box.Label, "Subscribe")
            .AddUnmatched("id", "subscribe"));

        Assert.Equal("subscribe", cut.Find("input[type=checkbox]").Id);
        Assert.Single(cut.FindAll("#subscribe"));
    }

    [Fact]
    public void Id_ShouldNameTheCombobox_WhenGivenToASelect()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Size")
            .AddUnmatched("id", "size"));

        Assert.Equal("size", cut.Find("[role=combobox]").Id);
        Assert.Single(cut.FindAll("#size"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Id_ShouldLeaveTheFieldItsOwnAndItsLabelWired_WhenBoundToNothing(string? id)
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Email")
            .AddUnmatched("id", id));

        var input = cut.Find("input");
        Assert.False(string.IsNullOrEmpty(input.Id));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
    }

    [Fact]
    public void Id_ShouldNameTheInputOnlyOnce_WhenWrittenInCapitals()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Email")
            .AddUnmatched("ID", "email"));

        Assert.Equal("email", Assert.Single(cut.FindAll("#email")).Id);
        Assert.Equal("input", cut.Find("#email").LocalName);
    }

    [Fact]
    public void Id_ShouldStayOnTheRoot_WhenGivenToAGroup()
    {
        var cut = Render<TbRadioGroup<string>>(parameters => parameters.Add(group => group.Label, "Pace")
            .AddUnmatched("id", "pace"));

        Assert.Equal("fieldset", Assert.Single(cut.FindAll("#pace")).LocalName);
    }

    [Fact]
    public void Id_ShouldStayOnTheRoot_WhenGivenToAFieldWithTwoBoxes()
    {
        var cut = Render<TbDateRangePicker<DateOnly>>(parameters => parameters.Add(picker => picker.Label, "Dates")
            .AddUnmatched("id", "dates"));

        Assert.Equal("dates", cut.Find(".tb-field").Id);
        Assert.Single(cut.FindAll("#dates"));
    }
}
