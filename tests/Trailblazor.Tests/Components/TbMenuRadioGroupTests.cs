using Bunit;

namespace Trailblazor.Tests.Components;

public class TbMenuRadioGroupTests : BunitContext
{
    private string? _units = "Miles";

    public TbMenuRadioGroupTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldCheckTheItemWithTheGroupsValue_WhenCalled()
    {
        var cut = RenderUnits();

        var group = cut.Find("[role=group]");
        Assert.Equal(cut.Find("[data-part=group-label]").Id, group.GetAttribute("aria-labelledby"));
        Assert.Equal(["true", "false"], cut.FindAll("[role=menuitemradio]").Select(item =>
            item.GetAttribute("aria-checked")));
    }

    [Fact]
    public void Click_ShouldMoveTheChoiceAndReportIt_WhenAnotherItemIsChosen()
    {
        var cut = RenderUnits();

        cut.FindAll("[role=menuitemradio]")[1].Click();

        Assert.Equal("Kilometers", _units);
        Assert.Equal(["false", "true"], cut.FindAll("[role=menuitemradio]").Select(item =>
            item.GetAttribute("aria-checked")));
    }

    [Fact]
    public void Click_ShouldDoNothing_WhenTheItemIsDisabled()
    {
        var cut = RenderUnits(kilometersDisabled: true);

        cut.FindAll("[role=menuitemradio]")[1].Click();

        Assert.Equal("Miles", _units);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnItemIsOutsideAGroup()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbMenuRadioItem<string>>(parameters =>
            parameters.Add(item => item.Value, "Miles")));

        Assert.Equal("TbMenuRadioItem must be inside a TbMenuRadioGroup its value fits.", failure.Message);
    }

    // Razor types an item by its literal, so Value="3" in a group of int? is a TbMenuRadioItem<int>.
    [Fact]
    public async Task Click_ShouldChooseTheItemsValue_WhenAnItemOfTheUnderlyingTypeIsInANullableGroup()
    {
        int? stops = 1;
        var cut = Render<TbMenuRadioGroup<int?>>(parameters => parameters.Add(group => group.Label, "Stops")
            .Add(group => group.Value, stops).Add(group => group.ValueChanged, (int? value) => stops = value)
            .AddChildContent<TbMenuRadioItem<int>>(item => item.Add(radio => radio.Value, 1).AddChildContent("One"))
            .AddChildContent<TbMenuRadioItem<int>>(item => item.Add(radio => radio.Value, 3).AddChildContent("Three")));

        Assert.Equal("true", cut.FindAll("[role=menuitemradio]")[0].GetAttribute("aria-checked"));
        await cut.FindAll("[role=menuitemradio]")[1].ClickAsync(new());

        Assert.Equal(3, stops);
    }

    private IRenderedComponent<TbMenuRadioGroup<string>> RenderUnits(bool kilometersDisabled = false) =>
        Render<TbMenuRadioGroup<string>>(parameters => parameters.Add(group => group.Label, "Units")
            .Add(group => group.Value, _units).Add(group => group.ValueChanged, (string? value) => _units = value)
            .AddChildContent<TbMenuRadioItem<string>>(item => item.Add(radio => radio.Value, "Miles")
                .AddChildContent("Miles"))
            .AddChildContent<TbMenuRadioItem<string>>(item => item.Add(radio => radio.Value, "Kilometers")
                .Add(radio => radio.Disabled, kilometersDisabled).AddChildContent("Kilometers")));
}
