using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Components;

public class TbChipTests : BunitContext
{
    public TbChipTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Render_ShouldBePlainTextInASoftNeutralPill_WhenItHasNoActions()
    {
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Invoices"));

        Assert.Equal(["tb-chip", "tb-chip--soft", "tb-chip--neutral", "tb-chip--medium"],
            cut.Find(".tb-chip").ClassList);
        Assert.Equal("SPAN", cut.Find("[data-part=action]").TagName);
        Assert.Equal("Invoices", cut.Find("[data-part=label]").TextContent.Trim());
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Click_ShouldToggleAndShowACheck_WhenBoundToSelected()
    {
        bool? selected = null;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Running")
            .Add(chip => chip.Icon, ExampleIcon)
            .Add(chip => chip.SelectedChanged, (bool value) => selected = value));
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("[data-part=icon]"));

        cut.Find("button").Click();

        Assert.True(selected);
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Contains("tb-chip--selected", cut.Find(".tb-chip").ClassList);
        Assert.Single(cut.FindAll("[data-part=check]"));
        Assert.Empty(cut.FindAll("[data-part=icon]"));
    }

    [Fact]
    public void Click_ShouldRunOnClickFromAPlainButton_WhenOnClickIsSet()
    {
        var clicks = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Open")
            .Add(chip => chip.OnClick, () => clicks++));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
        Assert.Null(cut.Find("button").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Render_ShouldLinkToTheAddressUnlessDisabled_WhenHrefIsSet()
    {
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Guide")
            .Add(chip => chip.Href, "guides/theming"));
        Assert.Equal("guides/theming", cut.Find("a").GetAttribute("href"));

        cut.Render(parameters => parameters.Add(chip => chip.Disabled, true));

        Assert.Empty(cut.FindAll("a"));
        Assert.Equal("true", cut.Find("[data-part=action]").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Click_ShouldAskToBeRemovedWithANamedButton_WhenOnCloseIsSet()
    {
        var closes = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.OnClose, () => closes++));

        cut.Find("[data-part=remove]").Click();

        Assert.Equal("Remove Archive", cut.Find("[data-part=remove]").GetAttribute("aria-label"));
        Assert.Equal(1, closes);
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("Backspace")]
    public void KeyDown_ShouldAskToBeRemoved_WhenGivenARemovalKey(string key)
    {
        var closes = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.OnClick, () => { }).Add(chip => chip.OnClose, () => closes++));

        cut.Find("[data-part=action]").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal(1, closes);
    }

    [Fact]
    public void KeyDown_ShouldDoNothing_WhenTheChipIsDisabled()
    {
        var closes = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.Disabled, true).Add(chip => chip.OnClose, () => closes++));

        cut.Find("[data-part=remove]").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.True(cut.Find("[data-part=remove]").HasAttribute("disabled"));
        Assert.Equal(0, closes);
    }

    [Fact]
    public void Render_ShouldBeAListOfItems_WhenChipsAreInAGroup()
    {
        var cut = Render<TbChipGroup>(parameters => parameters.Add(group => group.AriaLabel, "Selected tags")
            .AddChildContent<TbChip>(chip => chip.Add(c => c.Text, "Invoices")));

        var list = cut.Find("[role=list]");
        Assert.Equal("Selected tags", list.GetAttribute("aria-label"));
        Assert.Equal("-1", list.GetAttribute("tabindex"));
        Assert.Equal("listitem", cut.Find(".tb-chip").GetAttribute("role"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAGroupHasNoAriaLabel()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbChipGroup>());
    }

    private static readonly Icon ExampleIcon = new("<path d=\"M3 3h18v18H3z\" />");
}
