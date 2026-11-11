using Bunit;
using Microsoft.AspNetCore.Components;
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
    public async Task TabsClosableExample_ShouldRemoveTheTabAndFocusTheNextOne_WhenATabIsClosed()
    {
        var cut = Render<TabsClosableExample>();

        await cut.Tb().Tabs("Open trails").CloseAsync("Ridge loop");

        Assert.Equal(["All trails", "Lake shore", "Cedar flats"], cut.Tb().Tabs("Open trails").Tabs);
        Assert.Equal("Lake shore", cut.Tb().Tabs("Open trails").Selected);
        Assert.Equal(Header(cut, "Lake shore").Id, FocusedReference().Id);
    }

    [Fact]
    public async Task TabsClosableExample_ShouldFocusTheTabBefore_WhenTheLastTabIsClosed()
    {
        var cut = Render<TabsClosableExample>();

        await cut.Tb().Tabs("Open trails").CloseAsync("Cedar flats");

        Assert.Equal(["All trails", "Ridge loop", "Lake shore"], cut.Tb().Tabs("Open trails").Tabs);
        Assert.Equal(Header(cut, "Lake shore").Id, FocusedReference().Id);
    }

    [Fact]
    public async Task TabsAddExample_ShouldAddADayAndShowIt_WhenTheAddButtonIsClicked()
    {
        var cut = Render<TabsAddExample>();

        await cut.Tb().Tabs("Trip days").AddAsync();

        Assert.Equal(["Day 1", "Day 2", "Day 3"], cut.Tb().Tabs("Trip days").Tabs);
        Assert.Equal("Day 3", cut.Tb().Tabs("Trip days").Selected);
        Assert.Equal("Plans for day 3.", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    // bUnit's markup loses a re-rendered header's reference id, so the tab's own reference is compared instead.
    private static ElementReference Header(IRenderedComponent<TabsClosableExample> cut, string text) =>
        cut.FindComponents<TbTab>().Single(tab => tab.Instance.Text == text).Instance.Header;

    private ElementReference FocusedReference() => (ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!;

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
