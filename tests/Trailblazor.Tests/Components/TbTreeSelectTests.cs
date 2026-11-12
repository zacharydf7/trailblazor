using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbTreeSelectTests : BunitContext
{
    public TbTreeSelectTests()
    {
        this.AddTrailblazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public sealed record Place(string Id, string Name, IReadOnlyList<Place>? Children = null, bool Area = false);

    public sealed record Numbered(int Id, string Name);

    private static readonly Place[] Places =
    [
        new("north", "North ridge", [new("pine", "Pine Hollow"), new("eagle", "Eagle Ridge")], Area: true),
        new("valley", "River valley", [new("cedar", "Cedar Flats")], Area: true),
    ];

    private List<string?> Chosen { get; } = [];

    private IRenderedComponent<TbTreeSelect<Place, string>> RenderPlaces(
        Action<ComponentParameterCollectionBuilder<TbTreeSelect<Place, string>>>? configure = null) =>
        Render<TbTreeSelect<Place, string>>(parameters =>
        {
            parameters.Add(select => select.Label, "Trail").Add(select => select.Items, Places)
                .Add(select => select.KeySelector, place => place.Id).Add(select => select.ItemText, place => place.Name)
                .Add(select => select.Placeholder, "Choose a trail")
                .Add(select => select.ValueChanged, (string id) => Chosen.Add(id));
            configure?.Invoke(parameters);
        });

    private IRenderedComponent<TbTreeSelect<Place, string>> RenderInMemory(
        Action<ComponentParameterCollectionBuilder<TbTreeSelect<Place, string>>>? configure = null) =>
        RenderPlaces(parameters =>
        {
            parameters.Add(select => select.ChildrenSelector, place => place.Children)
                .Add(select => select.InitialExpandDepth, 1);
            configure?.Invoke(parameters);
        });

    private static AngleSharp.Dom.IElement Item(IRenderedComponent<TbTreeSelect<Place, string>> cut, string name) =>
        cut.FindAll("[role=treeitem]").Single(item => ItemName(item) == name);

    private static string ItemName(AngleSharp.Dom.IElement item) =>
        item.QuerySelector("[data-part=content]")!.TextContent.Trim();

    [Fact]
    public void Render_ShouldNameTheButtonByItsLabelAndChoice_WhenShown()
    {
        var cut = RenderInMemory();

        var trigger = cut.Find("[data-part=input]");
        Assert.Equal("BUTTON", trigger.TagName);
        Assert.Equal(trigger.Id, cut.Find("label").GetAttribute("for"));
        var names = trigger.GetAttribute("aria-labelledby")!.Split(' ');
        Assert.Equal(["Trail", "Choose a trail"], names.Select(id => cut.Find($"#{id}").TextContent.Trim()));
        Assert.Equal("dialog", trigger.GetAttribute("aria-haspopup"));
    }

    [Fact]
    public void Choose_ShouldSetTheValueAndShowItsText_WhenAnItemIsClicked()
    {
        var cut = RenderInMemory();

        Item(cut, "Cedar Flats").Click();

        Assert.Equal(["cedar"], Chosen);
        Assert.Equal("Cedar Flats", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public async Task Choose_ShouldCloseTheTreeWithoutRaisingAChange_WhenTheChosenItemIsClickedAgain()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Value, "cedar"));
        var popover = cut.FindComponent<TbPopover>().Instance;
        await cut.InvokeAsync(() => popover.OnToggleAsync(true));

        Item(cut, "Cedar Flats").Click();

        Assert.False(popover.Open);
        Assert.Empty(Chosen);
    }

    [Fact]
    public async Task Clear_ShouldEmptyTheValueAndRemoveTheButton_WhenClearableAndClicked()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Value, "cedar")
            .Add(select => select.Clearable, true));

        await cut.Find("[data-part=clear]").ClickAsync(new());

        Assert.Equal([null], Chosen);
        cut.Render(parameters => parameters.Add(select => select.Value, null));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
        Assert.Equal("Choose a trail", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData("cedar", true, false)]
    [InlineData("cedar", false, true)]
    public void Render_ShouldOfferNoClearButton_WhenThereIsNothingToClearOrItCantChange(string? value, bool disabled,
        bool readOnly)
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Value, value)
            .Add(select => select.Clearable, true).Add(select => select.Disabled, disabled)
            .Add(select => select.ReadOnly, readOnly));

        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Open_ShouldOpenTheTree_WhenTheParentSetsIt()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Open, true));

        Assert.True(cut.FindComponent<TbPopover>().Instance.Open);
    }

    [Fact]
    public void Open_ShouldCloseTheTreeAndItsSearch_WhenTheParentClearsIt()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Open, true)
            .Add(select => select.Searchable, true));
        cut.Find("[data-part=search]").Input("cedar");

        cut.Render(parameters => parameters.Add(select => select.Open, false));

        Assert.False(cut.FindComponent<TbPopover>().Instance.Open);
        Assert.Equal("", cut.Find("[data-part=search]").GetAttribute("value") ?? "");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Open_ShouldStayClosedAndSaySo_WhenTheFieldCantChange(bool disabled, bool readOnly)
    {
        var reported = new List<bool>();
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Disabled, disabled)
            .Add(select => select.ReadOnly, readOnly)
            .Add(select => select.OpenChanged, (bool open) => reported.Add(open)));

        cut.Render(parameters => parameters.Add(select => select.Open, true));

        Assert.DoesNotContain(cut.FindComponents<TbPopover>(), popover => popover.Instance.Open);
        Assert.Equal([false], reported);
    }

    [Fact]
    public async Task OpenChanged_ShouldReportEachToggle_WhenTheUserOpensAndChooses()
    {
        var reported = new List<bool>();
        var cut = RenderInMemory(parameters => parameters.Add(select => select.OpenChanged,
            (bool open) => reported.Add(open)));
        var popover = cut.FindComponent<TbPopover>().Instance;

        await cut.InvokeAsync(() => popover.OnToggleAsync(true));
        Item(cut, "Cedar Flats").Click();

        Assert.Equal([true, false], reported);
    }

    [Fact]
    public void Render_ShouldShowANestedItemsText_WhenTheValueIsSet()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Value, "eagle"));

        Assert.Equal("Eagle Ridge", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldAskForTheText_WhenTheValuesItemIsntLoaded()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Value, "summit")
            .Add(select => select.UnmatchedText, id => id == "summit" ? "Summit Gate" : id));

        Assert.Equal("Summit Gate", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Choose_ShouldShowTheItemsText_WhenItsBranchWasLoaded()
    {
        var cut = RenderPlaces(parameters => parameters
            .Add(select => select.LoadChildren, (place, _) => Task.FromResult<IEnumerable<Place>>(place.Children ?? []))
            .Add(select => select.HasChildren, place => place.Children is not null));

        Item(cut, "North ridge").QuerySelector("[data-part=toggle]")!.Click();
        cut.WaitForAssertion(() => Item(cut, "Pine Hollow").Click());

        Assert.Equal(["pine"], Chosen);
        Assert.Equal("Pine Hollow", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldShowThePlaceholder_WhenAnIntKeyHoldsItsDefault()
    {
        var cut = Render<TbTreeSelect<Numbered, int>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, [new(1, "Pine Hollow"), new(2, "Cedar Flats")])
            .Add(select => select.KeySelector, item => item.Id).Add(select => select.ItemText, item => item.Name)
            .Add(select => select.Placeholder, "Choose a trail").Add(select => select.Required, true));

        Assert.Equal("Choose a trail", cut.Find("[data-part=value]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[role=treeitem][aria-selected=true]"));
        Assert.Equal("true", cut.Find("[data-part=input]").GetAttribute("aria-required"));
    }

    [Fact]
    public void Activate_ShouldLeaveTheValue_WhenTheItemCantBeChosen()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.IsSelectable, place => !place.Area));

        Item(cut, "North ridge").DoubleClick();
        Item(cut, "North ridge").Click();

        Assert.Empty(Chosen);
    }

    [Fact]
    public void Search_ShouldKeepTheMatchingItemsAndTheirParents_WhenTyped()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));

        cut.Find("[data-part=search]").Input("cedar");

        Assert.Equal(["River valley", "Cedar Flats"], cut.FindAll("[role=treeitem]").Select(ItemName));
    }

    [Fact]
    public void Search_ShouldKeepTheItemsUnderAMatchingHeading_WhenTyped()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));

        cut.Find("[data-part=search]").Input("north");

        Assert.Equal(["North ridge", "Pine Hollow", "Eagle Ridge"], cut.FindAll("[role=treeitem]").Select(ItemName));
    }

    [Fact]
    public void Search_ShouldShowAndAnnounceThatNothingMatches_WhenNoItemDoes()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));

        cut.Find("[data-part=search]").Input("summit");

        Assert.Equal("No matches", cut.Find("[data-part=no-results]").TextContent.Trim());
        host.WaitForAssertion(() => Assert.Contains("No matches", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public void Labels_ShouldWordTheSearchNoMatchesAndClear_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true)
            .Add(select => select.Clearable, true).Add(select => select.Value, "cedar")
            .Add(select => select.Labels, new() { Search = "Find a trail", NoResults = "No trail by that name",
                Clear = "Clear the trail" }));

        cut.Find("[data-part=search]").Input("summit");

        Assert.Equal("Find a trail", cut.Find("[data-part=search]").GetAttribute("aria-label"));
        Assert.Equal("Clear the trail", cut.Find("[data-part=clear]").GetAttribute("aria-label"));
        Assert.Equal("No trail by that name", cut.Find("[data-part=no-results]").TextContent.Trim());
        host.WaitForAssertion(() => Assert.Contains("No trail by that name", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public void Labels_ShouldReachTheTree_WhenGivenTreeLabels()
    {
        var cut = Render<TbTreeSelect<Place, string>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, []).Add(select => select.KeySelector, place => place.Id)
            .Add(select => select.ItemText, place => place.Name)
            .Add(select => select.Labels, new() { Tree = new() { Empty = "No trails yet" } }));

        Assert.Equal("No trails yet", cut.Find(".tb-tree-select__tree [data-part=empty]").TextContent.Trim());
    }

    [Fact]
    public void Search_ShouldKeepItsFilter_WhenTheFieldRendersAgain()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));
        cut.Find("[data-part=search]").Input("cedar");
        var filter = cut.FindComponent<TbTreeView<Place, string>>().Instance.Filter;

        cut.Render();

        Assert.NotNull(filter);
        Assert.Same(filter, cut.FindComponent<TbTreeView<Place, string>>().Instance.Filter);
    }

    [Fact]
    public void Render_ShouldNotWalkTheTreeAgain_WhenTheItemsAreTheSame()
    {
        var reads = 0;
        var cut = RenderPlaces(parameters => parameters.Add(select => select.Value, "cedar")
            .Add(select => select.ChildrenSelector, place =>
            {
                reads++;
                return place.Children;
            }));
        var first = reads;

        cut.Render();

        Assert.Equal(first, reads);
        Assert.Equal("Cedar Flats", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldShowTheNewItemsText_WhenGivenANewItemsCollection()
    {
        var cut = RenderPlaces(parameters => parameters.Add(select => select.Value, "pine")
            .Add(select => select.ChildrenSelector, place => place.Children));

        cut.Render(parameters => parameters.Add(select => select.Items, [new Place("pine", "Pine Hollow loop")]));

        Assert.Equal("Pine Hollow loop", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public async Task RefreshAsync_ShouldShowAnItemChangedInPlace_WhenCalled()
    {
        List<Place> places = [new("pine", "Pine Hollow")];
        var cut = Render<TbTreeSelect<Place, string>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, places).Add(select => select.KeySelector, place => place.Id)
            .Add(select => select.ItemText, place => place.Name).Add(select => select.Value, "pine"));
        places[0] = new("pine", "Pine Hollow loop");

        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        Assert.Equal("Pine Hollow loop", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public async Task TreeSelectDriver_ShouldChooseAnItemByItsPathAndReadItsText_WhenGivenAPath()
    {
        var cut = RenderInMemory();
        var field = cut.Tb().TreeSelect("Trail");
        Assert.Null(field.Text);

        await field.ChooseAsync("North ridge", "Pine Hollow");

        Assert.Equal(["pine"], Chosen);
        Assert.Equal("Pine Hollow", field.Text);
    }

    [Fact]
    public async Task TreeSelectDriver_ShouldNarrowTheTree_WhenSearching()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));

        await cut.Tb().TreeSelect("Trail").SearchAsync("cedar");

        Assert.Equal(["River valley", "Cedar Flats"], cut.FindAll("[role=treeitem]").Select(ItemName));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task TreeSelectDriver_ShouldThrow_WhenTheFieldCantChange(bool disabled, bool readOnly)
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Disabled, disabled)
            .Add(select => select.ReadOnly, readOnly));

        var problem = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.Tb().TreeSelect("Trail").ChooseAsync("River valley", "Cedar Flats"));

        Assert.Contains("disabled or read-only", problem.Message);
    }

    [Fact]
    public void Search_ShouldStaySilent_WhenSomethingMatches()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));

        cut.Find("[data-part=search]").Input("eagle");

        Assert.Empty(cut.FindAll("[data-part=no-results]"));
        Assert.DoesNotContain("No matches", host.Markup);
    }

    [Fact]
    public void Search_ShouldStaySilent_WhenOnlyALoadedChildMatches()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderPlaces(parameters => parameters.Add(select => select.Searchable, true)
            .Add(select => select.LoadChildren, (place, _) => Task.FromResult<IEnumerable<Place>>(place.Children ?? []))
            .Add(select => select.HasChildren, place => place.Children is not null));
        Item(cut, "North ridge").QuerySelector("[data-part=toggle]")!.Click();
        cut.WaitForAssertion(() => Item(cut, "Pine Hollow"));

        cut.Find("[data-part=search]").Input("pine");

        Assert.DoesNotContain("No matches", host.Markup);
    }

    [Fact]
    public void Search_ShouldAnnounceNothingMatchesOnce_WhenTheUserKeepsTypingWithoutAMatch()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));

        cut.Find("[data-part=search]").Input("sum");
        cut.Find("[data-part=search]").Input("summit");

        host.WaitForAssertion(() => Assert.Single(host.FindAll("[role=status] p"),
            message => message.TextContent == "No matches"));
    }

    [Fact]
    public async Task Search_ShouldStartAfresh_WhenTheTreeClosesWithoutAChoice()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Searchable, true));
        var popover = cut.FindComponent<TbPopover>().Instance;
        await cut.InvokeAsync(() => popover.OnToggleAsync(true));
        cut.Find("[data-part=search]").Input("summit");

        await cut.InvokeAsync(() => popover.OnToggleAsync(false));
        Assert.Equal("", cut.Find("[data-part=search]").GetAttribute("value") ?? "");
        cut.Find("[data-part=search]").Input("summit");

        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("[role=status] p")
            .Count(message => message.TextContent == "No matches")));
    }

    [Fact]
    public void Render_ShouldShowAFocusableButtonThatOpensNothing_WhenReadOnly()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Value, "pine")
            .Add(select => select.ReadOnly, true));

        var trigger = cut.Find("[data-part=input]");
        Assert.False(trigger.HasAttribute("disabled"));
        Assert.False(trigger.HasAttribute("aria-haspopup"));
        Assert.Equal("Pine Hollow", cut.Find("[data-part=value]").TextContent.Trim());
        Assert.Empty(cut.FindComponents<TbPopover>());
    }

    [Fact]
    public void Render_ShouldDisableTheButton_WhenDisabled()
    {
        var cut = RenderInMemory(parameters => parameters.Add(select => select.Disabled, true));

        Assert.True(cut.Find("[data-part=input]").HasAttribute("disabled"));
    }

    [Fact]
    public void Form_ShouldBeInvalid_WhenARequiredTreeSelectIsEmpty()
    {
        bool? valid = null;
        Render<TbForm>(parameters => parameters.Add(form => form.IsValid, true)
            .Add(form => form.IsValidChanged, (bool now) => valid = now)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTreeSelect<Place, string>>(0);
                builder.AddAttribute(1, nameof(TbTreeSelect<Place, string>.Label), "Trail");
                builder.AddAttribute(2, nameof(TbTreeSelect<Place, string>.Items), Places);
                builder.AddAttribute(3, nameof(TbTreeSelect<Place, string>.KeySelector), (Func<Place, string>)(p => p.Id));
                builder.AddAttribute(4, nameof(TbTreeSelect<Place, string>.ItemText), (Func<Place, string>)(p => p.Name));
                builder.AddAttribute(5, nameof(TbTreeSelect<Place, string>.Required), true);
                builder.CloseComponent();
            })));

        Assert.False(valid);
    }
}
