using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDescriptionListTests : BunitContext
{
    public TbDescriptionListTests() => this.AddTrailblazor();

    private IRenderedComponent<TbDescriptionList> RenderRemoval(
        Action<ComponentParameterCollectionBuilder<TbDescriptionList>>? more = null, string? reason = null) =>
        Render<TbDescriptionList>(parameters =>
        {
            parameters.Add(list => list.ChildContent, (RenderFragment)(builder =>
            {
                Item(builder, 0, "Removed by", "Robin Hale");
                Item(builder, 10, "Reason", reason);
            }));
            more?.Invoke(parameters);
        });

    private static void Item(RenderTreeBuilder builder, int sequence, string term, string? value)
    {
        builder.OpenComponent<TbDescriptionItem>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbDescriptionItem.Term), term);
        builder.AddComponentParameter(sequence + 2, nameof(TbDescriptionItem.Value), value);
        builder.CloseComponent();
    }

    [Fact]
    public void TbDescriptionList_ShouldRenderTermsAndValues_WhenGivenItems()
    {
        var cut = RenderRemoval(reason: "Duplicate trail");

        // Each pair sits in a div inside the dl, which HTML allows.
        Assert.Equal(2, cut.FindAll("dl > div[data-part=item] > dt + dd").Count);
        var details = cut.Tb().DescriptionList();
        Assert.Equal(["Removed by", "Reason"], details.Terms);
        Assert.Equal(("Robin Hale", "Duplicate trail"), (details.ValueOf("Removed by"), details.ValueOf("Reason")));
    }

    [Fact]
    public void TbDescriptionItem_ShouldShowEmptyPlaceholder_WhenValueIsBlank()
    {
        var cut = RenderRemoval(reason: " ");

        var value = cut.FindAll("[data-part=value]")[1];
        Assert.Equal(("—", "Not set"), (value.QuerySelector("[aria-hidden=true]")!.TextContent,
            value.QuerySelector(".tb-visually-hidden")!.TextContent));
        Assert.Equal("", cut.Tb().DescriptionList().ValueOf("Reason"));
    }

    [Fact]
    public void EmptyText_ShouldBeWhatScreenReadersHear_WhenGivenForTheList()
    {
        var cut = RenderRemoval(parameters => parameters.Add(list => list.EmptyText, "None given"));

        Assert.Equal("None given", cut.Find("[data-part=value] .tb-visually-hidden").TextContent);
    }

    [Fact]
    public void Render_ShouldSetTheLayoutAndColumns_WhenGiven()
    {
        var cut = RenderRemoval(parameters => parameters.Add(list => list.Layout, DescriptionLayout.Inline)
            .Add(list => list.Columns, 2));

        var root = cut.Find(".tb-description-list");
        Assert.Contains("tb-description-list--inline", root.ClassList);
        Assert.Contains("--tb-description-list-columns: 2", root.GetAttribute("style"));
    }

    [Fact]
    public void Item_ShouldShowItsContentAndTermContent_WhenGivenThem()
    {
        var cut = Render<TbDescriptionList>(parameters => parameters.AddChildContent<TbDescriptionItem>(item => item
            .Add(each => each.TermContent, "<em>Trail</em>")
            .Add(each => each.ChildContent, "<a href=\"/t/1\">Ridge</a>")));

        Assert.Equal("Trail", cut.Find("dt em").TextContent);
        Assert.Equal("Ridge", cut.Tb().DescriptionList().ValueOf("Trail"));
    }

    [Fact]
    public void ValueOf_ShouldThrowNamingTheTerms_WhenNoTermMatches()
    {
        var cut = RenderRemoval();

        var error = Assert.Throws<InvalidOperationException>(() => cut.Tb().DescriptionList().ValueOf("Date"));

        Assert.Contains("\"Removed by\", \"Reason\"", error.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnItemHasNoTermOrIsOutsideAList()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbDescriptionList>(parameters =>
            parameters.AddChildContent<TbDescriptionItem>(item => item.Add(each => each.Value, "Robin"))));
        Assert.Throws<InvalidOperationException>(() => Render<TbDescriptionItem>(parameters =>
            parameters.Add(item => item.Term, "Removed by")));
    }

    [Fact]
    public void Columns_ShouldThrow_WhenLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderRemoval(parameters =>
            parameters.Add(list => list.Columns, 0)));
    }

    [Fact]
    public void EmptyText_ShouldReachTheItems_WhenItChangesAfterTheFirstRender()
    {
        var cut = RenderRemoval();

        cut.Render(parameters => parameters.Add(list => list.EmptyText, "None given"));

        Assert.Equal("None given", cut.Find("[data-part=value] .tb-visually-hidden").TextContent);
    }

    [Fact]
    public void ValueOf_ShouldReadTheListsOwnPairs_WhenAValueHoldsAnotherList()
    {
        var cut = RenderNested();

        var details = cut.Tb().DescriptionList();

        Assert.Equal(["Trail", "Owner"], details.Terms);
        // The inner list's Owner comes first in the markup, but it belongs to the trail's value.
        Assert.Equal("Robin Hale", details.ValueOf("Owner"));
    }

    [Fact]
    public void Within_ShouldReachTheInnerList_WhenScopedToTheOuterValue()
    {
        var cut = RenderNested();

        var inner = cut.Tb().Within(() => cut.Find("[data-part=value]")).DescriptionList();

        Assert.Equal("Park service", inner.ValueOf("Owner"));
    }

    // An outer list whose Trail value holds an inner list with an Owner of its own, before the outer Owner.
    private IRenderedComponent<TbDescriptionList> RenderNested() => Render<TbDescriptionList>(parameters =>
        parameters.Add(list => list.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbDescriptionItem>(0);
            builder.AddComponentParameter(1, nameof(TbDescriptionItem.Term), "Trail");
            builder.AddComponentParameter(2, nameof(TbDescriptionItem.ChildContent), (RenderFragment)(inner =>
            {
                inner.OpenComponent<TbDescriptionList>(0);
                inner.AddComponentParameter(1, nameof(TbDescriptionList.ChildContent), (RenderFragment)(pair =>
                    Item(pair, 0, "Owner", "Park service")));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
            Item(builder, 10, "Owner", "Robin Hale");
        })));
}
