using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbEmptyStateTests : BunitContext
{
    private static readonly Icon Folder = new("<path d=\"M3 5h7l2 2h9v12H3z\" />");

    public TbEmptyStateTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldShowTheTitleAsAHeadingWithItsDescriptionAndActions_WhenGivenThem()
    {
        var cut = Render<TbEmptyState>(parameters => parameters.Add(empty => empty.Title, "No trails saved yet")
            .Add(empty => empty.Description, "Trails you save show here.").Add(empty => empty.Icon, Folder)
            .Add(empty => empty.Actions, (RenderFragment)(builder => builder.AddMarkupContent(0,
                "<button type=\"button\">Find a trail</button>"))));

        Assert.Equal("No trails saved yet", cut.Find("h3[data-part=title]").TextContent);
        Assert.Equal("Trails you save show here.", cut.Find("[data-part=description]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[data-part=icon] svg").GetAttribute("aria-hidden"));
        Assert.Equal("Find a trail", cut.Find("[data-part=actions] button").TextContent);
        Assert.DoesNotContain("tb-empty-state--compact", cut.Find(".tb-empty-state").ClassList);
    }

    [Fact]
    public void Render_ShouldLeaveOutTheParts_WhenOnlyATitleIsGiven()
    {
        var cut = Render<TbEmptyState>(parameters => parameters.Add(empty => empty.Title, "No trails saved yet"));

        Assert.Empty(cut.FindAll("[data-part=icon], [data-part=description], [data-part=actions]"));
    }

    [Fact]
    public void Render_ShouldShowItsOwnContentAndBeCompact_WhenGivenChildContentAndCompact()
    {
        var cut = Render<TbEmptyState>(parameters => parameters.Add(empty => empty.Title, "No trails match")
            .Add(empty => empty.Description, "Unused").Add(empty => empty.Compact, true)
            .AddChildContent("Try <strong>another name</strong>."));

        Assert.Equal("Try <strong>another name</strong>.", cut.Find("[data-part=description]").InnerHtml.Trim());
        Assert.Contains("tb-empty-state--compact", cut.Find(".tb-empty-state").ClassList);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void Render_ShouldUseTheGivenHeadingLevel_WhenHeadingLevelIsSet(int level)
    {
        var cut = Render<TbEmptyState>(parameters => parameters.Add(empty => empty.Title, "No trails saved yet")
            .Add(empty => empty.HeadingLevel, level));

        Assert.Equal($"H{level}", cut.Find("[data-part=title]").TagName);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheTitleIsBlank()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbEmptyState>(parameters => parameters
            .Add(empty => empty.Title, " ")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Render_ShouldThrow_WhenTheHeadingLevelIsOutOfRange(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbEmptyState>(parameters => parameters
            .Add(empty => empty.Title, "No trails saved yet").Add(empty => empty.HeadingLevel, level)));
    }
}
