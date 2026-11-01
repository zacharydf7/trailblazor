using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbButtonGroupTests : BunitContext
{
    public TbButtonGroupTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldGroupTheButtons_WhenCalled()
    {
        var cut = Render<TbButtonGroup>(parameters => parameters
            .AddUnmatched("aria-label", "Text alignment")
            .AddChildContent<TbButton>(button => button.AddChildContent("Left")));

        var group = cut.Find("[role=group]");
        Assert.Equal("Text alignment", group.GetAttribute("aria-label"));
        Assert.NotNull(group.QuerySelector("button"));
    }

    [Fact]
    public void Render_ShouldGiveEveryButtonTheGroupsSettings_WhenTheButtonDoesntSetItsOwn()
    {
        var cut = RenderGroup(builder =>
        {
            builder.OpenComponent<TbButton>(0);
            builder.CloseComponent();
            builder.OpenComponent<TbButton>(1);
            builder.AddComponentParameter(2, nameof(TbButton.Variant), Variant.Outlined);
            builder.AddComponentParameter(3, nameof(TbButton.Tone), Tone.Success);
            builder.CloseComponent();
        });

        var buttons = cut.FindAll("button");
        Assert.Equal(["tb-button--soft", "tb-button--danger", "tb-button--small"],
            Modifiers(buttons[0].ClassList));
        Assert.Equal(["tb-button--outlined", "tb-button--success", "tb-button--small"],
            Modifiers(buttons[1].ClassList));
    }

    [Fact]
    public void Render_ShouldDrawAMenusTriggerWithTheGroupsSettings_WhenTheMenuDoesntSetItsOwn()
    {
        var cut = RenderGroup(builder =>
        {
            builder.OpenComponent<TbMenu>(0);
            builder.AddComponentParameter(1, nameof(TbMenu.Label), "More");
            builder.CloseComponent();
            builder.OpenComponent<TbMenu>(2);
            builder.AddComponentParameter(3, nameof(TbMenu.Label), "Other");
            builder.AddComponentParameter(4, nameof(TbMenu.Variant), Variant.Text);
            builder.CloseComponent();
        });

        var triggers = cut.FindAll("[data-part=trigger]");
        Assert.Equal(["tb-button--soft", "tb-button--danger", "tb-button--small"],
            Modifiers(triggers[0].ClassList));
        Assert.Equal(["tb-button--text", "tb-button--danger", "tb-button--small"],
            Modifiers(triggers[1].ClassList));
    }

    [Fact]
    public void Render_ShouldDrawAPopoversTriggerButNotItsPanelWithTheGroupsSettings_WhenTheyDontSetTheirOwn()
    {
        var cut = RenderGroup(builder =>
        {
            builder.OpenComponent<TbPopover>(0);
            builder.AddComponentParameter(1, nameof(TbPopover.Label), "Share");
            builder.AddComponentParameter(2, nameof(TbPopover.ChildContent), (RenderFragment)(panel =>
            {
                panel.OpenComponent<TbButton>(0);
                panel.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.Equal(["tb-button--soft", "tb-button--danger", "tb-button--small"],
            Modifiers(cut.Find("[data-part=trigger]").ClassList));
        Assert.Equal(["tb-button--filled", "tb-button--primary", "tb-button--medium"],
            Modifiers(cut.Find("[data-part=panel] button").ClassList));
    }

    [Fact]
    public void Render_ShouldDisableEveryButton_WhenTheGroupIsDisabled()
    {
        var cut = Render<TbButtonGroup>(parameters => parameters.Add(group => group.Disabled, true)
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbButton>(0);
                builder.CloseComponent();
                builder.OpenComponent<TbIconButton>(1);
                builder.AddComponentParameter(2, nameof(TbIconButton.Icon), new Icon("<path d=\"M4 4h16v16H4z\" />"));
                builder.AddComponentParameter(3, nameof(TbIconButton.AriaLabel), "Add");
                builder.CloseComponent();
            })));

        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Render_ShouldMarkTheGroupVertical_WhenVertical(bool vertical, bool marked)
    {
        var cut = Render<TbButtonGroup>(parameters => parameters.Add(group => group.Vertical, vertical));

        Assert.Equal(marked, cut.Find("[role=group]").ClassList.Contains("tb-button-group--vertical"));
    }

    private IRenderedComponent<TbButtonGroup> RenderGroup(RenderFragment buttons) =>
        Render<TbButtonGroup>(parameters => parameters.Add(group => group.Variant, Variant.Soft)
            .Add(group => group.Tone, Tone.Danger).Add(group => group.Size, Size.Small)
            .Add(group => group.ChildContent, buttons));

    private static string[] Modifiers(IEnumerable<string> classes) =>
        classes.Where(name => name.StartsWith("tb-button--", StringComparison.Ordinal)).ToArray();
}
