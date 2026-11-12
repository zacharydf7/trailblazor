using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Components;

public class TbChipTests : BunitContext
{
    public TbChipTests()
    {
        this.AddTrailblazor();
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
    public void Render_ShouldCallNoScript_WhenTheChipCanBeRemoved()
    {
        Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Invoices")
            .Add(chip => chip.OnRemove, () => { }));

        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void Render_ShouldGiveTheIconItsOwnTone_WhenIconToneIsSet()
    {
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "New alerts")
            .Add(chip => chip.Icon, ExampleIcon).Add(chip => chip.IconTone, Tone.Danger));

        Assert.Contains("tb-icon--danger", cut.Find("[data-part=icon]").ClassList);
        Assert.Contains("tb-chip--neutral", cut.Find(".tb-chip").ClassList);
    }

    [Fact]
    public void Render_ShouldIgnoreIconTone_WhenTheChipIsFilled()
    {
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "New alerts")
            .Add(chip => chip.Icon, ExampleIcon).Add(chip => chip.IconTone, Tone.Danger)
            .Add(chip => chip.Variant, Variant.Filled));

        Assert.Equal(["tb-icon", "tb-icon--small"], cut.Find("[data-part=icon]").ClassList);
    }

    [Fact]
    public void Render_ShouldLetTheIconTakeTheChipsColor_WhenIconToneIsNotSet()
    {
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Field notes")
            .Add(chip => chip.Icon, ExampleIcon).Add(chip => chip.Tone, Tone.Secondary));

        Assert.Equal(["tb-icon", "tb-icon--small"], cut.Find("[data-part=icon]").ClassList);
        Assert.Contains("tb-chip--secondary", cut.Find(".tb-chip").ClassList);
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
        cut.Find("button").Click();

        Assert.False(selected);
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.DoesNotContain("tb-chip--selected", cut.Find(".tb-chip").ClassList);
        Assert.Single(cut.FindAll("[data-part=icon]"));
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
    public async Task Click_ShouldAskOnce_WhenTheRemoveButtonIsClickedAgainWhileOnRemoveRuns()
    {
        var removals = 0;
        var held = new TaskCompletionSource();
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.OnRemove, () =>
            {
                removals++;
                return held.Task;
            }));

        cut.Find("[data-part=remove]").Click();
        cut.Find("[data-part=remove]").Click();
        Assert.Equal(1, removals);
        // Once the first removal ends, the chip can be asked again.
        await cut.InvokeAsync(held.SetResult);
        cut.Find("[data-part=remove]").Click();

        Assert.Equal(2, removals);
    }

    [Fact]
    public void Click_ShouldAskToBeRemovedWithANamedButton_WhenOnRemoveIsSet()
    {
        var removals = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.OnRemove, () => removals++));

        cut.Find("[data-part=remove]").Click();

        Assert.Equal("Remove Archive", cut.Find("[data-part=remove]").GetAttribute("aria-label"));
        Assert.Equal(1, removals);
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("Backspace")]
    public void KeyDown_ShouldAskToBeRemoved_WhenGivenARemovalKey(string key)
    {
        var removals = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.OnClick, () => { }).Add(chip => chip.OnRemove, () => removals++));

        cut.Find("[data-part=action]").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal(1, removals);
    }

    [Fact]
    public void KeyDown_ShouldAskOnce_WhenARemovalKeyIsHeld()
    {
        var removals = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.OnRemove, () => removals++));

        cut.Find("[data-part=remove]").KeyDown(new KeyboardEventArgs { Key = "Delete" });
        cut.Find("[data-part=remove]").KeyDown(new KeyboardEventArgs { Key = "Delete", Repeat = true });

        Assert.Equal(1, removals);
    }

    [Fact]
    public void KeyDown_ShouldDoNothing_WhenTheChipIsDisabled()
    {
        var removals = 0;
        var cut = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Archive")
            .Add(chip => chip.Disabled, true).Add(chip => chip.OnRemove, () => removals++));

        cut.Find("[data-part=remove]").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.True(cut.Find("[data-part=remove]").HasAttribute("disabled"));
        Assert.Equal(0, removals);
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
