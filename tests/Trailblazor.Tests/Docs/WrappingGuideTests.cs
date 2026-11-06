using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class WrappingGuideTests : DocsTest
{
    [Fact]
    public async Task AppTextField_ShouldBindAndSubmit_WhenBoundWithBindValue()
    {
        var cut = Render<AppComponentsExample>();

        await cut.Find("input[name='_trail']").ChangeAsync(new() { Value = "Ridge loop" });
        await cut.Find("form").SubmitAsync();

        Assert.Equal("Saved Ridge loop.", cut.Find("output").TextContent);
    }

    [Fact]
    public async Task AppTextField_ShouldPassParametersItDoesntDeclareThrough_WhenTheFormValidates()
    {
        var cut = Render<AppComponentsExample>();
        Assert.Equal("As it appears on the map.", cut.Find("[data-part=helper]").TextContent.Trim());
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-required"));

        await cut.Find("form").SubmitAsync();

        Assert.Equal("Enter a trail name.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void AppTextField_ShouldApplyItsDefault_WhenThePageSetsNoVariant()
    {
        var cut = Render<AppComponentsExample>();

        Assert.Contains("tb-field--filled", cut.Find(".tb-field").ClassList);
    }

    [Fact]
    public void AppTextField_ShouldPassATypedValueThrough_WhenItIsWrittenWithAnAt()
    {
        var cut = Render<AppComponentsExample>();

        Assert.Contains("tb-field--large", cut.Find(".tb-field").ClassList);
    }

    [Fact]
    public async Task AppTextField_ShouldForwardFocusToItsInput_WhenFocusAsyncIsCalled()
    {
        var cut = Render<AppComponentsExample>();

        await Button(cut, "Edit the name").ClickAsync(new());

        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void AppButton_ShouldLetThePageOverrideItsDefaults_WhenBothAreSet()
    {
        var cut = Render<AppComponentsExample>();

        Assert.Contains("tb-button--outlined", Button(cut, "Edit the name").ClassList);
        Assert.Contains("tb-button--filled", Button(cut, "Save").ClassList);
        Assert.Equal("submit", Button(cut, "Save").GetAttribute("type"));
    }

    [Fact]
    public async Task Wrapper_ShouldWorkWithTheTestingHelpers_WhenItWrapsASelect()
    {
        var chosen = "";
        var cut = Render<WrappedSelect>(parameters =>
            parameters.Add(select => select.Chosen, (string? size) => chosen = size!));

        await cut.Tb().Select("Size").ChooseAsync("Large");

        Assert.Equal("Large", chosen);
    }

    [Fact]
    public void TelemetryName_ShouldPassThroughAWrapper_WhenSetOnIt()
    {
        var cut = Render<AppComponentsExample>();

        Assert.Contains(cut.FindComponents<TbButton>(), button => button.Instance.TelemetryName == "edit-name");
        Assert.Equal("trail-name", cut.FindComponent<TbTextField<string>>().Instance.TelemetryName);
    }

    [Fact]
    public void AppTextField_ShouldRequireItsLabel_WhenDeclared()
    {
        var label = typeof(AppTextField).GetProperty(nameof(AppTextField.Label))!;

        Assert.True(label.IsDefined(typeof(Microsoft.AspNetCore.Components.EditorRequiredAttribute), false));
    }

    // A wrapper that adds no markup of its own, around a component the testing helpers can drive.
    private sealed class WrappedSelect : ComponentBase
    {
        [Parameter]
        public EventCallback<string?> Chosen { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbSelect<string>>(0);
            builder.AddComponentParameter(1, nameof(TbSelect<string>.Label), "Size");
            builder.AddComponentParameter(2, nameof(TbSelect<string>.ValueChanged), Chosen);
            builder.AddComponentParameter(3, nameof(TbSelect<string>.ChildContent), (RenderFragment)(options =>
            {
                options.OpenComponent<TbOption<string>>(0);
                options.AddComponentParameter(1, nameof(TbOption<string>.Value), "Large");
                options.AddComponentParameter(2, nameof(TbOption<string>.ChildContent),
                    (RenderFragment)(text => text.AddContent(0, "Large")));
                options.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
