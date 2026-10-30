using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbToggleGroupTests : BunitContext
{
    private static readonly Icon Square = new("<path d=\"M4 4h16v16H4z\" />");

    private string? View { get; set; }

    private IReadOnlyList<string> Layers { get; set; } = [];

    public TbToggleGroupTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Render_ShouldBeANamedGroupOfToggleButtons_WhenCalled()
    {
        var cut = RenderGroup("Satellite");

        var group = cut.Find("[role=group]");
        Assert.Equal("Map view", group.GetAttribute("aria-label"));
        var items = cut.FindAll("[data-part=item]");
        Assert.Equal(["false", "true", "false"], items.Select(item => item.GetAttribute("aria-pressed")));
        Assert.True(items[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Click_ShouldPressOnlyTheClickedItem_WhenAnotherItemIsClicked()
    {
        var cut = RenderGroup("Trails");

        cut.FindAll("[data-part=item]")[1].Click();

        Assert.Equal("Satellite", View);
        Assert.Equal(["false", "true", "false"],
            cut.FindAll("[data-part=item]").Select(item => item.GetAttribute("aria-pressed")));
    }

    [Fact]
    public void Click_ShouldKeepTheItemPressedWithoutRaisingValueChanged_WhenThePressedItemIsClickedAgain()
    {
        var cut = RenderGroup("Trails");
        View = "unchanged";

        cut.FindAll("[data-part=item]")[0].Click();

        Assert.Equal("unchanged", View);
        Assert.Equal("true", cut.FindAll("[data-part=item]")[0].GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Click_ShouldClearTheGroup_WhenThePressedItemIsClickedInAClearableGroup()
    {
        var cut = RenderGroup("Trails", clearable: true);

        cut.FindAll("[data-part=item]")[0].Click();

        Assert.Null(View);
        Assert.Equal("false", cut.FindAll("[data-part=item]")[0].GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Render_ShouldTurnEveryItemOff_WhenTheGroupIsDisabled()
    {
        var cut = RenderGroup("Trails", disabled: true);

        Assert.All(cut.FindAll("[data-part=item]"), item => Assert.True(item.HasAttribute("disabled")));
    }

    [Fact]
    public void Click_ShouldAddAndRemoveItems_WhenAMultipleGroupsItemsAreClicked()
    {
        Layers = ["Water"];
        var cut = Render<TbMultiToggleGroup<string>>(parameters => parameters
            .Add(group => group.AriaLabel, "Show on map")
            .Add(group => group.Value, Layers)
            .Add(group => group.ValueChanged, (IReadOnlyList<string> next) => Layers = next)
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                AddItem(builder, 0, "Campsites");
                AddItem(builder, 10, "Water");
            })));

        cut.FindAll("[data-part=item]")[0].Click();
        cut.FindAll("[data-part=item]")[1].Click();

        Assert.Equal(["Campsites"], Layers);
    }

    [Fact]
    public void Render_ShouldNameAnIconItemByItsAriaLabelAndPassItsAttributesOn_WhenGivenThem()
    {
        var cut = Render<TbToggleGroup<string>>(parameters => parameters
            .Add(group => group.AriaLabel, "Map view")
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbToggleItem<string>>(0);
                builder.AddAttribute(1, nameof(TbToggleItem<string>.Value), "Alerts");
                builder.AddAttribute(2, nameof(TbToggleItem<string>.Icon), (Icon?)Square);
                builder.AddAttribute(3, nameof(TbToggleItem<string>.AriaLabel), "Alerts");
                builder.AddAttribute(4, nameof(TbComponentBase.Class), "my-item");
                builder.AddAttribute(5, "data-testid", "alerts");
                builder.CloseComponent();
            })));

        var item = cut.Find("[data-part=item]");
        Assert.Equal("Alerts", item.GetAttribute("aria-label"));
        Assert.Contains("my-item", item.ClassList);
        Assert.Equal("alerts", item.GetAttribute("data-testid"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAClearableGroupsValueTypeIsNotNullable()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbToggleGroup<DayOfWeek>>(parameters => parameters
            .Add(group => group.AriaLabel, "Day").Add(group => group.Clearable, true)));
    }

    [Fact]
    public void Click_ShouldClearToNull_WhenAClearableGroupHasANullableValueType()
    {
        DayOfWeek? day = DayOfWeek.Monday;
        var cut = Render<TbToggleGroup<DayOfWeek?>>(parameters => parameters
            .Add(group => group.AriaLabel, "Day").Add(group => group.Clearable, true).Add(group => group.Value, day)
            .Add(group => group.ValueChanged, (DayOfWeek? next) => day = next)
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbToggleItem<DayOfWeek>>(0);
                builder.AddAttribute(1, nameof(TbToggleItem<DayOfWeek>.Value), DayOfWeek.Monday);
                builder.AddAttribute(2, nameof(TbToggleItem<DayOfWeek>.ChildContent),
                    (RenderFragment)(content => content.AddContent(0, "Monday")));
                builder.CloseComponent();
            })));

        cut.Find("[data-part=item]").Click();

        Assert.Null(day);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnItemIsOutsideAGroup()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbToggleItem<string>>(parameters => parameters
            .Add(item => item.Value, "Trails")));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnItemsValueIsNotTheGroupsType()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbToggleGroup<string>>(parameters => parameters
            .Add(group => group.AriaLabel, "Map view")
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbToggleItem<object>>(0);
                builder.AddAttribute(1, nameof(TbToggleItem<object>.Value), (object)3);
                builder.CloseComponent();
            }))));
    }

    private IRenderedComponent<TbToggleGroup<string?>> RenderGroup(string? value, bool clearable = false,
        bool disabled = false)
    {
        View = value;
        return Render<TbToggleGroup<string?>>(parameters => parameters
            .Add(group => group.AriaLabel, "Map view")
            .Add(group => group.Clearable, clearable)
            .Add(group => group.Disabled, disabled)
            .Add(group => group.Value, value)
            .Add(group => group.ValueChanged, (string? next) => View = next)
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                AddItem(builder, 0, "Trails");
                AddItem(builder, 10, "Satellite");
                AddItem(builder, 20, "Terrain", disabled: true);
            })));
    }

    private static void AddItem(RenderTreeBuilder builder, int sequence, string value, bool disabled = false)
    {
        builder.OpenComponent<TbToggleItem<string>>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbToggleItem<string>.Value), value);
        builder.AddAttribute(sequence + 2, nameof(TbToggleItem<string>.Disabled), disabled);
        builder.AddAttribute(sequence + 3, nameof(TbToggleItem<string>.ChildContent),
            (RenderFragment)(content => content.AddContent(0, value)));
        builder.CloseComponent();
    }
}
