using Bunit;

namespace Trailblazor.Tests.Components;

public class TbMenuCheckboxItemTests : BunitContext
{
    public TbMenuCheckboxItemTests() => this.AddTrailblazor();

    [Theory]
    [InlineData(false, "true")]
    [InlineData(true, "false")]
    public void Click_ShouldToggleAndReport_WhenTheItemIsEnabled(bool isChecked, string after)
    {
        bool? reported = null;
        var cut = Render<TbMenuCheckboxItem>(parameters => parameters.Add(item => item.Checked, isChecked)
            .Add(item => item.CheckedChanged, (bool value) => reported = value).AddChildContent("Map"));

        cut.Find("button").Click();

        Assert.Equal(!isChecked, reported);
        Assert.Equal(after, cut.Find("button").GetAttribute("aria-checked"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMarkTheItemToKeepTheMenuOpen_WhenKeepOpenIsSet(bool keepOpen)
    {
        var cut = Render<TbMenuCheckboxItem>(parameters => parameters.Add(item => item.KeepOpen, keepOpen)
            .AddChildContent("Map"));

        Assert.Equal(keepOpen, cut.Find("button").HasAttribute("data-keep-open"));
    }

    [Fact]
    public void Render_ShouldBeACheckboxItemWithAMark_WhenChecked()
    {
        var cut = Render<TbMenuCheckboxItem>(parameters => parameters.Add(item => item.Checked, true)
            .AddChildContent("Map"));

        var item = cut.Find("button");
        Assert.Equal(("menuitemcheckbox", "true", "-1"), (item.GetAttribute("role"), item.GetAttribute("aria-checked"),
            item.GetAttribute("tabindex")));
        Assert.NotNull(item.QuerySelector("[data-part=indicator] svg"));
    }

    [Fact]
    public void Click_ShouldDoNothing_WhenTheItemIsDisabled()
    {
        var reported = false;
        var cut = Render<TbMenuCheckboxItem>(parameters => parameters.Add(item => item.Disabled, true)
            .Add(item => item.CheckedChanged, (bool _) => reported = true).AddChildContent("Map"));

        cut.Find("button").Click();

        Assert.False(reported);
        Assert.Equal(("false", "true"), (cut.Find("button").GetAttribute("aria-checked"),
            cut.Find("button").GetAttribute("aria-disabled")));
    }
}
