using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbCheckboxTests : BunitContext
{
    private const string Module = "./_content/Trailblazor/checkbox.js";

    public TbCheckboxTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldMarkTheBoxReadOnlyButNotDisabled_WhenReadOnly()
    {
        var cut = Render<TbCheckbox<bool>>(parameters => parameters.Add(box => box.ReadOnly, true));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-readonly"));
        Assert.False(input.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldBeALabeledCheckboxThatPostsTrue_WhenCalled()
    {
        var cut = Render<TbCheckbox<bool>>(parameters => parameters
            .Add(checkbox => checkbox.Label, "Subscribe")
            .Add(checkbox => checkbox.Value, true)
            .Add(checkbox => checkbox.HelperText, "Monthly"));

        var input = cut.Find("label > input");
        Assert.Equal("checkbox", input.GetAttribute("type"));
        Assert.Equal("true", input.GetAttribute("value"));
        Assert.True(input.HasAttribute("checked"));
        Assert.Equal(cut.Find("[data-part=helper]").Id, input.GetAttribute("aria-describedby"));
        Assert.Contains("Subscribe", cut.Find("label").TextContent);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Change_ShouldRaiseValueChanged_WhenToggled(bool isChecked)
    {
        var value = !isChecked;
        var cut = Render<TbCheckbox<bool>>(parameters => parameters
            .Add(checkbox => checkbox.Value, value)
            .Add(checkbox => checkbox.ValueChanged, (bool next) => value = next));

        cut.Find("input").Change(isChecked);

        Assert.Equal(isChecked, value);
    }

    [Fact]
    public void Render_ShouldMarkTheInputMixed_WhenANullableValueIsNull()
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupVoid("setIndeterminate", _ => true);

        Render<TbCheckbox<bool?>>();

        Assert.True((bool)module.VerifyInvoke("setIndeterminate").Arguments[1]!);
    }

    [Fact]
    public void Render_ShouldClearTheMixedState_WhenTheValueBecomesChecked()
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupVoid("setIndeterminate", _ => true);
        var cut = Render<TbCheckbox<bool?>>();

        cut.Render(parameters => parameters.Add(checkbox => checkbox.Value, true));

        var calls = module.VerifyInvoke("setIndeterminate", calledTimes: 2);
        Assert.False((bool)calls[1].Arguments[1]!);
    }

    [Fact]
    public void Change_ShouldShowMixedAgain_WhenTheParentKeepsTheValueNull()
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupVoid("setIndeterminate", _ => true);
        var cut = Render<TbCheckbox<bool?>>(parameters => parameters
            .Add(checkbox => checkbox.ValueChanged, (bool? _) => { }));

        cut.Find("input").Change(true);
        cut.Render(parameters => parameters.Add(checkbox => checkbox.Value, (bool?)null));

        var calls = module.VerifyInvoke("setIndeterminate", calledTimes: 2);
        Assert.All(calls, call => Assert.True((bool)call.Arguments[1]!));
    }

    [Fact]
    public void Change_ShouldMarkTheInputInvalid_WhenValidationFails()
    {
        var cut = Render<TbCheckbox<bool>>(parameters => parameters
            .Add(checkbox => checkbox.Value, true)
            .Add(checkbox => checkbox.Validation, (bool accepted) => accepted ? [] : ["Accept the terms."]));

        cut.Find("input").Change(false);

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[data-part=error]").Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("Accept the terms.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Change_ShouldAddEachPartsClass_WhenClassesAreSet()
    {
        var cut = Render<TbCheckbox<bool>>(parameters => parameters
            .Add(checkbox => checkbox.Value, true)
            .Add(checkbox => checkbox.Label, "Accept the terms")
            .Add(checkbox => checkbox.HelperText, "Needed to sign up")
            .Add(checkbox => checkbox.Validation, (bool accepted) => accepted ? [] : ["Accept the terms."])
            .Add(checkbox => checkbox.Classes,
                new ChoiceClasses { Label = "my-label", Input = "my-input", Helper = "my-helper", Error = "my-error" }));

        cut.Find("input").Change(false);

        Assert.Contains("my-label", cut.Find("[data-part=label]").ClassList);
        Assert.Contains("my-input", cut.Find("[data-part=input]").ClassList);
        Assert.Contains("my-helper", cut.Find("[data-part=helper]").ClassList);
        Assert.Contains("my-error", cut.Find("[data-part=error]").ClassList);
    }

    [Fact]
    public void Render_ShouldNotCallScript_WhenTheValueIsNotMixed()
    {
        var cut = Render<TbCheckbox<bool?>>(parameters => parameters.Add(checkbox => checkbox.Value, false));

        Assert.False(cut.Find("input").HasAttribute("checked"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void Render_ShouldThrow_WhenGivenANonBoolType()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbCheckbox<int>>());
    }
}
