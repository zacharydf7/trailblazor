using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbRadioGroupTests : BunitContext
{
    // The group names its radios differently when interactive, so each test states how it renders.
    public TbRadioGroupTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
    }

    [Fact]
    public void Render_ShouldBeALabeledRadioGroupWhoseRadiosShareAName_WhenCalled()
    {
        var cut = RenderGroup("A4", helperText: "Next print run");

        var group = cut.Find("fieldset");
        Assert.Equal("radiogroup", group.GetAttribute("role"));
        Assert.Equal("Paper size", cut.Find("legend").TextContent.Trim());
        Assert.Equal(cut.Find("[data-part=helper]").Id, group.GetAttribute("aria-describedby"));
        var names = cut.FindAll("input[type=radio]").Select(radio => radio.GetAttribute("name")).Distinct();
        Assert.NotNull(Assert.Single(names));
    }

    [Fact]
    public void Render_ShouldCheckTheOptionMatchingTheValue_WhenAValueIsSet()
    {
        var cut = RenderGroup("A4");

        var radios = cut.FindAll("input[type=radio]");
        Assert.False(radios[0].HasAttribute("checked"));
        Assert.True(radios[1].HasAttribute("checked"));
        Assert.Equal("A4", radios[1].GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldRaiseValueChangedWithTheOptionsValue_WhenAnOptionIsChosen()
    {
        string? chosen = null;
        var cut = RenderGroup("Letter", onChanged: value => chosen = value);

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.Equal("A4", chosen);
        Assert.True(cut.FindAll("input[type=radio]")[1].HasAttribute("checked"));
    }

    [Fact]
    public void Render_ShouldDisableTheWholeGroup_WhenDisabledIsSet()
    {
        var cut = RenderGroup("Letter", disabled: true);

        Assert.True(cut.Find("fieldset").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldPassStylingToTheOptionsLabel_WhenAnOptionGetsClassStyleAndAttributes()
    {
        var cut = RenderGroup("Letter");

        var option = cut.Find("[data-option=letter]");
        Assert.Equal("label", option.LocalName);
        Assert.Contains("option-class", option.ClassList);
        Assert.Contains("outline: 1px solid red", option.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldDisableOneOption_WhenTheOptionIsDisabled()
    {
        var cut = RenderGroup("Letter");

        var radios = cut.FindAll("input[type=radio]");
        Assert.False(radios[0].HasAttribute("disabled"));
        Assert.True(radios[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Change_ShouldMarkTheGroupInvalidAndRequired_WhenValidationFailsOnARequiredGroup()
    {
        var cut = RenderGroup("Letter", required: true, validation: paper => paper == "A4" ? ["A4 is sold out."] : []);

        cut.FindAll("input[type=radio]")[1].Change(true);

        var group = cut.Find("fieldset");
        Assert.Equal("true", group.GetAttribute("aria-invalid"));
        Assert.Equal("true", group.GetAttribute("aria-required"));
        Assert.Equal(cut.Find("[data-part=error]").Id, group.GetAttribute("aria-describedby"));
        Assert.Contains("tb-radio-group--invalid", group.ClassList);
        Assert.NotNull(cut.Find("legend .tb-field__required"));
    }

    [Fact]
    public void Render_ShouldGiveEachGroupItsOwnName_WhenInteractiveGroupsBindSameNamedMembers()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var rows = new[] { new Row(), new Row() };

        var cut = Render(builder =>
        {
            foreach (var row in rows)
            {
                builder.OpenComponent<TbRadioGroup<string>>(0);
                builder.AddAttribute(1, "Value", row.Answer);
                builder.AddAttribute(2, "ValueExpression", Answer(row));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(options => AddOption(options, 0, "Yes")));
                builder.CloseComponent();
            }
        });

        var names = cut.FindAll("input[type=radio]").Select(radio => radio.GetAttribute("name")).ToList();
        Assert.Equal(2, names.Distinct().Count());
    }

    [Fact]
    public void Render_ShouldPutInputAttributesAndClassesOnEachRadio_WhenTheGroupHasThem()
    {
        var cut = Render<TbRadioGroup<string>>(parameters => parameters
            .Add(group => group.InputAttributes, new Dictionary<string, object> { ["name"] = "paper", ["data-x"] = "1" })
            .Add(group => group.Classes, new ChoiceClasses { Input = "my-input" })
            .Add(group => group.ChildContent, (RenderFragment)(builder => AddOption(builder, 0, "A4"))));

        var radio = cut.Find("input[type=radio]");
        Assert.Equal("paper", radio.GetAttribute("name"));
        Assert.Equal("1", radio.GetAttribute("data-x"));
        Assert.Contains("my-input", radio.ClassList);
    }

    [Fact]
    public void Change_ShouldAddTheGroupsPartClasses_WhenClassesAreSet()
    {
        var cut = Render<TbRadioGroup<string>>(parameters => parameters
            .Add(group => group.Label, "Paper size")
            .Add(group => group.HelperText, "Pick one")
            .Add(group => group.Validation, (string? paper) => paper == "A4" ? ["A4 is sold out."] : [])
            .Add(group => group.Classes,
                new ChoiceClasses { Label = "my-label", Helper = "my-helper", Error = "my-error" })
            .Add(group => group.ChildContent, (RenderFragment)(builder => AddOption(builder, 0, "A4"))));

        cut.Find("input[type=radio]").Change(true);

        Assert.Contains("my-label", cut.Find("legend[data-part=label]").ClassList);
        Assert.Contains("my-helper", cut.Find("[data-part=helper]").ClassList);
        Assert.Contains("my-error", cut.Find("[data-part=error]").ClassList);
    }

    [Fact]
    public void Render_ShouldThrow_WhenARadioIsOutsideAGroup()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbRadio<string>>(parameters => parameters
            .Add(radio => radio.Value, "A4")));
    }

    // Every row's group binds a member with the same path, as a @foreach over rows does.
    private static System.Linq.Expressions.Expression<Func<string>> Answer(Row row) => () => row.Answer;

    private IRenderedComponent<TbRadioGroup<string>> RenderGroup(string value, Action<string>? onChanged = null,
        string? helperText = null, bool disabled = false, bool required = false,
        Func<string?, IEnumerable<string>>? validation = null) =>
        Render<TbRadioGroup<string>>(parameters =>
        {
            parameters
                .Add(group => group.Label, "Paper size")
                .Add(group => group.Required, required)
                .Add(group => group.Validation, validation)
                .Add(group => group.Value, value)
                .Add(group => group.HelperText, helperText)
                .Add(group => group.Disabled, disabled)
                .Add(group => group.ChildContent, (RenderFragment)(builder =>
                {
                    AddOption(builder, 0, "Letter", ("Class", "option-class"), ("Style", "outline: 1px solid red"),
                        ("data-option", "letter"));
                    AddOption(builder, 10, "A4");
                    AddOption(builder, 20, "Legal", ("Disabled", true));
                }));
            parameters.Add(group => group.ValueChanged, (string next) => onChanged?.Invoke(next));
        });

    private static void AddOption(RenderTreeBuilder builder, int sequence, string value,
        params (string Name, object Value)[] attributes)
    {
        builder.OpenComponent<TbRadio<string>>(sequence);
        builder.AddAttribute(sequence + 1, "Value", value);
        foreach (var (name, attributeValue) in attributes)
        {
            builder.AddAttribute(sequence + 2, name, attributeValue);
        }

        builder.AddAttribute(sequence + 3, "ChildContent", (RenderFragment)(content => content.AddContent(0, value)));
        builder.CloseComponent();
    }

    private sealed class Row
    {
        public string Answer { get; set; } = "";
    }
}
