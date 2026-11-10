using Bunit;

namespace Trailblazor.Tests.Components;

public class TbInsertDividerTests : BunitContext
{
    [Fact]
    public void Render_ShouldNameTheInsertButton_WhenGivenALabel()
    {
        var cut = Render<TbInsertDivider>(parameters =>
            parameters.Add(divider => divider.Label, "Add a step after Lunch"));

        Assert.Equal("Add a step after Lunch", cut.Find("[data-part=insert]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Click_ShouldAskToInsert_WhenTheButtonIsChosen()
    {
        var inserts = 0;
        var cut = Render<TbInsertDivider>(parameters => parameters.Add(divider => divider.Label, "Add a step")
            .Add(divider => divider.OnInsert, () => inserts++));

        cut.Find("[data-part=insert]").Click();

        Assert.Equal(1, inserts);
    }

    [Fact]
    public void Render_ShouldTurnTheButtonOff_WhenDisabled()
    {
        var cut = Render<TbInsertDivider>(parameters => parameters.Add(divider => divider.Label, "Add a step")
            .Add(divider => divider.Disabled, true));

        Assert.True(cut.Find("[data-part=insert]").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldMarkTheDividerToShowAtRest_WhenAlwaysVisible()
    {
        var cut = Render<TbInsertDivider>(parameters => parameters.Add(divider => divider.Label, "Add a step")
            .Add(divider => divider.AlwaysVisible, true));

        Assert.Contains("tb-insert-divider--visible", cut.Find(".tb-insert-divider").ClassList);
    }
}
