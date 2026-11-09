using Bunit;

namespace Trailblazor.Tests.Components;

public class FieldInfoTests : BunitContext
{
    public FieldInfoTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void TextField_ShouldShowAnInfoButtonBesideItsLabel_WhenGivenInfo()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Info, "The name hikers see."));

        var info = cut.Find(".tb-field__label-row > [data-part=info]");
        Assert.Equal("Information about Trail", info.QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Equal("Trail", cut.Find("label").TextContent.Trim());
        Assert.Empty(cut.FindAll("label [data-part=info]"));
    }

    [Fact]
    public void Checkbox_ShouldShowTheInfoButtonAfterItsLabel_WhenGivenInfoContent()
    {
        var cut = Render<TbCheckbox<bool>>(parameters => parameters.Add(field => field.Label, "Guided")
            .Add(field => field.InfoContent, "A ranger leads the hike.")
            .Add(field => field.InfoButtonLabel, "About {0}"));

        Assert.Equal("About Guided",
            cut.Find(".tb-checkbox > .tb-field__label-row > [data-part=info] button").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("label [data-part=info]"));
    }

    [Fact]
    public void RadioGroup_ShouldBeNamedByItsLabelAlone_WhenGivenInfo()
    {
        var cut = Render<TbRadioGroup<string>>(parameters => parameters.Add(group => group.Label, "Difficulty")
            .Add(group => group.Info, "Hard trails climb more than 500 meters."));

        var name = cut.Find($"#{cut.Find("fieldset").GetAttribute("aria-labelledby")}");
        Assert.Equal("Difficulty", name.TextContent);
        Assert.NotNull(cut.Find("legend [data-part=info]"));
    }

    [Fact]
    public void Slider_ShouldShowAnInfoButtonBesideItsLabel_WhenGivenInfo()
    {
        var cut = Render<TbSlider<int>>(parameters => parameters.Add(field => field.Label, "Distance")
            .Add(field => field.Info, "In kilometers."));

        Assert.NotNull(cut.Find(".tb-field__label-row > [data-part=info]"));
        Assert.Equal("Distance", cut.Find("label").TextContent.Trim());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void FileUpload_ShouldShowAnInfoButtonUnlessHidden_WhenGivenInfo(bool hidden, int buttons)
    {
        var cut = Render<TbFileUpload>(parameters => parameters.Add(field => field.Label, "Trail map")
            .Add(field => field.Info, "A PDF or an image.").Add(field => field.Hidden, hidden));

        Assert.Equal(buttons, cut.FindAll("[data-part=info]").Count);
        Assert.Empty(cut.FindAll("label [data-part=info]"));
    }

    [Fact]
    public void Rating_ShouldBeNamedByItsLabelAlone_WhenGivenInfo()
    {
        var cut = Render<TbRating>(parameters => parameters.Add(field => field.Label, "Scenery")
            .Add(field => field.Info, "How much there is to see."));

        Assert.Equal("Scenery", cut.Find($"#{cut.Find("fieldset").GetAttribute("aria-labelledby")}").TextContent);
        Assert.NotNull(cut.Find("legend [data-part=info]"));
    }

    [Fact]
    public void CheckboxGroup_ShouldBeNamedByItsLabelAlone_WhenGivenInfo()
    {
        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(field => field.Label, "Amenities")
            .Add(field => field.Info, "Facilities at the trailhead."));

        Assert.Equal("Amenities", cut.Find($"#{cut.Find("fieldset").GetAttribute("aria-labelledby")}").TextContent);
        Assert.NotNull(cut.Find("legend [data-part=info]"));
    }

    [Fact]
    public void RadioGroup_ShouldKeepItsOwnLabelledBy_WhenGivenOneWithoutInfo()
    {
        var cut = Render<TbRadioGroup<string>>(parameters => parameters.Add(group => group.AriaLabel, "Difficulty")
            .AddUnmatched("aria-labelledby", "difficulty-heading"));

        Assert.Equal("difficulty-heading", cut.Find("fieldset").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAFieldHasInfoButNoLabel()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbTextField<string>>(parameters =>
            parameters.Add(field => field.AriaLabel, "Trail").Add(field => field.Info, "The name hikers see.")));

        Assert.Equal("A field needs a Label to show an info button.", error.Message);
    }
}
