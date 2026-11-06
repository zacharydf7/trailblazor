using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class LabelExampleTests : DocsTest
{
    [Fact]
    public void LabelExample_ShouldTieTheLabelToItsField_WhenRendered()
    {
        var cut = Render<LabelExample>();

        Assert.Equal("project-code", cut.Find("label").GetAttribute("for"));
        Assert.Equal("project-code", cut.Find("input").Id);
    }

    [Fact]
    public void GroupExample_ShouldNameTheGroupByTheLabelsTextAlone_WhenRendered()
    {
        var cut = Render<LabelGroupExample>();

        Assert.Equal("delivery-label", cut.Find("fieldset").GetAttribute("aria-labelledby"));
        Assert.Equal("Delivery speed", cut.Find("#delivery-label").TextContent.Trim());
    }

    [Fact]
    public void RequiredExample_ShouldMarkTheLabelAndTheField_WhenRendered()
    {
        var cut = Render<LabelRequiredExample>();

        Assert.Contains("*", cut.Find("label").TextContent);
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-required"));
    }

    [Fact]
    public void ContentExample_ShouldShowItsContentAndNameTheButtonFromText_WhenRendered()
    {
        var cut = Render<LabelContentExample>();

        Assert.Equal("Route", cut.Find("label em").TextContent);
        Assert.NotNull(Button(cut, "Information about Route name"));
    }

    [Fact]
    public void ButtonNameExample_ShouldNameTheButtonFromItsOwnWording_WhenRendered()
    {
        var cut = Render<LabelButtonNameExample>();

        Assert.NotNull(Button(cut, "What does Trail grade mean?"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheLabel()
    {
        var cut = ComponentPage("label");

        Assert.Equal(["Enter", "Space", "Escape", "Tab"], cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("Required", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
