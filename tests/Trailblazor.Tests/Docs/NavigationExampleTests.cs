using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs;

// The tabs, accordion and pagination pages' examples, as the docs show them.
public class NavigationExampleTests : DocsTest
{
    [Fact]
    public async Task TabsIndexExample_ShouldShowTheDay_WhenItsTabIsSelected()
    {
        var cut = Render<TabsIndexExample>();

        await cut.Tb().Tabs("Days of the trip").SelectAsync("Saturday");

        Assert.Equal("Day 2 of 3", cut.Find("output").TextContent);
    }

    [Fact]
    public void AccordionMultipleExample_ShouldKeepBothOpen_WhenBothAreOpened()
    {
        var cut = Render<AccordionMultipleExample>();

        Button(cut, "Permits").Click();
        Button(cut, "Parking").Click();

        Assert.Equal("true", Button(cut, "Permits").GetAttribute("aria-expanded"));
        Assert.Equal("true", Button(cut, "Parking").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void AccordionBindExample_ShouldOpenTheItem_WhenTheButtonIsPressed()
    {
        var cut = Render<AccordionBindExample>();

        Button(cut, "Show the route").Click();

        Assert.Equal("true", Button(cut, "Route details").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void AccordionActionsExample_ShouldLeaveTheSectionClosed_WhenItsActionIsPressed()
    {
        var cut = Render<AccordionActionsExample>();

        Button(cut, "Print the policy").Click();

        Assert.Contains("Printed the policy 1 time.", cut.Markup);
        Assert.Equal("false", Button(cut, "Returns").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void PaginationLabelsExample_ShouldNameItsButtonsInFrench_WhenRendered()
    {
        var cut = Render<PaginationLabelsExample>();

        Assert.NotNull(cut.Find("button[aria-label='Page suivante']"));
    }
}
