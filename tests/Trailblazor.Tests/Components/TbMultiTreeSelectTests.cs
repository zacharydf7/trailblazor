using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbMultiTreeSelectTests : BunitContext
{
    public TbMultiTreeSelectTests()
    {
        this.AddTrailblazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public sealed record Place(string Id, string Name, IReadOnlyList<Place>? Children = null, bool Area = false);

    private static readonly Place[] Places =
    [
        new("north", "North ridge", [new("pine", "Pine Hollow"), new("eagle", "Eagle Ridge")], Area: true),
        new("valley", "River valley", [new("cedar", "Cedar Flats")], Area: true),
    ];

    // A heading inside a heading.
    private static readonly Place[] Countries =
    [
        new("land", "Highlands", [new("north", "North ridge", [new("pine", "Pine Hollow")], Area: true)], Area: true),
    ];

    private List<IReadOnlyList<string>> Chosen { get; } = [];

    private IRenderedComponent<TbMultiTreeSelect<Place, string>> RenderPlaces(
        Action<ComponentParameterCollectionBuilder<TbMultiTreeSelect<Place, string>>>? configure = null,
        Place[]? items = null) =>
        Render<TbMultiTreeSelect<Place, string>>(parameters =>
        {
            parameters.Add(select => select.Label, "Trails").Add(select => select.Items, items ?? Places)
                .Add(select => select.KeySelector, place => place.Id)
                .Add(select => select.ItemText, place => place.Name)
                .Add(select => select.ChildrenSelector, place => place.Children)
                .Add(select => select.InitialExpandDepth, 1).Add(select => select.Placeholder, "Choose trails")
                .Add(select => select.ValueChanged, (IReadOnlyList<string> ids) => Chosen.Add(ids));
            configure?.Invoke(parameters);
        });

    private static AngleSharp.Dom.IElement Item(IRenderedComponent<TbMultiTreeSelect<Place, string>> cut,
        string name) =>
        cut.FindAll("[role=treeitem]").Single(item => item.QuerySelector("[data-part=content]")!.TextContent.Trim()
            == name);

    [Fact]
    public void Click_ShouldCheckTheItemAndAddItToTheValue_WhenARowIsClicked()
    {
        var cut = RenderPlaces();

        Item(cut, "Cedar Flats").Click();
        Item(cut, "Pine Hollow").Click();

        // Cedar Flats is all of River valley, so checking it checks the valley too; the value follows tree order.
        Assert.Equal(["pine", "valley", "cedar"], Chosen[^1]);
        Assert.Equal("true", Item(cut, "Cedar Flats").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Render_ShouldShowTheCheckedItemsTextOrThePlaceholder_WhenGivenAValue()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.Value, ["pine", "cedar"]));
        Assert.Equal("Pine Hollow, Cedar Flats", cut.Find("[data-part=value]").TextContent.Trim());

        cut.Render(parameters => parameters.Add(select => select.Value, []));

        Assert.Equal("Choose trails", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Theory]
    [InlineData(null, null, "Pine Hollow, Eagle Ridge, Cedar Flats")]
    [InlineData(2, null, "Pine Hollow, Eagle Ridge, 1 more")]
    [InlineData(0, "{0} trails", "3 trails")]
    public void Render_ShouldNameUpToMaxShownItemsAndCountTheRest_WhenGivenMaxShown(int? maxShown, string? more,
        string expected)
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.Value, ["pine", "eagle", "cedar"])
            .Add(select => select.MaxShown, maxShown).Add(select => select.MoreLabel, more));

        Assert.Equal(expected, cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldNameThreeItemsAndCountTheRest_WhenMaxShownIsLeftAlone()
    {
        var cut = RenderPlaces(parameters =>
            parameters.Add(select => select.Value, ["pine", "eagle", "cedar", "valley"]));

        Assert.Equal("Pine Hollow, Eagle Ridge, Cedar Flats, 1 more", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Check_ShouldLeaveAHeadingOutOfTheValue_WhenIsSelectableExcludesIt()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.IsSelectable, place => !place.Area));

        // A click on a heading's row opens or closes it, so the test checks it through its checkbox.
        Item(cut, "North ridge").QuerySelector("[data-part=check]")!.Click();

        Assert.Equal(["pine", "eagle"], Chosen[^1]);
    }

    [Fact]
    public void Check_ShouldUncheckEverythingInside_WhenAHeadingAboveAnotherIsClickedTwice()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.IsSelectable, place => !place.Area),
            Countries);

        Item(cut, "Highlands").QuerySelector("[data-part=check]")!.Click();
        Assert.Equal(["pine"], Chosen[^1]);
        Item(cut, "Highlands").QuerySelector("[data-part=check]")!.Click();

        Assert.Empty(Chosen[^1]);
        Assert.Equal("false", Item(cut, "Highlands").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Check_ShouldUncheckANestedHeadingOnTheFirstClick_WhenTheValueCameFromOutside()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.IsSelectable, place => !place.Area)
            .Add(select => select.Value, ["pine"]), Countries);
        Assert.Equal("true", Item(cut, "Highlands").GetAttribute("aria-checked"));

        Item(cut, "Highlands").QuerySelector("[data-part=check]")!.Click();

        Assert.Empty(Chosen[^1]);
        Assert.Equal("false", Item(cut, "Highlands").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Check_ShouldReachItemsThatLoadLater_WhenAHeadingAboveThemIsChecked()
    {
        // Loaded rather than held in memory, so North ridge's trail arrives only as it opens.
        var cut = Render<TbMultiTreeSelect<Place, string>>(parameters => parameters
            .Add(select => select.Label, "Trails").Add(select => select.Items, Countries)
            .Add(select => select.KeySelector, place => place.Id).Add(select => select.ItemText, place => place.Name)
            .Add(select => select.IsSelectable, place => !place.Area)
            .Add(select => select.LoadChildren,
                (place, _) => Task.FromResult<IEnumerable<Place>>(place.Children ?? []))
            .Add(select => select.HasChildren, place => place.Children is not null)
            .Add(select => select.ValueChanged, (IReadOnlyList<string> ids) => Chosen.Add(ids)));
        // Nothing inside Highlands has loaded yet.
        Item(cut, "Highlands").QuerySelector("[data-part=check]")!.Click();
        Assert.Empty(Chosen);
        Item(cut, "Highlands").QuerySelector("[data-part=toggle]")!.Click();
        cut.WaitForAssertion(() => Item(cut, "North ridge").QuerySelector("[data-part=toggle]")!.Click());

        cut.WaitForAssertion(() => Assert.Equal(["pine"], Chosen[^1]));
        Assert.Equal("true", Item(cut, "Highlands").GetAttribute("aria-checked"));
    }

    [Fact]
    public async Task Reset_ShouldUncheckAHeadingWhoseItemsHaventLoaded_WhenTheFormResets()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbMultiTreeSelect<Place, string>>(0);
            builder.AddAttribute(1, "Label", "Trails");
            builder.AddAttribute(2, "Items", Countries);
            builder.AddAttribute(3, "KeySelector", (Func<Place, string>)(place => place.Id));
            builder.AddAttribute(4, "ItemText", (Func<Place, string>)(place => place.Name));
            builder.AddAttribute(5, "IsSelectable", (Func<Place, bool>)(place => !place.Area));
            builder.AddAttribute(6, "LoadChildren", (Func<Place, CancellationToken, Task<IEnumerable<Place>>>)
                ((place, _) => Task.FromResult<IEnumerable<Place>>(place.Children ?? [])));
            builder.AddAttribute(7, "HasChildren", (Func<Place, bool>)(place => place.Children is not null));
            builder.CloseComponent();
        })));
        AngleSharp.Dom.IElement Row(string name) => cut.FindAll("[role=treeitem]")
            .Single(item => item.QuerySelector("[data-part=content]")!.TextContent.Trim() == name);
        Row("Highlands").QuerySelector("[data-part=check]")!.Click();

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());
        Row("Highlands").QuerySelector("[data-part=toggle]")!.Click();
        cut.WaitForAssertion(() => Row("North ridge").QuerySelector("[data-part=toggle]")!.Click());

        cut.WaitForAssertion(() => Row("Pine Hollow"));
        Assert.Equal("false", Row("Pine Hollow").GetAttribute("aria-checked"));
        Assert.Equal("false", Row("Highlands").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Check_ShouldOfferNoCheckOnAHeading_WhenIsSelectableExcludesItAndItHoldsNothing()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.IsSelectable, place => !place.Area),
            [.. Places, new("closed", "Closed ridge", Area: true)]);

        Assert.Null(Item(cut, "Closed ridge").QuerySelector("[data-part=check]"));
        Assert.NotNull(Item(cut, "North ridge").QuerySelector("[data-part=check]"));
    }

    [Fact]
    public void Check_ShouldOfferNoCheckOnAHeading_WhenIsSelectableExcludesItAndCascadeChecksIsOff()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.IsSelectable, place => !place.Area)
            .Add(select => select.CascadeChecks, false));
        var north = Item(cut, "North ridge");

        Assert.Null(north.QuerySelector("[data-part=check]"));
        Assert.Null(north.GetAttribute("aria-checked"));
        Assert.NotNull(Item(cut, "Pine Hollow").QuerySelector("[data-part=check]"));
    }

    [Fact]
    public void Check_ShouldCheckOnlyTheRowClicked_WhenCascadeChecksIsOff()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.CascadeChecks, false));

        Item(cut, "North ridge").Click();

        Assert.Equal(["north"], Chosen[^1]);
    }
}
