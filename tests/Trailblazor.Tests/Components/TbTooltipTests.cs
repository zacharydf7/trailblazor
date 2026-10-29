using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbTooltipTests : BunitContext
{
    private const string Module = "./_content/Trailblazor/tooltip.js";

    public TbTooltipTests() => Services.AddTrailblazor();

    [Fact]
    public void Render_ShouldWrapTheTriggerAndHoldAClosedTooltip_WhenCalled()
    {
        JSInterop.SetupModule(Module).SetupModule("createTooltip", _ => true);

        var cut = Render<TbTooltip>(parameters => parameters
            .Add(tooltip => tooltip.Text, "Delete the selection")
            .AddChildContent("<button>Delete</button>"));

        var tooltip = cut.Find("[role=tooltip]");
        Assert.Equal("manual", tooltip.GetAttribute("popover"));
        Assert.Equal("Delete the selection", tooltip.TextContent);
        Assert.NotNull(cut.Find(".tb-tooltip-anchor > button"));
    }

    [Theory]
    [InlineData(TooltipPlacement.Top, "Top")]
    [InlineData(TooltipPlacement.Bottom, "Bottom")]
    [InlineData(TooltipPlacement.Start, "Start")]
    [InlineData(TooltipPlacement.End, "End")]
    public void Render_ShouldSetUpTheTooltipOnTheRequestedSide_WhenGivenAPlacement(TooltipPlacement placement,
        string side)
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupModule("createTooltip", _ => true);

        Render<TbTooltip>(parameters => parameters
            .Add(tooltip => tooltip.Text, "Delete")
            .Add(tooltip => tooltip.Placement, placement));

        var options = Assert.IsType<AnchoredPositionOptions>(module.VerifyInvoke("createTooltip").Arguments[2]);
        Assert.Equal(side, options.Side.ToString());
    }

    [Fact]
    public async Task DisposeAsync_ShouldTearDownTheTooltip_WhenTheComponentGoes()
    {
        var handle = JSInterop.SetupModule(Module).SetupModule("createTooltip", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete"));

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }
}
