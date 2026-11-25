using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A button or menu item that's off can say why.
public class DisabledReasonTests : BunitContext
{
    private const string Reason = "Name the trip first.";
    private int _clicks;

    public DisabledReasonTests() => this.AddTrailblazor();

    private IRenderedComponent<TbButton> RenderPublish(bool disabled) => Render<TbButton>(parameters => parameters
        .Add(button => button.Disabled, disabled).Add(button => button.DisabledReason, Reason)
        .Add(button => button.TelemetryName, "publish").Add(button => button.OnClick, () => _clicks++)
        .AddChildContent("Publish"));

    private static string? Description(IRenderedComponent<IComponent> cut, string selector) =>
        cut.Find(selector).GetAttribute("aria-describedby") is { } ids
            ? string.Join(" ", ids.Split(' ').Select(id => cut.Find($"#{id}").TextContent.Trim()))
            : null;

    [Fact]
    public void Render_ShouldDescribeTheButtonWithItsReason_WhenDisabled()
    {
        var cut = RenderPublish(disabled: true);

        Assert.Equal(Reason, Description(cut, "button"));
        // The hidden reason stays out of the button's name.
        Assert.Equal("Publish", Choices.NameOf(cut.Find("button")));
        // The tooltip shows the reason to sighted people; screen readers have it as the description already.
        var tooltip = cut.Find("[role=tooltip]");
        Assert.Equal((Reason, "true"), (tooltip.TextContent.Trim(), tooltip.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Render_ShouldIgnoreTheReason_WhenEnabled()
    {
        var cut = RenderPublish(disabled: false);

        Assert.Null(cut.Find("button").GetAttribute("aria-describedby"));
        Assert.Empty(cut.FindAll("[role=tooltip]"));
    }

    [Fact]
    public void Click_ShouldStayFocusableAndCountThePress_WhenDisabledWithAReason()
    {
        var cut = RenderPublish(disabled: true);
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");

        cut.Find("button").Click();

        var button = cut.Find("button");
        Assert.Equal((false, "true", 0),
            (button.HasAttribute("disabled"), button.GetAttribute("aria-disabled"), _clicks));
        Assert.Equal("pressed_off", Assert.Single(used.GetMeasurementSnapshot()).Tags["trailblazor.action"]);
    }

    [Fact]
    public void Render_ShouldShowTheNameAndTheReasonInOneTooltip_WhenAnIconButtonIsDisabledWithAReason()
    {
        var cut = Render<TbIconButton>(parameters => parameters.Add(button => button.Icon, BuiltInIcons.Minus)
            .Add(button => button.AriaLabel, "Delete trip").Add(button => button.ShowTooltip, true)
            .Add(button => button.Disabled, true).Add(button => button.DisabledReason, Reason));

        var tooltip = Assert.Single(cut.FindAll("[role=tooltip]"));
        Assert.Equal(["Delete trip", Reason],
            tooltip.QuerySelectorAll(".tb-tooltip__line").Select(line => line.TextContent));
        // The tooltip's name line is already the button's name, so the description is the reason alone.
        Assert.Equal(Reason, Description(cut, "button"));
    }

    [Fact]
    public void Render_ShouldDescribeTheFloatingButtonWithItsReason_WhenDisabled()
    {
        var cut = Render<TbFab>(parameters => parameters.Add(fab => fab.Icon, BuiltInIcons.Minus)
            .Add(fab => fab.Label, "Add a stop").Add(fab => fab.Disabled, true).Add(fab => fab.DisabledReason, Reason));

        var button = cut.Find("button");
        Assert.Equal((Reason, "true", false),
            (Description(cut, "button"), button.GetAttribute("aria-disabled"), button.HasAttribute("disabled")));
        Assert.Equal(Reason, cut.Find("[role=tooltip]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldShowTheReasonAsTheItemsDescription_WhenAMenuItemIsDisabled()
    {
        var cut = Render<TbMenuItem>(parameters => parameters.Add(item => item.Description, "Share it with the group")
            .Add(item => item.Disabled, true).Add(item => item.DisabledReason, Reason).AddChildContent("Publish"));

        Assert.Equal(Reason, cut.Find("[data-part=description]").TextContent);
        Assert.Equal(Reason, Description(cut, "button"));
    }

    [Fact]
    public void Render_ShouldKeepTheItemsOwnDescription_WhenAMenuItemIsEnabled()
    {
        var cut = Render<TbMenuItem>(parameters => parameters.Add(item => item.Description, "Share it with the group")
            .Add(item => item.DisabledReason, Reason).AddChildContent("Publish"));

        Assert.Equal("Share it with the group", cut.Find("[data-part=description]").TextContent);
    }

    [Fact]
    public void Render_ShouldShowOneTooltipWithTheNameAndTheReason_WhenAToolbarIconButtonIsDisabledWithAReason()
    {
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Trip")
            .AddChildContent<TbToolbarButton>(button => button.Add(item => item.Icon, BuiltInIcons.Minus)
                .Add(item => item.AriaLabel, "Delete trip").Add(item => item.Disabled, true)
                .Add(item => item.DisabledReason, Reason)));

        var tooltip = Assert.Single(cut.FindAll("[role=tooltip]"));
        Assert.Equal(["Delete trip", Reason],
            tooltip.QuerySelectorAll(".tb-tooltip__line").Select(line => line.TextContent));
        Assert.Equal("true", tooltip.GetAttribute("aria-hidden"));
        Assert.Equal(Reason, Description(cut, "[aria-label='Delete trip']"));
    }

    [Fact]
    public async Task Overflow_ShouldShowTheReasonAsTheItemsDescription_WhenADisabledToolbarButtonMovesIntoTheMenu()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toolbar.js");
        module.SetupModule("attachToolbar", _ => true);
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Trip")
            .AddChildContent<TbToolbarButton>(button => button.Add(item => item.Label, "Publish")
                .Add(item => item.Disabled, true).Add(item => item.DisabledReason, Reason)));
        var overflow =
            (DotNetObjectReference<ToolbarOverflow>)module.Invocations["attachToolbar"].Single().Arguments[2]!;

        await cut.InvokeAsync(() => overflow.Value.OnOverflow([cut.Find("button").GetAttribute("data-toolbar-item")!]));

        Assert.Equal(Reason, cut.Find("[role=menuitem] [data-part=description]").TextContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_ShouldDescribeTheRibbonButtonWithItsReason_WhenDisabled(bool iconOnly)
    {
        var cut = Render<TbRibbonButton>(parameters => parameters.Add(button => button.Label, "Publish")
            .Add(button => button.Icon, BuiltInIcons.Minus).Add(button => button.IconOnly, iconOnly)
            .Add(button => button.Disabled, true).Add(button => button.DisabledReason, Reason));

        Assert.Equal(Reason, Description(cut, "button"));
    }
}
